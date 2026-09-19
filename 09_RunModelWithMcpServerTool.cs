using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    
    /// <summary>
    /// mostra l'utilizzo del tool MCP. precondizione: deve essere raggiungibile da azure!   
    /// </summary>
    internal class RunModelWithMcpServerTool
    {
        private OpenAIClient _OpenAIClient;

        public RunModelWithMcpServerTool(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelWithMcpServerTool FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelWithMcpServerTool(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelWithMcpServerTool FromApiKey(string openAiURL, string password)
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
            return new RunModelWithMcpServerTool(openAiClient);
        }

        public async Task Run(string modelId)
        {
             
           
            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            

            var mcpTool = ResponseTool.CreateMcpTool(
                // label che serve dare un nome univoco all'mcp. utile in caso ne avesse a disposizione piu di uno
                serverLabel: "ms-tool"

                //uri (deve essere raggiungible); questo mcp è offerto gratuitamente fda microsoft
                , serverUri: new Uri("https://learn.microsoft.com/api/mcp")
                
                //forza l'llm a usare il tool sempre. diversamente, avrò una risposta interemedia con cui mi verrà chiesto il permesso di usarlo
                , toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
           

            var createOptions = new CreateResponseOptions
            {
                Model = modelId, // Usiamo il modello direttamente
                BackgroundModeEnabled = true, // Non bloccante, facciamo polling
                Instructions="Usa obbligatoriamente il tool MCP che hai a disposizione per ricavare le informazioni riguardanti il mondo azure/.net",
                Tools =
                {
                    mcpTool
                },
                InputItems =
                {
                    (ResponseItem)ResponseItem.CreateUserMessageItem(@"Come mi connetto a una risorsa foundry usando c#?")
                }
            };

            Console.WriteLine($"Invio richiesta al modello {modelId} con MCP tool");

            var res=await responsesClient.CreateResponseAsync(createOptions);

            // 5. Polling finché il modello non ha finito (la ricerca può impiegarci qualche secondo)
            while (res.Value.Status != ResponseStatus.Completed)
            {
                Console.WriteLine($"⏳ Il modello sta ancora elaborando (status: {res.Value.Status})... riprovo fra 2 sec.");
                await Task.Delay(2000);
                res = await responsesClient.GetResponseAsync(res.Value.Id);
            }

            Console.WriteLine("✅ Elaborazione completata!");

            // 6. Leggiamo l'output testuale
            string responseText = res.Value.GetOutputText();
            Console.WriteLine($"📝 Output testuale del modello:\n{responseText}\n");
 
       
        }
    }
}