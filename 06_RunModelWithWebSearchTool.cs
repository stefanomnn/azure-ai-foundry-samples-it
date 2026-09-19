using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    
    /// <summary>
    /// mostra l'utilizzo del tool WEB SEARCH che permette all' LLM di cercare su internet    
    /// </summary>
    internal class RunModelWithWebSearchTool
    {
        private OpenAIClient _OpenAIClient;

        public RunModelWithWebSearchTool(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelWithWebSearchTool FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelWithWebSearchTool(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelWithWebSearchTool FromApiKey(string openAiURL, string password)
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
            return new RunModelWithWebSearchTool(openAiClient);
        }


        private void AnalyzeResponse(ResponseResult llmResponse)
        {
            //  Leggiamo rapidamente l'output del modello 
            string responsetToInitialPrompt = llmResponse.GetOutputText();
            Console.WriteLine($"📝 Output testuale del modello:\n{responsetToInitialPrompt}");


            //token input & output
            Console.WriteLine($"📝 Tokens: Input={llmResponse.Usage.InputTokenCount}, Output={llmResponse.Usage.OutputTokenCount}");

            //analisi degli output: analizziamo l'array OutputItem cercando il primo elemento di tipo MessageResponseItem. altri possibili valori sono function call, reasoning etc. a noi però interessa la risposta vera e propria
            var responseItem = llmResponse.OutputItems.FirstOrDefault(o => o is MessageResponseItem) as MessageResponseItem;
            if (responseItem != null)
            {
                int i = 0;
                foreach (ResponseContentPart content in responseItem.Content)
                {
                    Console.WriteLine($"Messaggio[{i}].Text: {content.Text}");
                    int j = 0;
                    // le annotations sono riferimenti extra presenti nella risposta, es il nome del file da cui ha preso la risposta (se presente), o un file di output generato.
                    // le trovi nel sample relativo al codeinterpreter
                    foreach (ResponseMessageAnnotation annot in content.OutputTextAnnotations)
                    {
                        Console.WriteLine($"Messaggio[{i}.Annotations[{j}].Kind: {annot.Kind}");
                        j++;
                    }
                    i++;
                }
            }
        }


        public async Task Run(string modelId)
        {
          
           
            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            
            string prompt = @"Che tempo farà a palermo nelle prossime due ore?";
 
            var createOptions = new CreateResponseOptions
            {
                Model = modelId, // Usiamo il modello direttamente
                BackgroundModeEnabled = true, // Non bloccante, facciamo polling (ignorato sul alcuni provider). utile se il modello impiega molto tempo a fare la ricerca
                ToolChoice =ResponseToolChoice.CreateRequiredChoice(), // obbliga l'llm a chiamare il tool
                Tools =
                {                    
                    ResponseTool.CreateWebSearchTool(WebSearchToolLocation.CreateApproximateLocation(country: "IT",region: "piemonte", city: "turin"))                   
                },
                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(prompt)
                }
            };

            Console.WriteLine($"Invio richiesta al modello {modelId} con tool web search");

            ClientResult<ResponseResult> res = responsesClient.CreateResponse(createOptions);

            // 5. Polling finché il modello non ha finito (la ricerca può impiegarci qualche secondo)
            while (res.Value.Status != ResponseStatus.Completed)
            {
                Console.WriteLine($"⏳ Il modello sta ancora elaborando (status: {res.Value.Status})... riprovo fra 2 sec.");
                await Task.Delay(2000);
                res = await responsesClient.GetResponseAsync(res.Value.Id);
            }

            Console.WriteLine("✅ Elaborazione completata!");

            AnalyzeResponse(res.Value);
 
       
        }
    }
}