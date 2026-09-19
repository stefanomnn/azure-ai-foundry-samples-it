using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 03: Image Analysis (REST API)
/// 
/// OVERVIEW:
/// L'API Analyze Image scansiona immagini per rilevare contenuti dannosi
/// nelle stesse quattro categorie del testo: Hate, Sexual, Violence, SelfHarm.
/// Supporta immagini inviate come base64 (content) o tramite blob URL.
/// Limiti: max 2048x2048 pixel, 4 MB, min 50x50 pixel.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/image:analyze?api-version=2024-09-15-preview
/// 
/// REQUEST BODY:
///   {
///     "image": { "content": "<base64_encoded_image>" },
///     "categories": ["Hate","Sexual","Violence","SelfHarm"],
///     "outputType": "FourSeverityLevels"
///   }
/// 
/// RESPONSE:
///   {
///     "categoriesAnalysis": [
///       { "category": "Hate", "severity": 0 },
///       ...
///     ]
///   }
/// 
/// USE CASE: Moderazione di immagini caricate dagli utenti, foto profilo, meme.
/// </summary>
public   class ImageAnalysisSample
{
    private readonly HttpClient _httpClient = new();

    
    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public ImageAnalysisSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }


    public   async Task Run()
    {
       
        string imagePath = Path.Combine(AppContext.BaseDirectory, "assets\\inferno_incipit.png");
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Image not found: {imagePath}");
            return;
        }

        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        string base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            image = new { content = base64Image },
            categories = new[] { "Hate", "Sexual", "Violence", "SelfHarm" },
            outputType = "FourSeverityLevels"
        };

        string url = $"{_EndPoint}/contentsafety/image:analyze?api-version=2024-09-15-preview";

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

            Console.WriteLine("=== IMAGE ANALYSIS RESULTS ===");
            Console.WriteLine($"Image: {imagePath} ({imageBytes.Length} bytes)");
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
            Console.WriteLine("Max image size: 2048x2048 pixels, 4 MB. Min: 50x50 pixels.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Image analysis failed. Message: {ex.Message}");
            throw;
        }
    }
}