using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 07: Protected Material Text Detection (REST API)
/// 
/// OVERVIEW:
/// L'API Protected Material for Text rileva se un testo generato da AI contiene
/// materiale protetto da copyright, come testi di canzoni, articoli, ricette o
/// altri contenuti web noti. E' pensata per essere eseguita su completions di
/// LLM, non su prompt utente.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/text:detectProtectedMaterial?api-version=2024-09-15-preview
/// 
/// REQUEST BODY:
///   { "text": "string" }
/// 
/// RESPONSE:
///   {
///     "protectedMaterialAnalysis": { "detected": true }
///   }
/// 
/// LIMITI: max 10K caratteri, min 110 caratteri (per LLM completions).
/// 
/// USE CASE: Verificare che output LLM non contenga testi coperti da copyright.
/// </summary>
public class ProtectedMaterialTextSample
{
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public ProtectedMaterialTextSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;

        // Esempio: testo di una canzone nota (Kiss Me - Sixpence None The Richer)
        string textToCheck = """
            Kiss me out of the bearded barley
            Nightly beside the green, green grass
            Swing, swing, swing the spinning step
            You wear those shoes and I will wear that dress
            Oh, kiss me beneath the milky twilight
            Lead me out on the moonlit floor
            """;

        var requestBody = new { text = textToCheck };

        string url = $"{endpoint}/contentsafety/text:detectProtectedMaterial?api-version=2024-09-15-preview";

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

            Console.WriteLine("=== PROTECTED MATERIAL TEXT RESULTS ===");
            Console.WriteLine($"Text length: {textToCheck.Length} chars");
            Console.WriteLine();

            if (root.TryGetProperty("protectedMaterialAnalysis", out JsonElement analysis))
            {
                bool detected = analysis.GetProperty("detected").GetBoolean();
                Console.WriteLine($"Protected Material Detected: {detected}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Protected material text detection failed. Message: {ex.Message}");
            throw;
        }
    }
}