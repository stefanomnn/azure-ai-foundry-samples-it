using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Threading.Tasks;

namespace AzureAiSamples
{

    /// <summary>
    /// esempio di conversationclient, per poter salvare lo stato della conversazione e poterla riprendere in un secondo momento
    /// </summary>
    internal class RunModelUsingConversationsProtocol
    {
        private readonly string _FoundryProjectUrl;
        private readonly string _TenantId;

        public RunModelUsingConversationsProtocol(string foundryProjectUrl,string tenantId)
        {
            _FoundryProjectUrl = foundryProjectUrl;
            _TenantId = tenantId;
        }


        public async Task Run(string modelId)
        {



            var projectClient = new AIProjectClient(
                new Uri(_FoundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = _TenantId
                }));


            ProjectOpenAIClient openAiClient = projectClient.GetProjectOpenAIClient();
            ProjectConversationsClient conversationClient = openAiClient.GetProjectConversationsClient();
            ClientResult<ProjectConversation> conversation = conversationClient.CreateProjectConversation();

            ProjectResponsesClient responsesClient = openAiClient.GetProjectResponsesClientForModel(modelId, conversation.Value.Id);
            ClientResult<ResponseResult> response = await responsesClient.CreateResponseAsync("Ciao, ricordati che mi chiamo stefano");



            Console.WriteLine();
            Console.WriteLine("Output dell'Agent al prompt 'mi chiamo stefano':");
            Console.WriteLine(response.Value.GetOutputText());

            //grazie al supporto della conversation, llm saprà come mi chiamo, anche se non specifico il lastResponseId
            ClientResult<ResponseResult> response2 = await responsesClient.CreateResponseAsync("come mi chiamo?");

            Console.WriteLine();
            Console.WriteLine("Output dell'Agent al prompt 'come mi chiamo?':");
            Console.WriteLine(response2.Value.GetOutputText());


            Console.WriteLine();
            Console.WriteLine("recupero elenco completo dei messaggi...");
            // Recupero dei messaggi/oggetti all'interno di una conversazione esistente

            var messsages = conversationClient.GetProjectConversationItemsAsync(conversation.Value.Id);
            await foreach (var message in messsages)
            {
                ResponseItem responseItem = message;
            }



        }
    }


}
