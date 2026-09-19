using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 08: Protected Material Code Detection (REST API, preview)
/// 
/// OVERVIEW:
/// L'API Protected Material for Code rileva se un codice generato da AI
/// corrisponde a codice presente in repository GitHub pubblici noti.
/// Supporta la trasparenza verso gli utenti finali e la compliance con
/// le policy organizzative. NOTA: l'indice copre repo GitHub fino al 6 Aprile 2023.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/text:detectProtectedMaterialForCode?api-version=2024-09-15-preview
/// 
/// REQUEST BODY:
///   { "code": "string" }
/// 
/// RESPONSE:
///   {
///     "protectedMaterialAnalysis": {
///       "detected": true,
///       "codeCitations": [
///         { "license": "MIT", "sourceUrls": ["https://github.com/..."] }
///       ]
///     }
///   }
/// 
/// USE CASE: Verificare che codice generato da LLM non violi licenze open source.
/// </summary>
public   class ProtectedMaterialCodeSample
{   
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public ProtectedMaterialCodeSample (string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;

        // Esempio di codice Python comune (simile a tutorial Pygame)
        string codeToCheck = """
            import pygame
            pygame.init()
            win = pygame.display.set_mode((500, 500))
            pygame.display.set_caption("My Game")
            x = 50
            y = 50
            width = 40
            height = 60
            vel = 5
            run = True
            while run:
                pygame.time.delay(100)
                for event in pygame.event.get():
                    if event.type == pygame.QUIT:
                        run = False
                keys = pygame.key.get_pressed()
                if keys[pygame.K_LEFT] and x > vel:
                    x -= vel
                if keys[pygame.K_RIGHT] and x < 500 - width - vel:
                    x += vel
                if keys[pygame.K_UP] and y > vel:
                    y -= vel
                if keys[pygame.K_DOWN] and y < 500 - height - vel:
                    y += vel
                win.fill((0, 0, 0))
                pygame.draw.rect(win, (255, 0, 0), (x, y, width, height))
                pygame.display.update()
            pygame.quit()
            """;

        var requestBody = new { code = codeToCheck };

        string url = $"{endpoint}/contentsafety/text:detectProtectedMaterialForCode?api-version=2024-09-15-preview";

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

            Console.WriteLine("=== PROTECTED MATERIAL CODE RESULTS ===");
            Console.WriteLine($"Code length: {codeToCheck.Length} chars");
            Console.WriteLine();

            if (root.TryGetProperty("protectedMaterialAnalysis", out JsonElement analysis))
            {
                bool detected = analysis.GetProperty("detected").GetBoolean();
                Console.WriteLine($"Protected Code Detected: {detected}");

                if (analysis.TryGetProperty("codeCitations", out JsonElement citations))
                {
                    Console.WriteLine();
                    Console.WriteLine("--- Citations ---");
                    foreach (JsonElement citation in citations.EnumerateArray())
                    {
                        string? license = citation.TryGetProperty("license", out JsonElement lic) ? lic.GetString() : "N/A";
                        Console.WriteLine($"  License: {license}");

                        if (citation.TryGetProperty("sourceUrls", out JsonElement urls))
                        {
                            foreach (JsonElement sourceUrl in urls.EnumerateArray())
                                Console.WriteLine($"    URL: {sourceUrl.GetString()}");
                        }
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Protected material code detection failed. Message: {ex.Message}");
            throw;
        }
    }
}