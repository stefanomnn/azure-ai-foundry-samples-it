using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    /// <summary>
    /// Mostra come usare lo STREAMING delle risposte (token per token)
    /// invece di aspettare la risposta completa.
    /// Pattern fondamentale per chat UI in tempo reale.
    /// 
    /// NOTA: usa ChatClient (Chat Completions API) perché lo streaming
    /// non è disponibile su ResponsesClient nella versione 2.1.0 dell'SDK.
    /// ChatClient è destinato a essere deprecato in futuro, ma al momento
    /// rimane l'unica opzione per lo streaming token-per-token.
    /// </summary>
    internal class RunModelSampleStreaming
    {
        private OpenAIClient _OpenAIClient;

        public RunModelSampleStreaming(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelSampleStreaming FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelSampleStreaming(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelSampleStreaming FromApiKey(string openAiURL, string password)
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
            return new RunModelSampleStreaming(openAiClient);
        }



        public async Task Run(string modelId)
        {
          
           
            // 2. Otteniamo un ChatClient (Chat Completions API) che supporta lo streaming
            ChatClient chatClient = _OpenAIClient.GetChatClient(modelId);

            string prompt = "Scrivi un breve racconto di fantascienza in 5 paragrafi. Ogni paragrafo deve iniziare con il numero del paragrafo.";

            Console.WriteLine($"🚀 Invio richiesta in STREAMING al modello {modelId}...");
            Console.WriteLine("📝 I token appariranno man mano che vengono generati:\n");

            // 3. Chiamata in streaming: i token arrivano progressivamente
            AsyncCollectionResult<StreamingChatCompletionUpdate> streamingResult =
                chatClient.CompleteChatStreamingAsync(prompt);

            int chunkCount = 0;
            await foreach (StreamingChatCompletionUpdate update in streamingResult)
            {
                // Ogni update contiene uno o più "content update" con i delta di testo
                foreach (ChatMessageContentPart contentPart in update.ContentUpdate)
                {
                    Console.Write(contentPart.Text);
                }
                chunkCount++;
            }

            Console.WriteLine($"\n\n✅ Streaming completato! Chunk ricevuti: {chunkCount}");
            Console.WriteLine("--- Fine streaming ---");
        }
    }
}