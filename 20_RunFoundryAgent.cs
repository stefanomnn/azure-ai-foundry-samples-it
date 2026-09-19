using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;
using System;
using System.Threading.Tasks;

namespace AzureAiSamples
{

    /// <summary>
    /// questa classe mostra come:
    /// - creare un agent via API configurandolo
    /// - richiamare l'agent
    /// - eliminare un agent
    /// </summary>
    internal class RunFoundryAgent
    {
        private readonly string _FoundryProjectUrl;
        private readonly string _TenantId;

        private async Task<string> CreateTemporaryAgent(AIProjectClient projectClient, string modelId) {
            string agentName =
               $"temp-code-interpreter-{Guid.NewGuid():N}";

            ProjectsAgentVersion? agentVersion = null;
            var agentDefinition =
            new DeclarativeAgentDefinition(modelId)
            {
                Instructions =
                    "Se la richiesta richiede un calcolo, " +
                    "devi usare il Code Interpreter per eseguirlo. " +
                    "Non eseguire il calcolo mentalmente."
            };

            agentDefinition.Tools.Add(
                ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(
                        CodeInterpreterToolContainerConfiguration
                            .CreateAutomaticContainerConfiguration([])
                    )
                )
            );

            Console.WriteLine("Creo l'Agent temporaneo...");

            agentVersion =
                await projectClient
                    .AgentAdministrationClient
                    .CreateAgentVersionAsync(
                        agentName,
                        new ProjectsAgentVersionCreationOptions(
                            agentDefinition));

            Console.WriteLine(
                $"Agent creato: {agentVersion.Name}");

            return agentName;

        }

        public RunFoundryAgent(string foundryProjectUrl,string tenantId)
        {
            _FoundryProjectUrl = foundryProjectUrl;
            _TenantId = tenantId;
        }


        public   async Task Run(string modelId)
        {
            var c = new InteractiveBrowserCredential();
            

            var projectClient = new AIProjectClient(
                new Uri(_FoundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { 
                 TenantId= _TenantId,                    
                }));

            string agentName = null;
          
            try
            {
                agentName = await CreateTemporaryAgent(projectClient,modelId);
        
                ProjectOpenAIClient openAIClient =
                    projectClient.GetProjectOpenAIClient();

                ProjectResponsesClient responseClient =
                    openAIClient.GetProjectResponsesClientForAgent(agentName);

                Console.WriteLine();
                Console.WriteLine(
                    "Chiedo all'Agent di usare il Code Interpreter (quanto fa 5*3 ?)...");

                var response =
                    await responseClient.CreateResponseAsync("quanto fa 5*3? sii conciso nelle risposte.");

                string output =
                    response.Value.GetOutputText();

                Console.WriteLine();
                Console.WriteLine("Output dell'Agent:");
                Console.WriteLine(output);
            }
            finally
            {
                if (agentName!=null)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        "Elimino l'Agent temporaneo...");

                    await projectClient
                        .AgentAdministrationClient
                        .DeleteAgentVersionAsync( agentName:agentName, agentVersion:"1" );

                    Console.WriteLine("Agent eliminato.");
                }
            }
        }
    }


}
