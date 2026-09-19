using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 11: Custom Categories (Standard) (REST API, preview)
/// 
/// OVERVIEW:
/// Custom Categories (standard) permette di creare e addestrare categorie
/// personalizzate di contenuti su misura per il proprio dominio. Richiede
/// un blob storage Azure con dati di training (file annotazioni.jsonl).
/// 
/// ENDPOINT REST (2024-09-15-preview):
///   Creare categoria:    PATCH /text/categories/{name}
///   Addestrare:          POST  /text/categories/{name}:train
///   Ottenere status:     GET   /text/categories/{name}?version={version}
///   Elencare categorie:  GET   /text/categories
///   Analizzare testo:    POST  /text/categories/{name}:analyze
///   Eliminare:           DELETE /text/categories/{name}?version={version}
/// 
/// ATTENZIONE: il training richiede 5-10 ore. Questo sample mostra SOLO
/// le operazioni di management (CRUD) piu' l'analisi. Per il training
/// serve preparare un dataset in Azure Blob Storage.
/// 
/// USE CASE: Categorie specifiche di settore (es. "spam finanziario",
/// "bullismo scolastico", "disinformazione medica"). 
/// </summary>
public class CustomCategoriesStandardSample
{
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey; 
    private const string CategoryName = "sample-custom-category";
    public  CustomCategoriesStandardSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;
        const string apiVer = "2024-09-15-preview";

        Console.WriteLine("=== CUSTOM CATEGORIES (STANDARD) === ");
        Console.WriteLine();
        Console.WriteLine("This sample demonstrates management operations only.");
        Console.WriteLine("Full training requires an Azure Blob Storage container");
        Console.WriteLine("with a annotation file and takes 5-10 hours.");
        Console.WriteLine();

        // 1. Create/get category
        await CreateOrUpdateCategory(endpoint, apiKey, apiVer);

        // 2. List categories
        await ListCategories(endpoint, apiKey, apiVer);

        // 3. Analyze text with the custom category  
        await AnalyzeWithCategory(endpoint, apiKey, apiVer);

        // 4. Delete category
        await DeleteCategory(endpoint, apiKey, apiVer);
    }

    private   async Task CreateOrUpdateCategory(string endpoint, string apiKey, string apiVer)
    {
        Console.WriteLine("--- Create/Update Custom Category ---");
        string url = $"{endpoint}/contentsafety/text/categories/{CategoryName}?api-version={apiVer}";

        var body = new
        {
            categoryName = CategoryName,
            version = 1
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }

    private   async Task ListCategories(string endpoint, string apiKey, string apiVer)
    {
        Console.WriteLine("--- List Custom Categories ---");
        string url = $"{endpoint}/contentsafety/text/categories?api-version={apiVer}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
        Console.WriteLine($"  Response: {json.RootElement.GetRawText()}");
        Console.WriteLine();
    }

    private   async Task AnalyzeWithCategory(string endpoint, string apiKey, string apiVer)
    {
        Console.WriteLine("--- Analyze Text with Custom Category ---");
        string url = $"{endpoint}/contentsafety/text/categories/{CategoryName}:analyze?api-version={apiVer}";

        var body = new { text = "This is a sample text to check against custom category." };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
            Console.WriteLine($"  Analysis Result: {json.RootElement.GetRawText()}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"  Analysis note: {ex.Message}");
            Console.WriteLine("  (The category may not be trained yet - training requires blob storage setup and 5-10 hours)");
        }
        Console.WriteLine();
    }

    private   async Task DeleteCategory(string endpoint, string apiKey, string apiVer)
    {
        Console.WriteLine("--- Delete Custom Category ---");
        string url = $"{endpoint}/contentsafety/text/categories/{CategoryName}?api-version={apiVer}&version=1";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }
}