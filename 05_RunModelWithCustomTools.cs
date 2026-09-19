using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    internal class RunModelWithCustomTools
    {
        // ── Implementazione fittizia della funzione ─────────────────────
        private static string GetWeather(string city)
        {
            var weatherData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["milano"] = "🌧️ Pioggia, 12°C, umidità 85%",
                ["roma"] = "☀️ Soleggiato, 22°C, umidità 45%",
                ["napoli"] = "⛅ Parzialmente nuvoloso, 18°C, umidità 60%",
                ["torino"] = "🌫️ Nebbia, 8°C, umidità 90%",
                ["palermo"] = "☀️ Soleggiato, 25°C, umidità 40%",
            };

            return weatherData.TryGetValue(city, out var weather)
                ? weather
                : $"🌤️ Sereno, 15°C (dati stimati per {city})";
        }

        private OpenAIClient _OpenAIClient;

        public RunModelWithCustomTools(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelWithCustomTools FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelWithCustomTools(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelWithCustomTools FromApiKey(string openAiURL, string password)
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
            return new RunModelWithCustomTools(openAiClient);
        }


        public async Task Run(string modelId)
        {
           

            // 2. Creiamo un ResponsesClient per il modello RAW (NON per un agent!)
            //    Nota: NON usiamo GetProjectResponsesClientForAgent() ma il client standard
            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            

            // Definizione del tool function per ResponsesClient
            var weatherTool = ResponseTool.CreateFunctionTool(
                functionName: "get_weather",
                strictModeEnabled:true,
                
                functionDescription: "Restituisce le condizioni meteo correnti per una città italiana",
                functionParameters: BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        city = new
                        {
                            type = "string",
                            description = "Il nome della città italiana, es. 'Milano', 'Roma'"
                        }
                    },
                    required = new[] { "city" },
                    additionalProperties = false
                })
               
            );

            CreateResponseOptions options = new()
            {
                Tools = { weatherTool },

                // costringe il modello a usare sempre il tool.
                // alternative, da ricordare per l'esame ai-103:
                // AUTO : l'llm decide se usare il tool o meno (default)
                // REQUIRED : il tool deve essere usato forzatamente dall'LLM, almeno una volta
                // NONE: il tool non viene usato
                ToolChoice = ResponseToolChoice.CreateRequiredChoice(), 
                Model = modelId,
                Instructions= "Sei un assistente meteo. Quando l'utente chiede il meteo di una città, usa la funzione get_weather per ottenere i dati. Rispondi sempre in italiano.",
                InputItems =
                    {
                        ResponseItem.CreateUserMessageItem("Che tempo fa a Milano? E a Palermo?")
                    }
            };
            

            Console.WriteLine("🚀 Invio richiesta al modello con ResponsesClient e function calling...");
            Console.WriteLine("📝 Domanda: Che tempo fa a Milano? E a Palermo?\n");

            // 3. Prima chiamata al modello
            var response = await responsesClient.CreateResponseAsync(options);

            List<ResponseItem> items = new();
            // 4. Gestione del ciclo di tool call
            while (true)
            {
                bool hasToolCall = false;

                foreach (var item in response.Value.OutputItems)
                {
                    // Accodiamo l'output del modello alla cronologia della conversazione
                    items.Add(item);

                    if (item is FunctionCallResponseItem call && call.FunctionName == "get_weather")
                    {
                        hasToolCall = true;
                        using JsonDocument argsDoc = JsonDocument.Parse(call.FunctionArguments);
                        string city = argsDoc.RootElement.GetProperty("city").GetString()!;

                        Console.WriteLine($"🔧 Il modello richiede: get_weather(\"{city}\")");

                        string weatherResult = GetWeather(city);
                        Console.WriteLine($"   Risultato: {weatherResult}");

                        // Creazione corretta dell'item di risposta alla funzione
                        items.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, weatherResult));
                    }
                }

                // Se non ci sono state richieste di funzioni, il ciclo termina
                if (!hasToolCall)
                {
                    break;
                }

                // 4. Inviamo nuovamente la richiesta aggiornata con i risultati del tool
                options = new()
                {
                    Model = modelId,
                    Tools = { weatherTool }
                };

                foreach (var i in items)
                {
                    options.InputItems.Add(i);
                }

                response = await responsesClient.CreateResponseAsync(options);
            }

            // 6. Risposta finale
            string finalResponse = response.Value.GetOutputText();
            Console.WriteLine($"\n🤖 Risposta finale del modello:\n{finalResponse}");
        }
    }
}