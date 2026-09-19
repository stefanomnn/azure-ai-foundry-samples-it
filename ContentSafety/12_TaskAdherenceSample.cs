using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzureAiSamples.ContentSafety;

/// <summary>
/// Sample 12: Task Adherence (REST API, preview)
/// 
/// OVERVIEW:
/// Task Adherence verifica che l'uso di tool da parte di agenti AI sia
/// allineato con le istruzioni dell'utente e il task assegnato. Rileva
/// quando un agente intraprende azioni non autorizzate, non intenzionali
/// o premature nell'invocazione di tool che gestiscono dati utente,
/// operazioni ad alto rischio o azioni esterne.
/// 
/// ENDPOINT REST:
///   POST {endpoint}/contentsafety/agent:analyzeTaskAdherence?api-version=2025-09-15-preview
/// 
/// NOTA: Task Adherence usa api-version=2025-09-15-preview (diversa da 2024-09-15-preview).
/// 
/// REQUEST BODY:
///   {
///     "tools": [ { "type": "function", "function": { "name": "...", "description": "..." } } ],
///     "messages": [
///       { "source": "Prompt", "role": "User", "contents": "..." },
///       { "source": "Completion", "role": "Assistant", "contents": "...", "toolCalls": [...] },
///       { "source": "Completion", "role": "Tool", "toolCallId": "...", "contents": "..." }
///     ]
///   }
/// 
/// RESPONSE:
///   {
///     "taskRiskDetected": true,
///     "details": "Agent attempts to perform an action without user confirmation."
///   }
/// 
/// USE CASE: Verificare agenti AI autonomi che invocano tool critici.
/// </summary>
public   class TaskAdherenceSample
{
    private readonly HttpClient _httpClient = new();
    private readonly string _EndPoint;
    private readonly string _ApiKey;
    private const string CategoryName = "sample-custom-category";
    public TaskAdherenceSample(string contentSafetyUrl, string apiKey)
    {
        _EndPoint = contentSafetyUrl;
        _ApiKey = apiKey;
    }
    public async Task Run()
    {
        string endpoint = _EndPoint;
        string apiKey = _ApiKey;

        var requestBody = new
        {
            tools = new[]
            {
                new { type = "function", function = new { name = "get_credit_card_limit", description = "Get credit card limit of the user" } },
                new { type = "function", function = new { name = "get_car_price", description = "Get car price of a particular model" } },
                new { type = "function", function = new { name = "order_car", description = "Buy a particular car model instantaneously" } }
            },
            messages = new object[]
            {
                new { source = "Prompt", role = "User", contents = "How many cars can I buy with my credit card limit?" },
                new { source = "Completion", role = "Assistant", contents = "Getting the required information",
                      toolCalls = new[]
                      {
                          new { type = "function", function = new { name = "get_credit_card_limit", arguments = "" }, id = "call_001" },
                          new { type = "function", function = new { name = "get_car_price", arguments = "" }, id = "call_002" }
                      }
                },
                new { source = "Completion", role = "Tool", toolCallId = "call_001", contents = "50000" },
                new { source = "Completion", role = "Tool", toolCallId = "call_002", contents = "25000" },
                new { source = "Completion", role = "Assistant", contents = "Your credit limit is 50000 and the car costs 25000, so you can buy 2 cars." }
            }
        };

        string url = $"{endpoint}/contentsafety/agent:analyzeTaskAdherence?api-version=2025-09-15-preview";

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

            Console.WriteLine("=== TASK ADHERENCE RESULTS ===");
            Console.WriteLine();

            if (root.TryGetProperty("taskRiskDetected", out JsonElement riskDetected))
            {
                Console.WriteLine($"Task Risk Detected: {riskDetected.GetBoolean()}");
            }

            if (root.TryGetProperty("details", out JsonElement details))
            {
                Console.WriteLine($"Details: {details.GetString()}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Task adherence analysis failed. Message: {ex.Message}");
            throw;
        }
    }
}