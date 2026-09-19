using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 04: Blocklist Management (REST API)
/// 
/// OVERVIEW:
/// Gestione completa (CRUD) delle blocklist personalizzate tramite REST API.
/// Le blocklist permettono di definire termini specifici da flaggare durante
/// l'analisi del testo, complementari alle categorie standard di moderazione.
/// 
/// OPERAZIONI DISPONIBILI:
///   A) Create/Update blocklist  - PATCH /text/blocklists/{name}
///   B) Add items                - POST  /text/blocklists/{name}:addOrUpdateBlocklistItems
///   C) List all blocklists      - GET   /text/blocklists
///   D) List items in blocklist  - GET   /text/blocklists/{name}/blocklistItems
///   E) Remove items             - POST  /text/blocklists/{name}:removeBlocklistItems
///   F) Delete blocklist         - DELETE /text/blocklists/{name}
/// 
/// API VERSION: 2024-09-15-preview
/// 
/// USE CASE: Filtrare linguaggio specifico del dominio, PII, competitor names.
/// </summary>
public   class BlocklistManagementSample
{
    
    private const string BlocklistName = "test-blocklist";

    private readonly HttpClient _httpClient = new();


    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public BlocklistManagementSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }

    public   async Task Run()
    {
        string endpoint= _EndPoint;
        string apiKey = _ApiKey;
       
        await CreateBlocklist(endpoint, apiKey);
        await AddItems(endpoint, apiKey);
        await ListAll(endpoint, apiKey);
        await ListItems(endpoint, apiKey);
        await RemoveOne(endpoint, apiKey);
        await DeleteIt(endpoint, apiKey);
    }

    private   async Task CreateBlocklist(string endpoint, string apiKey)
    {
        Console.WriteLine("=== A) Create Blocklist ===");
        string url = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}?api-version=2024-09-15-preview";
        var body = new { description = "Test blocklist for sample" };
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }

    private   async Task AddItems(string endpoint, string apiKey)
    {
        Console.WriteLine("=== B) Add Items ===");
        string url = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}:addOrUpdateBlocklistItems?api-version=2024-09-15-preview";
        var body = new
        {
            blocklistItems = new[]
            {
                new { text = "badword1", description = "Offensive term 1" },
                new { text = "badword2", description = "Offensive term 2" },
                new { text = "secretcode123", description = "Internal code" }
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
        if (json.RootElement.TryGetProperty("blocklistItems", out var items))
            foreach (var item in items.EnumerateArray())
                Console.WriteLine($"  {item.GetProperty("blocklistItemId").GetString()}: {item.GetProperty("text").GetString()}");
        Console.WriteLine();
    }

    private   async Task ListAll(string endpoint, string apiKey)
    {
        Console.WriteLine("=== C) List Blocklists ===");
        string url = $"{endpoint}/contentsafety/text/blocklists?api-version=2024-09-15-preview";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
        if (json.RootElement.TryGetProperty("value", out var value))
            foreach (var bl in value.EnumerateArray())
                Console.WriteLine($"  {bl.GetProperty("blocklistName").GetString()}: {(bl.TryGetProperty("description", out var d) ? d.GetString() : "-")}");
        Console.WriteLine();
    }

    private   async Task ListItems(string endpoint, string apiKey)
    {
        Console.WriteLine($"=== D) List Items of '{BlocklistName}' ===");
        string url = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}/blocklistItems?api-version=2024-09-15-preview";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
        if (json.RootElement.TryGetProperty("value", out var value))
            foreach (var item in value.EnumerateArray())
                Console.WriteLine($"  {item.GetProperty("blocklistItemId").GetString()}: {item.GetProperty("text").GetString()}");
        Console.WriteLine();
    }

    private   async Task RemoveOne(string endpoint, string apiKey)
    {
        Console.WriteLine("=== E) Remove First Item ===");
        string listUrl = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}/blocklistItems?api-version=2024-09-15-preview";
        using var getReq = new HttpRequestMessage(HttpMethod.Get, listUrl);
        getReq.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var getResp = await _httpClient.SendAsync(getReq);
        getResp.EnsureSuccessStatusCode();
        using var getJson = await getResp.Content.ReadFromJsonAsync<JsonDocument>()!;
        string? firstId = null;
        if (getJson.RootElement.TryGetProperty("value", out var val) && val.GetArrayLength() > 0)
            firstId = val[0].GetProperty("blocklistItemId").GetString();
        if (firstId != null)
        {
            string url = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}:removeBlocklistItems?api-version=2024-09-15-preview";
            var body = new { blocklistItemIds = new[] { firstId } };
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            req.Content = JsonContent.Create(body);
            var response = await _httpClient.SendAsync(req);
            Console.WriteLine($"  Removed {firstId}. Status: {(int)response.StatusCode}");
        }
        Console.WriteLine();
    }

    private   async Task DeleteIt(string endpoint, string apiKey)
    {
        Console.WriteLine("=== F) Delete Blocklist ===");
        string url = $"{endpoint}/contentsafety/text/blocklists/{BlocklistName}?api-version=2024-09-15-preview";
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }
}