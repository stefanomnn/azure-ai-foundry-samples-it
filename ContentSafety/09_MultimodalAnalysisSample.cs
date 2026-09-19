using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 09: Multimodal Analysis (REST API, preview)
/// 
/// OVERVIEW:
/// L'API Multimodal analizza contenuti che combinano immagini e testo,
/// preservando il contesto per una comprensione piu' completa. Supporta
/// OCR automatico per rilevare testo nelle immagini. Le categorie sono
/// le stesse dell'analisi testo/immagine: Hate, Sexual, Violence, SelfHarm.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/imageWithText:analyze?api-version=2024-09-15-preview
/// 
/// REQUEST BODY:
///   {
///     "image": { "content": "<base64_encoded_image>" },
///     "text": "Testo associato all'immagine",
///     "categories": ["Hate","Sexual","Violence","SelfHarm"],
///     "enableOcr": true
///   }
/// 
/// RESPONSE:
///   {
///     "categoriesAnalysis": [
///       { "category": "Hate", "severity": 2 },
///       ...
///     ]
///   }
/// 
/// LIMITI: max 7200x7200 pixel, 4 MB, min 50x50. Testo max 1000 caratteri.
/// 
/// USE CASE: Moderazione di meme, post social con immagini+testo, captions.
/// </summary>
public   class MultimodalAnalysisSample
{   
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public MultimodalAnalysisSample (string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;

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
            text = "Check this image for harmful content and hate speech.",
            categories = new[] { "Hate", "Sexual", "Violence", "SelfHarm" },
            enableOcr = true
        };

        string url = $"{endpoint}/contentsafety/imageWithText:analyze?api-version=2024-09-15-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(requestBody);

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using JsonDocument json = await response.Content.ReadFromJsonAsync<JsonDocument>()
                ?? throw new InvalidOperationException("Empty response body.");

            JsonElement root = json.RootElement;

            Console.WriteLine("=== MULTIMODAL ANALYSIS RESULTS ===");
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
            Console.WriteLine("Multimodal analysis considers both image content and associated text.");
            Console.WriteLine("OCR is enabled to extract text embedded within the image.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Multimodal analysis failed. Message: {ex.Message}");
            throw;
        }
    }
}