using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.IO;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    /// <summary>
    /// mostra come fare una chiamata con contenuto di input misto, ovvero testo e immagine.
    /// Invia l'immagine "inferno_incipit.png" e chiede al modello quali sono le dimensioni
    /// </summary>
    internal class RunModelSampleMixedInput
    {
        private readonly OpenAIClient _OpenAIClient;

        public RunModelSampleMixedInput(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelSampleMixedInput FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelSampleMixedInput(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelSampleMixedInput FromApiKey(string openAiURL, string password)
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
            return new RunModelSampleMixedInput(openAiClient);
        }


        public async Task Run(string modelId, string imgFilePath)
        {

            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            // carica immagine da locale e ci associa un URI in base64 per inviarla al modello
            // attenzione: il modello deve supportare input misto, altrimenti non funzionerà. es. deepseek non lo supporta
            byte[] imageBytes = File.ReadAllBytes(imgFilePath);
            string base64String = Convert.ToBase64String(imageBytes);
            Uri imageUri = new Uri($"data:image/png;base64,{base64String}");

            // 2. Crea il messaggio utente strutturato.
            // l'input di un messaggio misto è un array di parti, dove ogni parte può essere testo o immagine.
            MessageResponseItem userMessage = ResponseItem.CreateUserMessageItem(new[]
            {
                    ResponseContentPart.CreateInputTextPart("che dimensioni ha questa immagine?"),
                    ResponseContentPart.CreateInputImagePart(imageUri)
            });

            Console.WriteLine($"🚀 Invio richiesta al modello {modelId}: che dimensioni ha questa immagine? (segue binary dell'imagine)");
            var res = await responsesClient.CreateResponseAsync(modelId, new[] { userMessage });

            string responseText = res.Value.GetOutputText();
            Console.WriteLine($"📝 Output testuale del modello:\n{responseText}");
            Console.WriteLine($"📝 Tokens: Input={res.Value.Usage.InputTokenCount}, Output={res.Value.Usage.OutputTokenCount}");


        }
    }
}