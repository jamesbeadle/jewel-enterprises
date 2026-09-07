using System.Net;
using System.Text.Json;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpQueryClient : IQueryClient
{
    private readonly HttpClient httpClient;
    private readonly QueryRouteTable routes;
    private readonly IErrorSink errors;
    private readonly AppVersionService versions;

    public HttpQueryClient(HttpClient httpClient, QueryRouteTable routes, IErrorSink errors, AppVersionService versions)
    {
        this.httpClient = httpClient;
        this.routes = routes;
        this.errors = errors;
        this.versions = versions;
    }

    public async Task<TResult> AskAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var operation = query.GetType().Name;
        var route = routes.For(query.GetType());
        var path = route.PathFor(query);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await SendAsync<TResult>(path, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Navigating away mid-fetch is not a fault.
                throw;
            }
            catch (HttpRequestException transient)
                when (attempt < RetryDelays.Length
                      && IsWorthRetrying(transient)
                      && !cancellationToken.IsCancellationRequested)
            {
                // Deliberately NOT reported: a cold start that answers on the second attempt is not
                // something the user needs to see. Only a failure that survives every attempt is.
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
            catch (HttpRequestException requestFailure)
            {
                // Every query failure is worth reporting: unlike a command, a failed read has no dialog
                // standing behind it to explain itself, and the page it feeds would otherwise just sit
                // there looking empty — the exact "zeroes that never fill in" this is meant to end.
                // The one exception is a query that declares itself best-effort (IBestEffortQuery):
                // it feeds decoration, its caller already treats "nothing" as the answer, and a red
                // banner over an otherwise complete page would be the only thing the user noticed.
                if (query is not IBestEffortQuery)
                {
                    var status = requestFailure.StatusCode is HttpStatusCode code ? (int)code : (int?)null;
                    errors.ReportRequestFailure(operation, "GET", path, status, null, requestFailure);
                }
                throw;
            }
            catch (Exception failure)
            {
                if (query is not IBestEffortQuery)
                    errors.ReportRequestFailure(operation, "GET", path, null, null, failure);
                throw;
            }
        }
    }

    /// <summary>
    /// What a retry repeats: one GET, and the reading of its answer. Every failure leaves here as an
    /// exception, so the caller above is the single place that decides between retrying and reporting.
    /// </summary>
    private async Task<TResult> SendAsync<TResult>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        // Every response carries the API's build number; noticing it here — before the status
        // check, so a stale tab's 401s still count — is what makes "each route load checks the
        // version" true without a single extra request. See AppVersionService.
        versions.ObserveResponse(response);

        response.EnsureSuccessStatusCode();

        // "Nothing recorded yet" is a legitimate answer to the nullable queries — a project with
        // no retention terms, a lead with no outcome — but it never arrives as JSON. ASP.NET
        // renders OkObjectResult(null) as 204 with an empty body, and deserialising nothing
        // throws. An empty body behind a success status means "none", not a broken response.
        //
        // Emptiness is decided on the BYTES, not the headers. Content-Length is absent on a
        // chunked response — which is how Static Web Apps' managed Functions routinely answer —
        // so a header-only check reads "unknown length" as "not empty", falls through to the
        // deserialiser and throws the very JsonException it was added to prevent.
        if (response.StatusCode == HttpStatusCode.NoContent) return Nothing<TResult>();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return Nothing<TResult>();

        // A literal "null" body lands here too, so both roads to "no answer" meet in one place.
        var result = JsonSerializer.Deserialize<TResult>(json, WebJson);
        return result ?? Nothing<TResult>();
    }

    /// <summary>
    /// How long to wait before each retry. Two retries, front-loaded: a warming host is usually
    /// there within a second, and a user waiting on a picker is not helped by a longer ladder.
    /// </summary>
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(600),
    };

    /// <summary>
    /// Whether a failed GET is worth repeating. Queries are reads — repeating one cannot double-apply
    /// anything — so the only question is whether the failure was about the request or about the road
    /// to it. The API is Static Web Apps managed Functions, whose gateway answers 503 while the host
    /// is cold or recycling and severs the connection outright when it restarts mid-flight; neither
    /// says anything about the query. A 4xx does, and repeating it just delays the answer.
    ///
    /// A null status is the connection-level failure — DNS, refused, reset — which is the shape a
    /// host restart takes when it lands between the request and the response.
    ///
    /// A 500 is retried too. In principle it says "the query itself broke", but on this platform a
    /// deploy landing under an open tab answers 500 as readily as 503 (2026-09-07: two unrelated
    /// reads, /projects and /records/activity, each failed once within minutes of a push to main
    /// and answered normally a minute later). Repeating a read costs nothing, and a 500 that is
    /// really about the query still fails all three attempts and is reported as before — 800 ms
    /// later than it would have been.
    /// </summary>
    private static bool IsWorthRetrying(HttpRequestException failure) =>
        failure.StatusCode switch
        {
            null => true,
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false,
        };

    /// <summary>The same options ReadFromJsonAsync applied by default, held once so moving to the
    /// explicit deserialiser changed nothing about how payloads are read.</summary>
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// What a query answers with when the server sent nothing.
    ///
    /// A single-record query may legitimately answer null — "this project has no retention terms".
    /// A LIST query must NEVER answer null, and that distinction is the whole point of this method.
    /// Pages dereference a list result directly (<c>unassigned.Count</c> in the triage queue), so a
    /// null list is a NullReferenceException at RENDER time — long after the load method's own
    /// try/catch could have helped, because nothing threw. The page's fallback never runs, the
    /// error boundary takes the whole screen, and the cause looks nothing like its origin. An empty
    /// body for a list means "none", and "none" is an empty list.
    ///
    /// The empty collection is built by deserialising "[]" rather than by reflection: the
    /// serialiser already has to instantiate this exact type on the normal path, so there is no
    /// extra trimming risk in a WASM publish. A result shaped like a dictionary can't be filled
    /// from "[]", so that falls back to null rather than throwing on the way out.
    /// </summary>
    private static TResult Nothing<TResult>()
    {
        var type = typeof(TResult);
        if (type == typeof(string) || !typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return default!;

        try { return JsonSerializer.Deserialize<TResult>("[]", WebJson)!; }
        catch { return default!; }
    }
}
