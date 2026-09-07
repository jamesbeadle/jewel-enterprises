using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jewel.JPMS.Api.Features.Xero;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Characterisation of the Xero client's writes — every request the portal makes that changes a
/// bill in Xero — pinned before <c>XeroClient.Writes</c> is divided: the exact JSON each write
/// sends, which bills each refuses and with what words, and how a staged draft decides its tax
/// type. A recording handler stands in for Xero's HTTP; the client is the real class.
/// </summary>
public sealed class XeroClientWritesTests
{
    private const string BillId = "bill-1";
    private const string SitesId = "cat-sites";
    private const string CostCodeId = "cat-cost";

    // ---- Reading a bill ----------------------------------------------------------------------

    [Fact]
    public async Task GetBillReadsTheSummaryOffXerosInvoice()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("AUTHORISED", lineAmountTypes: "Inclusive", subTotal: 3200m, tax: 0m, total: 3200m,
            lines: new[] { Line("l1", 3200m, 0m, "321", "NONE") });

        var summary = await xero.Client.GetBillAsync(BillId, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal("AUTHORISED", summary!.Status);
        Assert.Equal("Aug 2026", summary.InvoiceNumber);
        Assert.Equal("Adam Midgley", summary.ContactName);
        Assert.Equal(new DateTime(2026, 8, 25), summary.Date);
        Assert.Equal("Inclusive", summary.LineAmountTypes);
        Assert.Equal((3200m, 0m, 3200m), (summary.SubTotal, summary.TotalTax, summary.Total));
        Assert.Equal(1, summary.LineCount);
        Assert.Equal("NONE", summary.TaxType);
        Assert.Equal(new[] { "POST identity", "GET Invoices/bill-1" }, xero.Calls);
    }

