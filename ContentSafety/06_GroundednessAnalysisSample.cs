using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 06: Groundedness Detection — verifica se il testo generato da LLM è supportato dalle fonti fornite.
/// API REST verificata dalla documentazione Microsoft Learn:
///   POST {endpoint}/contentsafety/text:detectGroundedness?api-version=2024-09-15-preview
/// La risposta include: ungroundedDetected, ungroundedPercentage, ungroundedDetails e (opzionale) correctionText.
/// reference:
/// https://learn.microsoft.com/it-it/rest/api/contentsafety/text-groundedness-detection-operations/detect-groundedness-options?view=rest-contentsafety-2024-02-15-preview&tabs=HTTP#analyzetextgroundednessresult
/// </summary>
public class GroundednessAnalysisSample
{
    private   readonly HttpClient _httpClient = new();

    private readonly string _EndPoint;
    private readonly string _ApiKey;

    public GroundednessAnalysisSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }

    /// <summary>
    /// Groundness con task=QnA:
    /// verifica di una risposta a domanda diretta, es. qual'è la capitale della Francia?
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public   async Task RunQNA()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;

        // Testo generato da LLM (contiene un fatto errato: Berlino anziché Parigi)
        string llmResponse = "La capitale della Francia è Berlino.";

        // Fonti di grounding: il testo corretto a cui l'LLM dovrebbe attenersi
        string[] groundingSources = { "La capitale della Francia è Parigi." };

        var requestBody = new
        {
            domain = "Generic", // alternativa: Medical
            task = "QnA",
            qna = new { query = "Qual è la capitale della Francia?" },
            text = llmResponse,
            groundingSources
        };
        /*
         * per avere come output la "spiegazione" (reason) dell'errore bisogna abilitare il reasoning,con il supporto di LLM dedicato:
            {
              "domain": "Generic",
              "task": "Summarization",
              "text": "La capitale della Francia è Berlino.",
              "groundingSources": [
                "La capitale della Francia è Parigi."
              ],
              "reasoning": true,
              "llmResource": {
                "resourceType": "AzureOpenAI",
                "azureOpenAIEndpoint": "https://tua-risorsa-openai.openai.azure.com/",
                "azureOpenAIDeploymentName": "gpt-4o"
              }
            }         
         */

        string url = $"{endpoint}/contentsafety/text:detectGroundedness?api-version=2024-09-15-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Content = JsonContent.Create(requestBody);

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using JsonDocument json = await response.Content.ReadFromJsonAsync<JsonDocument>()
                ?? throw new InvalidOperationException("Empty response body.");

            Console.WriteLine("=== GROUNDEDNESS RESULTS ===");

            JsonElement root = json.RootElement;
            /* output atteso:
                {
                    "ungroundedDetected": true,
                    "ungroundedPercentage": 1,
                    "ungroundedDetails": [
                        {
                            "text": "La capitale della Francia è Berlino.",
                            "offset": {
                                "utf8": 0,
                                "utf16": 0,
                                "codePoint": 0
                            },
                            "length": {
                                "utf8": 37,
                                "utf16": 36,
                                "codePoint": 36
                            }
                        }
                    ]
                }             
             */

            if (root.TryGetProperty("ungroundedDetected", out JsonElement ungroundedDetected))
            {
                Console.WriteLine($"Ungrounded Detected: {ungroundedDetected.GetBoolean()}");
            }

            if (root.TryGetProperty("ungroundedPercentage", out JsonElement ungroundedPct))
            {
                Console.WriteLine($"Ungrounded Percentage: {ungroundedPct.GetDouble():P0}");
            }

            if (root.TryGetProperty("ungroundedDetails", out JsonElement details) && details.GetArrayLength() > 0)
            {
                Console.WriteLine("--- Ungrounded Details ---");
                foreach (JsonElement detail in details.EnumerateArray())
                {
                    if (detail.TryGetProperty("text", out JsonElement text))
                    {
                        Console.WriteLine($"  Ungrounded text: \"{text.GetString()}\"");
                    }
                }
            }

            if (root.TryGetProperty("reason", out JsonElement reasonElement))
            {
                string? reason = reasonElement.GetString();
                if (!string.IsNullOrEmpty(reason))
                {
                    Console.WriteLine($"--- reason ---");
                    Console.WriteLine($"{reason}\"");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Groundedness analysis failed. Message: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// Groundness con task=Summarization:
    /// verifica la congruenza di un output che non viene da una domanda diretta , ma ad esempio come risultato di un riassunto
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RunSummarizationCheck()
    {   // Il documento lungo originale (la fonte di verità)
        string[] groundingSources = {
            "L'azienda Rossi S.r.l. ha chiuso il terzo trimestre con un fatturato di 2,5 milioni di euro, registrando una crescita del 15% rispetto all'anno precedente. Il settore trainante è stato quello dell'e-commerce, mentre la divisione fisica ha subito una leggera flessione del 2%. Per il prossimo anno è prevista l'assunzione di 10 nuove risorse."
        };

        // Il riassunto generato dall'LLM (che contiene un'informazione inventata/non presente nel testo: le 50 assunzioni anziché 10)
        string llmSummary = "Nel terzo trimestre la Rossi S.r.l. ha raggiunto 2,5 milioni di euro di fatturato (+15%), trainata dall'e-commerce. A causa dell'ottimo andamento, l'azienda assumerà 50 nuove risorse nel corso del prossimo anno.";

        // Configurazione della richiesta per Summarization (nota che il campo 'qna' non serve)
        var requestBody = new
        {
            domain = "Generic", // alternativa: Medical
            task = "Summarization",
            text = llmSummary,
            groundingSources
        };

        string url = $"{_EndPoint}/contentsafety/text:detectGroundedness?api-version=2024-09-15-preview";
         
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _ApiKey);
        request.Content = JsonContent.Create(requestBody);

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using JsonDocument json = await response.Content.ReadFromJsonAsync<JsonDocument>()
               ?? throw new InvalidOperationException("Risposta vuota.");
            var root = json.RootElement;
            Console.WriteLine("=== GROUNDEDNESS (SUMMARIZATION) RESULTS ===");
            /* OUTPUT ATTESO:
                    {
                      "ungroundedDetected": true,
                      "ungroundedPercentage": 0.45,
                      "ungroundedDetails": [
                        {
                          "text": "A causa dell'ottimo andamento, l'azienda assumerà 50 nuove risorse nel corso del prossimo anno.",
                          "offset": {
                            "utf8": 116,
                            "utf16": 116,
                            "codePoint": 116
                          },
                          "length": {
                            "utf8": 96,
                            "utf16": 95,
                            "codePoint": 95
                          }
                        }
                      ]
                    }             
             */

            if (root.TryGetProperty("ungroundedDetected", out JsonElement ungroundedDetected))
            {
                Console.WriteLine($"Ungrounded Detected: {ungroundedDetected.GetBoolean()}");
            }

            if (root.TryGetProperty("ungroundedPercentage", out JsonElement ungroundedPct))
            {
                Console.WriteLine($"Ungrounded Percentage: {ungroundedPct.GetDouble():P0}");
            }

            if (root.TryGetProperty("ungroundedDetails", out JsonElement details) && details.GetArrayLength() > 0)
            {
                Console.WriteLine("--- Ungrounded Details ---");
                foreach (JsonElement detail in details.EnumerateArray())
                {
                    if (detail.TryGetProperty("text", out JsonElement text))
                    {
                        Console.WriteLine($"  Ungrounded text: \"{text.GetString()}\"");
                    }
                }
            }

            if (root.TryGetProperty("reason", out JsonElement reasonElement))
            {
                string? reason = reasonElement.GetString();
                if (!string.IsNullOrEmpty(reason))
                {
                    Console.WriteLine($"--- reason ---");
                    Console.WriteLine($"{reason}\"");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante la chiamata: {ex.Message}");
        }
    }
}
