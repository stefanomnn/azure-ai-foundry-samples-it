using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 10: Custom Categories (Rapid) (REST API, preview)
/// 
/// OVERVIEW:
/// Custom Categories (rapid) permette di definire rapidamente pattern di
/// contenuti dannosi emergenti e scansionare testo e immagini per match.
/// Si crea un "incident", si aggiungono sample, lo si deploya,
/// e poi si esegue la detect su nuovo testo.
/// 
/// FLUSSO DI LAVORO:
///   1. Creare incident:     PATCH /text/incidents/{name}
///   2. Aggiungere sample:   POST  /text/incidents/{name}:addIncidentSamples
///   3. Deployare incident:  POST  /text/incidents/{name}:deploy
///   4. Rilevare (detect):   POST  /text:detectIncidents
///   5. Pulire:              DELETE /text/incidents/{name}
/// 
/// NOTA: questa API usa api-version=2024-02-15-preview (non 2024-09-15-preview).
/// 
/// USE CASE: Risposta rapida a nuovi trend di contenuti dannosi.
///
/// </summary>
public   class CustomCategoriesRapidSample
{
 
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;
    private const string IncidentName = "test-rapid-incident";
    public CustomCategoriesRapidSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;
        const string apiVersion = "2024-02-15-preview";

        await CreateIncident(endpoint, apiKey, apiVersion);
        await AddSamples(endpoint, apiKey, apiVersion);
        await DeployIncident(endpoint, apiKey, apiVersion);
        await DetectIncidents(endpoint, apiKey, apiVersion);
        await DeleteIncident(endpoint, apiKey, apiVersion);
    }
private   async Task CreateIncident(string endpoint, string apiKey, string apiVersion)
    {
        Console.WriteLine("=== A) Create Rapid Incident ===");
        string url = $"{endpoint}/contentsafety/text/incidents/{IncidentName}?api-version={apiVersion}";
        var body = new { incidentName = IncidentName, incidentDefinition = "Test incident for harmful content detection" };
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }

    private   async Task AddSamples(string endpoint, string apiKey, string apiVersion)
    {
        Console.WriteLine("=== B) Add Samples to Incident ===");
        string url = $"{endpoint}/contentsafety/text/incidents/{IncidentName}:addIncidentSamples?api-version={apiVersion}";
        var body = new
        {
            incidentSamples = new[]
            {
                new { text = "I want to end my life, nothing matters anymore." },
                new { text = "I feel so hopeless and worthless, I should just disappear." }
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }

    private   async Task DeployIncident(string endpoint, string apiKey, string apiVersion)
    {
        Console.WriteLine("=== C) Deploy Incident ===");
        string url = $"{endpoint}/contentsafety/text/incidents/{IncidentName}:deploy?api-version={apiVersion}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine("  Note: Deployment may take a few moments to complete.");
        Console.WriteLine();
    }

    private   async Task DetectIncidents(string endpoint, string apiKey, string apiVersion)
    {
        Console.WriteLine("=== D) Detect Incidents in New Text ===");
        string url = $"{endpoint}/contentsafety/text:detectIncidents?api-version={apiVersion}";
        var body = new { text = "I am feeling really down today and I don't want to go on." };
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(body);
        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var json = await response.Content.ReadFromJsonAsync<JsonDocument>()!;
            Console.WriteLine($"  Detection Results: {json.RootElement.GetRawText()}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"  Detect call note: {ex.Message}");
            Console.WriteLine("  (Expected if no matching incidents or deployment in progress)");
        }
        Console.WriteLine();
    }

    private   async Task DeleteIncident(string endpoint, string apiKey, string apiVersion)
    {
        Console.WriteLine("=== E) Delete Incident ===");
        string url = $"{endpoint}/contentsafety/text/incidents/{IncidentName}?api-version={apiVersion}";
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        var response = await _httpClient.SendAsync(request);
        Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine();
    }
}