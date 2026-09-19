using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 02: Text Analysis con Blocklist (REST API)
/// 
/// OVERVIEW:
/// Estende l''analisi del testo (Sample 01) associando una o piu'' blocklist
/// personalizzate. Le blocklist contengono termini specifici da flaggare.
/// Quando un termine nella blocklist viene trovato nel testo, il risultato
/// include i dettagli del match. Opzionalmente, HaltOnBlocklistHit interrompe
/// l''analisi delle categorie di moderazione quando viene rilevato un match.
/// 
/// PREREQUISITO: eseguire prima Sample 04 per creare la blocklist di test.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/text:analyze?api-version=2024-09-15-preview
/// 
/// REQUEST BODY (con blocklist):
///   {
///     "text": "string",
///     "categories": ["Hate","Sexual","Violence","SelfHarm"],
///     "blocklistNames": ["test-blocklist"],
///     "haltOnBlocklistHit": false,
///     "outputType": "FourSeverityLevels"
///   }
/// 
/// RESPONSE:
///   {
///     "categoriesAnalysis": [...],
///     "blocklistsMatch": [
///       { "blocklistName": "test-blocklist", "blocklistItemId": "...", "blocklistItemText": "badword1" }
///     ]
///   }
/// 
/// USE CASE: Filtrare slang aziendale, dati sensibili, competitor names.
/// </summary>
public   class TextAnalysisWithBlocklistSample
{
    private   readonly HttpClient _httpClient = new();

    public const string BlocklistName = "test-blocklist";

    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public TextAnalysisWithBlocklistSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }

    public   async Task Run()
    {
       
        string textToAnalyze = "This text contains badword1 and badword2 which should be blocked.";

        var requestBody = new
        {
            text = textToAnalyze,
            categories = new[] { "Hate", "Sexual", "Violence", "SelfHarm" },
            blocklistNames = new[] { BlocklistName },
            haltOnBlocklistHit = false,
            outputType = "FourSeverityLevels"
        };

        string url = $"{_EndPoint}/contentsafety/text:analyze?api-version=2024-09-15-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _ApiKey);
        request.Content = JsonContent.Create(requestBody);

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using JsonDocument json = await response.Content.ReadFromJsonAsync<JsonDocument>()
                ?? throw new InvalidOperationException("Empty response body.");

            JsonElement root = json.RootElement;

            Console.WriteLine("=== TEXT ANALYSIS WITH BLOCKLIST ===");
            Console.WriteLine($"Text: \"{textToAnalyze}\"");
            Console.WriteLine($"Blocklist: {BlocklistName}");
            Console.WriteLine();

            if (root.TryGetProperty("categoriesAnalysis", out JsonElement categoriesAnalysis))
            {
                Console.WriteLine("--- Severity Results ---");
                foreach (JsonElement cat in categoriesAnalysis.EnumerateArray())
                {
                    string? category = cat.GetProperty("category").GetString();
                    int severity = cat.GetProperty("severity").GetInt32();
                    Console.WriteLine($"  {category}: Severity = {severity}");
                }
            }

            Console.WriteLine();
            if (root.TryGetProperty("blocklistsMatch", out JsonElement blocklistsMatch))
            {
                Console.WriteLine("--- Blocklist Match Results ---");
                foreach (JsonElement match in blocklistsMatch.EnumerateArray())
                {
                    string? name = match.GetProperty("blocklistName").GetString();
                    string? id = match.GetProperty("blocklistItemId").GetString();
                    string? text = match.GetProperty("blocklistItemText").GetString();
                    Console.WriteLine($"  Blocklist: {name}, Item ID: {id}, Text: {text}");
                }
            }
            else
            {
                Console.WriteLine("  No blocklist matches found.");
            }

            Console.WriteLine();
            Console.WriteLine("Note: Set haltOnBlocklistHit=true to skip severity analysis when blocklist matches are found.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Text analysis with blocklist failed. Message: {ex.Message}");
            throw;
        }
    }
}