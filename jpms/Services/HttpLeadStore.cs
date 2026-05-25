using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpLeadStore : ILeadStore
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<Lead> cachedLeads = Array.Empty<Lead>();
    private bool hasLoadedLeads;

    public HttpLeadStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Lead> All()
    {
        if (!hasLoadedLeads) _ = LoadLeadsAsync();
        return cachedLeads;
    }

    public Lead? Find(string leadId) =>
        cachedLeads.FirstOrDefault(lead =>
            string.Equals(lead.LeadId, leadId, StringComparison.OrdinalIgnoreCase));

    public Lead Upsert(Lead lead)
    {
        _ = PostAndRefreshLeadsAsync("/api/leads", lead);
        return lead;
    }

    private async Task LoadLeadsAsync()
    {
        hasLoadedLeads = true;
        try { cachedLeads = (await httpClient.GetFromJsonAsync<List<Lead>>("/api/leads"))?.AsReadOnly() ?? (IReadOnlyList<Lead>)Array.Empty<Lead>(); OnChange?.Invoke(); }
        catch { cachedLeads = Array.Empty<Lead>(); }
    }

    private async Task PostAndRefreshLeadsAsync<T>(string url, T body)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        await LoadLeadsAsync();
    }

    private T? CachedScalar<T>(Dictionary<string, T?> cache, string key, string url) where T : class
    {
        if (!cache.ContainsKey(key)) _ = LoadScalarAsync(cache, key, url);
        return cache.TryGetValue(key, out var value) ? value : null;
    }

    private IReadOnlyList<T> CachedList<T>(Dictionary<string, IReadOnlyList<T>> cache, string key, string url)
    {
        if (!cache.ContainsKey(key)) _ = LoadListAsync(cache, key, url);
        return cache.TryGetValue(key, out var list) ? list : Array.Empty<T>();
    }

    private async Task LoadScalarAsync<T>(Dictionary<string, T?> cache, string key, string url) where T : class
    {
        try { cache[key] = await httpClient.GetFromJsonAsync<T?>(url); OnChange?.Invoke(); }
        catch { cache[key] = null; }
    }

    private async Task LoadListAsync<T>(Dictionary<string, IReadOnlyList<T>> cache, string key, string url)
    {
        try { cache[key] = (await httpClient.GetFromJsonAsync<List<T>>(url))?.AsReadOnly() ?? (IReadOnlyList<T>)Array.Empty<T>(); OnChange?.Invoke(); }
        catch { cache[key] = Array.Empty<T>(); }
    }

    private async Task PostScalarAsync<T>(string url, T body, string key, Dictionary<string, T?> cache, string refreshUrl) where T : class
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        cache.Remove(key);
        await LoadScalarAsync(cache, key, refreshUrl);
    }

    private async Task PostListAsync<T>(string url, T body, string key, Dictionary<string, IReadOnlyList<T>> cache, string refreshUrl)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        cache.Remove(key);
        await LoadListAsync(cache, key, refreshUrl);
    }
}