    [Fact]
    public async Task GetBillTakesTheTaxTypeOfTheLargestLine()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 1000m, 200m, 1200m,
            new[] { Line("l1", 100m, 20m, "321", "INPUT2"), Line("l2", 900m, 0m, "321", "NONE") });

        var summary = await xero.Client.GetBillAsync(BillId, CancellationToken.None);

        Assert.Equal("NONE", summary!.TaxType);
    }

    [Fact]
    public async Task GetBillIsNullWhenXeroHasNoSuchBill()
    {
        var xero = new FakeXero { BillStatusCode = HttpStatusCode.NotFound };

        Assert.Null(await xero.Client.GetBillAsync(BillId, CancellationToken.None));
    }

    [Fact]
    public async Task GetBillThrowsWhenXeroIsNotConfigured()
    {
        var xero = new FakeXero(configured: false);

        var failure = await Assert.ThrowsAsync<XeroCallFailedException>(() => xero.Client.GetBillAsync(BillId, CancellationToken.None));
        Assert.StartsWith("Xero isn't connected", failure.Message);
    }

    // ---- Recoding a bill to a schedule -------------------------------------------------------

    [Fact]
    public async Task ARecodeProRatesTheBillsOwnMoneyAcrossTheSchedulesSplitAndKeepsItsStatus()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("AUTHORISED", "Inclusive", 3200m, 0m, 3200m, new[] { Line("old", 3200m, 0m, "321", "NONE") });
        xero.Updated = Bill("AUTHORISED", "Inclusive", 3200m, 0m, 3200m,
            new[] { Line("new-1", 2000m, 0m, "321", "NONE", "Woodhouse", "SUB-GWK"), Line("new-2", 1200m, 0m, "321", "NONE", "Woodhouse", "SUB-BRK") });

        var result = await xero.Client.RecodeBillAsync(
            new XeroBillCodingRequest(BillId, new[] { ScheduleLine("Groundworks", 1000m, "SUB-GWK"), ScheduleLine("Brickwork", 600m, "SUB-BRK") }),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "POST identity", "GET Invoices/bill-1", "GET TrackingCategories", "POST Invoices/bill-1" }, xero.Calls);
        var payload = xero.LastBody!;
        Assert.Equal(BillId, (string?)payload["InvoiceID"]);
        Assert.Equal("Inclusive", (string?)payload["LineAmountTypes"]);
        Assert.Null(payload["Status"]);
        var lines = payload["LineItems"]!.AsArray();
        Assert.Equal(2, lines.Count);
        Assert.Equal(2000m, (decimal?)lines[0]!["LineAmount"]);
        Assert.Equal(2000m, (decimal?)lines[0]!["UnitAmount"]);
        Assert.Equal(1m, (decimal?)lines[0]!["Quantity"]);
        Assert.Equal(0m, (decimal?)lines[0]!["TaxAmount"]);
        Assert.Equal("NONE", (string?)lines[0]!["TaxType"]);
        Assert.Equal("321", (string?)lines[0]!["AccountCode"]);
        Assert.Equal("Groundworks", (string?)lines[0]!["Description"]);
        Assert.Equal(1200m, (decimal?)lines[1]!["LineAmount"]);
        var tracking = lines[0]!["Tracking"]!.AsArray();
        Assert.Equal((SitesId, "Woodhouse"), ((string?)tracking[0]!["TrackingCategoryID"], (string?)tracking[0]!["Option"]));
        Assert.Equal((CostCodeId, "SUB-GWK"), ((string?)tracking[1]!["TrackingCategoryID"], (string?)tracking[1]!["Option"]));

        Assert.Equal("AUTHORISED", result.Status);
        Assert.Equal("NONE", result.TaxType);
        Assert.Equal((3200m, 0m, 3200m), (result.SubTotal, result.TotalTax, result.Total));
        Assert.Equal(new[] { "new-1", "new-2" }, result.Lines.Select(line => line.LineItemId));
        Assert.Equal(("Woodhouse", "SUB-BRK"), (result.Lines[1].SiteOption, result.Lines[1].CostCodeOption));
    }

    [Fact]
    public async Task AnExclusiveBillIsProRatedOnItsSubTotalWithTheVatAlongside()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 1000m, 200m, 1200m, new[] { Line("old", 1000m, 200m, "321", "INPUT2") });
        xero.Updated = xero.Bill;

        await xero.Client.RecodeBillAsync(
            new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 1m, "SUB-GWK"), ScheduleLine("b", 2m, "SUB-BRK") }),
            CancellationToken.None);

        var lines = xero.LastBody!["LineItems"]!.AsArray();
        Assert.Equal(new decimal?[] { 333.33m, 666.67m }, lines.Select(line => (decimal?)line!["LineAmount"]));
        Assert.Equal(new decimal?[] { 66.67m, 133.33m }, lines.Select(line => (decimal?)line!["TaxAmount"]));
        Assert.All(lines, line => Assert.Equal("INPUT2", (string?)line!["TaxType"]));
    }

    [Fact]
    public async Task ARecodeRefusesAPaidBillWithoutWriting()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("AUTHORISED", "Inclusive", 3200m, 0m, 3200m, new[] { Line("old", 3200m, 0m, "321", "NONE") }, paid: 3200m);

        var result = await xero.Client.RecodeBillAsync(new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 1m, "SUB-GWK") }), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.StartsWith("Bill Aug 2026 can't be recoded — it is AUTHORISED with £3,200.00 paid against it.", result.Error);
        Assert.DoesNotContain("POST Invoices/bill-1", xero.Calls);
    }

    [Fact]
    public async Task ARecodeRefusesASiteOrCostCodeOptionXeroDoesNotHold()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("old", 100m, 0m, "321", "NONE") });

        var missingSite = await xero.Client.RecodeBillAsync(
            new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 1m, "SUB-GWK", site: "Nowhere") }), CancellationToken.None);
        Assert.Equal("Xero's \"Sites\" tracking category has no option named \"Nowhere\" — check the site's Xero mapping against Xero's tracking options.", missingSite.Error);

        var missingCode = await xero.Client.RecodeBillAsync(
            new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 1m, "SUB-XYZ") }), CancellationToken.None);
        Assert.StartsWith("Xero's \"Cost Code\" tracking category has no option named \"SUB-XYZ\" — create it in Xero", missingCode.Error);
        Assert.DoesNotContain("POST Invoices/bill-1", xero.Calls);
    }

    [Fact]
    public async Task ARecodeRefusesAnEmptyOrZeroSchedule()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("old", 100m, 0m, "321", "NONE") });

        var empty = await xero.Client.RecodeBillAsync(new XeroBillCodingRequest(BillId, Array.Empty<XeroScheduleLine>()), CancellationToken.None);
        Assert.Equal("Nothing to code — the schedule has no lines.", empty.Error);
        Assert.Empty(xero.Calls);

        var zero = await xero.Client.RecodeBillAsync(new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 0m, "SUB-GWK") }), CancellationToken.None);
        Assert.Equal("The schedule's lines sum to £0.00 — nothing to split the bill across.", zero.Error);
    }

    [Fact]
    public async Task ARecodeReportsXerosRefusalInsteadOfThrowing()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("old", 100m, 0m, "321", "NONE") });
        xero.WriteStatusCode = HttpStatusCode.BadRequest;
        xero.WriteBody = """{"Elements":[{"ValidationErrors":[{"Message":"Account code 321 is not valid"}]}]}""";

        var result = await xero.Client.RecodeBillAsync(new XeroBillCodingRequest(BillId, new[] { ScheduleLine("a", 1m, "SUB-GWK") }), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Xero rejected the recode bill request with HTTP 400. Account code 321 is not valid", result.Error);
    }

    // ---- Staging a draft bill ------------------------------------------------------------------

    [Fact]
    public async Task AStagedDraftGoesToTheKnownContactWithItsDefaultTaxType()
    {
        var xero = new FakeXero();
        xero.Contacts = Contacts(("contact-1", "Adam Midgley", "NONE"));
        xero.Created = Bill("DRAFT", "Exclusive", 1600m, 0m, 1600m, new[] { Line("new-1", 1600m, 0m, "321", "NONE") });
        xero.Created["InvoiceID"] = "bill-2";

        var result = await xero.Client.CreateDraftBillAsync(
            new XeroDraftBillRequest("Adam Midgley", new DateTime(2026, 8, 31), new DateTime(2026, 9, 30), "JPMS labour Aug 2026 — Adam Midgley",
                new[] { ScheduleLine("Groundworks", 1600m, "SUB-GWK") }),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "POST identity", "GET TrackingCategories", "GET Contacts?where=Name==\"Adam Midgley\"", "PUT Invoices" }, xero.Calls);
        var payload = xero.LastBody!;
        Assert.Equal("ACCPAY", (string?)payload["Type"]);
        Assert.Equal("contact-1", (string?)payload["Contact"]!["ContactID"]);
        Assert.Equal(("2026-08-31", "2026-09-30"), ((string?)payload["Date"], (string?)payload["DueDate"]));
        Assert.Equal("JPMS labour Aug 2026 — Adam Midgley", (string?)payload["Reference"]);
        Assert.Equal(("DRAFT", "Exclusive"), ((string?)payload["Status"], (string?)payload["LineAmountTypes"]));
        var line = payload["LineItems"]!.AsArray().Single()!;
        Assert.Equal((1600m, "321", "NONE"), ((decimal?)line["LineAmount"], (string?)line["AccountCode"], (string?)line["TaxType"]));
        Assert.Null(line["TaxAmount"]);
        Assert.Equal("bill-2", result.FreshStatus);
        Assert.Equal("Tax type NONE from the contact's default. Staged net £1,600.00, VAT £0.00, total £1,600.00.", result.Note);
    }

    [Fact]
    public async Task AStagedDraftFallsBackToTheContactsLastBillsTaxType()
    {
        var xero = new FakeXero();
        xero.Contacts = Contacts(("contact-1", "Adam Midgley", null));
        xero.ContactBills = new[]
        {
            Bill("VOIDED", "Exclusive", 100m, 20m, 120m, new[] { Line("v", 100m, 20m, "321", "INPUT2") }),
            Bill("AUTHORISED", "Exclusive", 100m, 0m, 100m, new[] { Line("a", 100m, 0m, "321", "NONE") }),
        };
        xero.Created = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("new-1", 100m, 0m, "321", "NONE") });

        var result = await xero.Client.CreateDraftBillAsync(DraftRequest(), CancellationToken.None);

        Assert.Equal("NONE", (string?)xero.LastBody!["LineItems"]![0]!["TaxType"]);
        Assert.StartsWith("Tax type NONE from the contact's most recent bill (Aug 2026, 25 Aug 2026) — the contact has no default set.", result.Note);
        Assert.Contains("GET Invoices?where=Type==\"ACCPAY\"&&Contact.ContactID==Guid(\"contact-1\")", xero.Calls);
    }

    [Fact]
    public async Task AStagedDraftForAnUnknownContactNamesItAndLeavesTheTaxTypeToXero()
    {
        var xero = new FakeXero();
        xero.Contacts = Contacts();
        xero.Created = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("new-1", 100m, 0m, "321", null) });

        var result = await xero.Client.CreateDraftBillAsync(DraftRequest(), CancellationToken.None);

        Assert.Equal("Adam Midgley", (string?)xero.LastBody!["Contact"]!["Name"]);
        Assert.Null(xero.LastBody["LineItems"]![0]!["TaxType"]);
        Assert.StartsWith("Xero has no contact named \"Adam Midgley\" — one was created with the bill, and Xero's account default tax type applied", result.Note);
    }

    [Fact]
    public async Task AStagedDraftStillGoesInWhenTheContactCannotBeRead()
    {
        var xero = new FakeXero { ContactsStatusCode = HttpStatusCode.Forbidden };
        xero.Created = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("new-1", 100m, 0m, "321", null) });

        var result = await xero.Client.CreateDraftBillAsync(DraftRequest(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.StartsWith("Couldn't read the contact's tax type (Xero rejected the contacts request with HTTP 403.", result.Note);
    }

    // ---- Site tracking on named lines --------------------------------------------------------

    [Fact]
    public async Task SettingSiteTrackingReplacesOnlyTheSitesEntryOfTheNamedLines()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("AUTHORISED", "Exclusive", 300m, 60m, 360m, new[]
        {
            Line("l1", 100m, 20m, "321", "INPUT2", "Old site", "SUB-GWK"),
            Line("l2", 200m, 40m, "321", "INPUT2", "Old site", "SUB-BRK"),
        });

        var result = await xero.Client.SetSiteTrackingAsync(
            new XeroSiteTrackingRequest(BillId, false, new[] { new XeroSiteTrackingLine("l1", "Woodhouse") }), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("AUTHORISED", result.FreshStatus);
        var lines = xero.LastBody!["LineItems"]!.AsArray();
        Assert.Null(xero.LastBody["Status"]);
        var retargeted = lines[0]!["Tracking"]!.AsArray();
        Assert.Equal((SitesId, "Woodhouse"), ((string?)retargeted[0]!["TrackingCategoryID"], (string?)retargeted[0]!["Option"]));
        Assert.Equal((CostCodeId, "SUB-GWK"), ((string?)retargeted[1]!["TrackingCategoryID"], (string?)retargeted[1]!["Option"]));
        var untouched = lines[1]!["Tracking"]!.AsArray();
        Assert.Equal("Old site", (string?)untouched[0]!["Option"]);
        Assert.Equal("l2", (string?)lines[1]!["LineItemID"]);
    }

    [Fact]
    public async Task SettingSiteTrackingSkipsAPaidBillAndRefusesAVanishedLine()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("PAID", "Exclusive", 100m, 0m, 100m, new[] { Line("l1", 100m, 0m, "321", "NONE") });
        var paid = await xero.Client.SetSiteTrackingAsync(new XeroSiteTrackingRequest(BillId, false, new[] { new XeroSiteTrackingLine("l1", "Woodhouse") }), CancellationToken.None);
        Assert.True(paid.Succeeded);
        Assert.True(paid.AlreadyApproved);

        xero.Bill = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("l1", 100m, 0m, "321", "NONE") });
        var vanished = await xero.Client.SetSiteTrackingAsync(new XeroSiteTrackingRequest(BillId, false, new[] { new XeroSiteTrackingLine("gone", "Woodhouse") }), CancellationToken.None);
        Assert.Equal("The bill's lines have changed in Xero since they were synced (1 line(s) no longer exist). Sync from Xero and try again.", vanished.Error);
    }

    // ---- Approval ------------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalStampsTrackingSplitsSharedLinesAndAuthorises()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("DRAFT", "Exclusive", 300m, 60m, 360m, new[]
        {
            Line("l1", 100m, 20m, "321", "INPUT2"),
            Line("l2", 200m, 40m, "321", "INPUT2"),
            Line("l3", 50m, 10m, "429", "INPUT2"),
        });

        var result = await xero.Client.ApproveInvoiceAsync(new XeroApprovalRequest(BillId, false, new[]
        {
            new XeroApprovalLineInstruction("l1", new[] { new XeroApprovalShare("Woodhouse", "SUB-GWK", 100m) }),
            new XeroApprovalLineInstruction("l2", new[] { new XeroApprovalShare("Woodhouse", "SUB-GWK", 150m), new XeroApprovalShare("Woodhouse", "SUB-BRK", 50m) }),
        }), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("AUTHORISED", (string?)xero.LastBody!["Status"]);
        var lines = xero.LastBody["LineItems"]!.AsArray();
        Assert.Equal(4, lines.Count);
        Assert.Equal("l1", (string?)lines[0]!["LineItemID"]);
        Assert.Null(lines[1]!["LineItemID"]);
        Assert.Equal(new decimal?[] { 150m, 50m }, new[] { (decimal?)lines[1]!["LineAmount"], (decimal?)lines[2]!["LineAmount"] });
        Assert.Equal(new decimal?[] { 30m, 10m }, new[] { (decimal?)lines[1]!["TaxAmount"], (decimal?)lines[2]!["TaxAmount"] });
        Assert.Equal("Bill line [SUB-BRK]", (string?)lines[2]!["Description"]);
        Assert.Equal("l3", (string?)lines[3]!["LineItemID"]);
        Assert.Null(lines[3]!["Tracking"]);
    }

    [Fact]
    public async Task ApprovalIsANoOpOnAnAuthorisedBillAndRefusesADriftedLine()
    {
        var xero = new FakeXero();
        xero.Bill = Bill("AUTHORISED", "Exclusive", 100m, 0m, 100m, new[] { Line("l1", 100m, 0m, "321", "NONE") });
        var already = await xero.Client.ApproveInvoiceAsync(Approval("l1", 100m), CancellationToken.None);
        Assert.True(already.AlreadyApproved);
        Assert.DoesNotContain("POST Invoices/bill-1", xero.Calls);

        xero.Bill = Bill("DRAFT", "Exclusive", 100m, 0m, 100m, new[] { Line("l1", 100m, 0m, "321", "NONE") });
        var drifted = await xero.Client.ApproveInvoiceAsync(Approval("l1", 90m), CancellationToken.None);
        Assert.Equal("Line \"Bill line\" is 100.00 net in Xero but was allocated as 90.00 — the bill has changed since it was synced. Sync from Xero and re-allocate.", drifted.Error);
    }

    // ---- Builders ---------------------------------------------------------------------------------

    private static XeroApprovalRequest Approval(string lineId, decimal net) =>
        new(BillId, false, new[] { new XeroApprovalLineInstruction(lineId, new[] { new XeroApprovalShare("Woodhouse", "SUB-GWK", net) }) });

    private static XeroDraftBillRequest DraftRequest() =>
        new("Adam Midgley", new DateTime(2026, 8, 31), new DateTime(2026, 9, 30), "JPMS labour Aug 2026 — Adam Midgley",
            new[] { ScheduleLine("Groundworks", 100m, "SUB-GWK") });

    private static XeroScheduleLine ScheduleLine(string description, decimal net, string costCode, string site = "Woodhouse") =>
        new(description, net, "321", site, costCode);

    private static JsonObject Line(string id, decimal amount, decimal tax, string account, string? taxType, string? site = null, string? costCode = null)
    {
        var line = new JsonObject
        {
            ["LineItemID"] = id, ["Description"] = "Bill line", ["Quantity"] = 1m, ["UnitAmount"] = amount,
            ["LineAmount"] = amount, ["TaxAmount"] = tax, ["AccountCode"] = account,
        };
        if (taxType is not null) line["TaxType"] = taxType;
        var tracking = new JsonArray();
        if (site is not null) tracking.Add(new JsonObject { ["Name"] = "Sites", ["Option"] = site, ["TrackingCategoryID"] = SitesId });
        if (costCode is not null) tracking.Add(new JsonObject { ["Name"] = "Cost Code", ["Option"] = costCode, ["TrackingCategoryID"] = CostCodeId });
        if (tracking.Count > 0) line["Tracking"] = tracking;
        return line;
    }

    private static JsonObject Bill(string status, string lineAmountTypes, decimal subTotal, decimal tax, decimal total, JsonObject[] lines, decimal paid = 0m) =>
        new()
        {
            ["InvoiceID"] = BillId, ["Status"] = status, ["InvoiceNumber"] = "Aug 2026", ["Reference"] = "Labour",
            ["Contact"] = new JsonObject { ["ContactID"] = "contact-1", ["Name"] = "Adam Midgley" },
            ["DateString"] = "2026-08-25T00:00:00", ["LineAmountTypes"] = lineAmountTypes,
            ["SubTotal"] = subTotal, ["TotalTax"] = tax, ["Total"] = total, ["AmountPaid"] = paid, ["AmountCredited"] = 0m,
            ["AmountDue"] = total - paid, ["LineItems"] = new JsonArray(lines),
        };

    private static JsonObject Contacts(params (string Id, string Name, string? PayableTaxType)[] contacts) =>
        new()
        {
            ["Contacts"] = new JsonArray(contacts.Select(contact =>
            {
                var node = new JsonObject { ["ContactID"] = contact.Id, ["Name"] = contact.Name };
                if (contact.PayableTaxType is not null) node["AccountsPayableTaxType"] = contact.PayableTaxType;
                return (JsonNode)node;
            }).ToArray()),
        };

    /// <summary>Xero as the writes see it: a token, one bill, tracking categories, a contact
    /// lookup, and every write recorded with its body.</summary>
    private sealed class FakeXero : HttpMessageHandler
    {
        public XeroClient Client { get; }
        public List<string> Calls { get; } = new();
        public JsonObject? LastBody { get; private set; }
        public JsonObject? Bill { get; set; }
        public JsonObject? Updated { get; set; }
        public JsonObject? Created { get; set; }
        public JsonObject Contacts { get; set; } = new() { ["Contacts"] = new JsonArray() };
        public JsonObject[] ContactBills { get; set; } = Array.Empty<JsonObject>();
        public HttpStatusCode BillStatusCode { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode ContactsStatusCode { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode WriteStatusCode { get; set; } = HttpStatusCode.OK;
        public string WriteBody { get; set; } = "";

        public FakeXero(bool configured = true)
        {
            var options = new XeroOptions { ClientId = configured ? "id" : null, ClientSecret = configured ? "secret" : null, CacheMinutes = 0 };
            Client = new XeroClient(new HttpClient(this), options, NullLogger<XeroClient>.Instance);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var path = Uri.UnescapeDataString(url.Replace("https://api.xero.com/api.xro/2.0/", "").Replace("https://identity.xero.com/connect/token", "identity"));
            Calls.Add($"{request.Method} {path.Split("&order=")[0]}");
            if (path != "identity" && request.Content is not null)
                LastBody = JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
            return Route(request.Method, path);
        }

        private HttpResponseMessage Route(HttpMethod method, string path)
        {
            if (path == "identity") return Json(HttpStatusCode.OK, """{"access_token":"token","expires_in":1800}""");
            if (path == "TrackingCategories") return Json(HttpStatusCode.OK, TrackingCategories());
            if (path.StartsWith("Contacts?")) return Json(ContactsStatusCode, Contacts.ToJsonString());
            if (path.StartsWith("Invoices?")) return Json(HttpStatusCode.OK, Collection("Invoices", ContactBills));
            if (method == HttpMethod.Get && path == $"Invoices/{BillId}")
                return Json(BillStatusCode, Bill is null ? Collection("Invoices") : Collection("Invoices", Bill));
            if (method == HttpMethod.Post && path == $"Invoices/{BillId}")
                return Write(Collection("Invoices", Updated ?? Bill!));
            if (method == HttpMethod.Put && path == "Invoices")
                return Write(Collection("Invoices", Created!));
            throw new InvalidOperationException($"Unexpected Xero call {method} {path}");
        }

        private HttpResponseMessage Write(string okBody) =>
            Json(WriteStatusCode, WriteStatusCode == HttpStatusCode.OK ? okBody : WriteBody);

        private static string Collection(string name, params JsonObject[] items) =>
            new JsonObject { [name] = new JsonArray(items.Select(item => (JsonNode)JsonNode.Parse(item.ToJsonString())!).ToArray()) }.ToJsonString();

        private static string TrackingCategories() => new JsonObject
        {
            ["TrackingCategories"] = new JsonArray(
                Category(SitesId, "Sites", "Woodhouse", "Abbot Road"),
                Category(CostCodeId, "Cost Code", "SUB-GWK", "SUB-BRK")),
        }.ToJsonString();

        private static JsonObject Category(string id, string name, params string[] options) => new()
        {
            ["TrackingCategoryID"] = id, ["Name"] = name,
            ["Options"] = new JsonArray(options.Select(option => (JsonNode)new JsonObject { ["Name"] = option, ["TrackingOptionID"] = $"{id}:{option}" }).ToArray()),
        };

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
