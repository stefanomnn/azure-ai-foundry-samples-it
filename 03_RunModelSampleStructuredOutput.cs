using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;

namespace AzureAiSamples
{
    /// <summary>
    /// Mostra come forzare il modello a restituire output STRUTTURATO (JSON)
    /// con uno schema preciso. Copre anche:
    /// - refusal detection (status != Completed, output vuoto)
    /// - validazione dei campi required dopo il parsing
    /// - retry automatico con prompt più rigido (max 2 tentativi)
    /// Utile per integrazioni applicative dove serve parsare automaticamente la risposta.
    /// </summary>
    internal class RunModelSampleStructuredOutput
    {
        private OpenAIClient _OpenAIClient;

        public RunModelSampleStructuredOutput(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelSampleStructuredOutput FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelSampleStructuredOutput(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelSampleStructuredOutput FromApiKey(string openAiURL, string password)
        {
            if (openAiURL.EndsWith("/"))
            {
                openAiURL = openAiURL.Substring(0, openAiURL.Length - 1);
            }
            if (!openAiURL.EndsWith("/openai/v1"))
            {
                openAiURL += "/openai/v1";
            }
            var openAiClient = new OpenAIClient(new ApiKeyCredential(password), new OpenAIClientOptions
            {
                Endpoint = new Uri(openAiURL)
            });
            return new RunModelSampleStructuredOutput(openAiClient);
        }




        // ── Schema dell'output che vogliamo dal modello ─────────────────
        private static readonly BinaryData OutputSchema = BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                persone = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            nome = new { type = "string" },
                            ruolo = new { type = "string" },
                            competenze = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            anni_esperienza = new { type = "integer" }
                        },
                        required = new[] { "nome", "ruolo", "competenze", "anni_esperienza" },
                        additionalProperties = false
                    }
                },
                progetto = new { type = "string" },
                stima_giorni = new { type = "integer" }
            },
            required = new[] { "persone", "progetto", "stima_giorni" },
            additionalProperties = false
        });

        public async Task Run(string modelId)
        {
            const int maxRetries = 2;
            var responseClient = _OpenAIClient.GetResponsesClient();

            string prompt = """
                Immagina un team di 3 persone per sviluppare un'app mobile di food delivery.
                Per ogni persona indica nome, ruolo, 2-3 competenze e anni di esperienza.
                Dai un nome al progetto e stima i giorni di sviluppo.
                """;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                string currentPrompt = attempt == 0
                    ? prompt
                    : $"IMPORTANTE: restituisci SOLO il JSON valido con TUTTI i campi richiesti (persone, progetto, stima_giorni). {prompt}";

                var opt = new CreateResponseOptions
                {
                    Model = modelId,
                    TextOptions = new ResponseTextOptions
                    {
                        TextFormat = ResponseTextFormat.CreateJsonSchemaFormat("team_progetto", OutputSchema, "Team di sviluppo e stima progetto")
                    },
                    InputItems = { ResponseItem.CreateUserMessageItem(currentPrompt) }
                };

                Console.WriteLine(attempt > 0
                    ? $"🔄 Tentativo {attempt + 1}/{maxRetries + 1}..."
                    : "🚀 Invio richiesta...");

                ClientResult<ResponseResult> response = await responseClient.CreateResponseAsync(opt);

                // ── REFUSAL DETECTION ──────────────────────────
                if (response.Value.Status != ResponseStatus.Completed)
                {
                    Console.WriteLine($"⚠️ Modello non ha completato. Status: {response.Value.Status}");
                    if (attempt < maxRetries) continue;
                    Console.WriteLine("❌ Tutti i tentativi esauriti per refusal.");
                    return;
                }

                string jsonOutput = response.Value.GetOutputText();

                if (string.IsNullOrWhiteSpace(jsonOutput))
                {
                    Console.WriteLine("⚠️ Output vuoto — possibile refusal silenzioso.");
                    if (attempt < maxRetries) continue;
                    Console.WriteLine("❌ Tutti i tentativi esauriti per output vuoto.");
                    return;
                }

                Console.WriteLine($"📝 JSON ricevuto:\n{jsonOutput}\n");

                // ── SCHEMA VALIDATION ──────────────────────────
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonOutput);
                    JsonElement root = doc.RootElement;

                    string[] requiredRoot = { "persone", "progetto", "stima_giorni" };
                    var missing = requiredRoot.Where(f => !root.TryGetProperty(f, out _)).ToList();

                    if (missing.Count > 0)
                    {
                        Console.WriteLine($"⚠️ Campi mancanti nel JSON: {string.Join(", ", missing)}");
                        if (attempt < maxRetries) continue;
                        Console.WriteLine("❌ Tutti i tentativi esauriti per schema non valido.");
                        return;
                    }

                    Console.WriteLine($"📋 Progetto: {root.GetProperty("progetto").GetString()}");
                    Console.WriteLine($"📅 Stima: {root.GetProperty("stima_giorni").GetInt32()} giorni\n");

                    Console.WriteLine("👥 Team:");
                    foreach (JsonElement persona in root.GetProperty("persone").EnumerateArray())
                    {
                        Console.WriteLine($"  - {persona.GetProperty("nome").GetString()}");
                        Console.WriteLine($"    Ruolo: {persona.GetProperty("ruolo").GetString()}");
                        Console.WriteLine($"    Esperienza: {persona.GetProperty("anni_esperienza").GetInt32()} anni");

                        Console.Write("    Competenze: ");
                        var competenze = new List<string>();
                        foreach (JsonElement comp in persona.GetProperty("competenze").EnumerateArray())
                            competenze.Add(comp.GetString()!);
                        Console.WriteLine(string.Join(", ", competenze));
                        Console.WriteLine();
                    }

                    Console.WriteLine($"📊 Usage — Input: {response.Value.Usage.InputTokenCount}, Output: {response.Value.Usage.OutputTokenCount}");
                    return;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"⚠️ JSON malformato: {ex.Message}");
                    if (attempt < maxRetries) continue;
                    Console.WriteLine($"❌ Tutti i tentativi esauriti. Ultimo JSON ricevuto:\n{jsonOutput}");
                }
            }
        }
    }
}
