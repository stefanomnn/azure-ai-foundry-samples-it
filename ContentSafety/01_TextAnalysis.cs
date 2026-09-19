using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 01: Text Analysis (REST API)
/// 
/// OVERVIEW:
/// L''API Analyze Text scansiona il testo per rilevare contenuti dannosi in quattro
/// categorie: Hate (odio), Sexual (contenuto sessuale), Violence (violenza),
/// SelfHarm (autolesionismo). Ogni categoria restituisce un livello di severita'
/// su scala 0-6 (FourSeverityLevels): 0=Safe, 2=Low, 4=Medium, 6=High.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/text:analyze?api-version=2024-09-15-preview
/// 
/// REQUEST BODY:
///   {
///     "text": "string",
///     "categories": ["Hate","Sexual","Violence","SelfHarm"],
///     "outputType": "FourSeverityLevels"
///   }
/// 
/// RESPONSE:
///   {
///     "categoriesAnalysis": [
///       { "category": "Hate", "severity": 0 },
///       { "category": "SelfHarm", "severity": 2 },
///       ...
///     ]
///   }
/// 
/// USE CASE: Moderazione di prompt utente, chat, commenti, recensioni.
/// </summary>
public   class TextAnalysisSample
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public TextAnalysisSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }

    public  async Task Run()
    {
      
       // string textToAnalyze = "You are an idiot! I hate everything about you. I'm going to hurt myself because nobody cares.";
        string textToAnalyze = "L'ho colpito ripetutamente alla testa con un martello fino a provocargli ferite gravissime e una forte emorragia. ho tagliato la gola e ho seppellito la testa sotto la terra. e lo farò di nuovo";

        var requestBody = new
        {
            text = textToAnalyze,
            categories = new[] { "Hate", "Sexual", "Violence", "SelfHarm" },
            outputType = "EightSeverityLevels"
        };

        string url = $"{_EndPoint}/contentsafety/text:analyze?api-version=2024-09-01";

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

            Console.WriteLine("=== TEXT ANALYSIS RESULTS ===");
            Console.WriteLine($"Text analyzed: \"{textToAnalyze}\"");
            Console.WriteLine();

            if (root.TryGetProperty("categoriesAnalysis", out JsonElement categoriesAnalysis))
            {
                Console.WriteLine("--- Categories Severity ---");
                foreach (JsonElement cat in categoriesAnalysis.EnumerateArray())
                {
                    string? category = cat.GetProperty("category").GetString();
                    int severity = cat.GetProperty("severity").GetInt32();
                    Console.WriteLine($"  {category}: Severity = {severity}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Severity scale (FourSeverityLevels): 0 = Safe, 2 = Low, 4 = Medium, 6 = High");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Text analysis failed. Message: {ex.Message}");
            throw;
        }
    }
}
