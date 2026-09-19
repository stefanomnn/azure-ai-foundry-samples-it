using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Files;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureAiSamples
{
    /// <summary>
    /// richiama un modello LLM con il tool CODE INTERPRETER (che permette di eseguire codice python e generare file)
    /// mostra anche come scaricare i file generati dal modello
    /// </summary>
    internal class RunModelWithCodeInterpreterTool
    {
        
        private OpenAIClient _OpenAIClient;

        public RunModelWithCodeInterpreterTool(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelWithCodeInterpreterTool FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelWithCodeInterpreterTool(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelWithCodeInterpreterTool FromApiKey(string openAiURL, string password)
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
            return new RunModelWithCodeInterpreterTool(openAiClient);
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
             
            
            // creiamo il response client
            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            // creiamo il prompt che vogliamo dare al modello
            string prompt = @"dato questo array
                [
                { ""skill"": ""c#"", valore : 10 }
                , { ""skill"": ""sql server"", valore : 10 }
                , { ""skill"": ""azure"", valore : 6 }
                ]
                mi crei un diagramma a barre ? mettimi le label dello skill orientate in verticale.
                Voglio una immagine scaricabile come parte dell'output.
                ";

            //  Creiamo la richiesta passando ESPLICITAMENTE il tool code_interpreter
            //    Questo è il punto chiave: il tool lo passiamo NOI, non è pre-configurato nell'agente
            var createOptions = new CreateResponseOptions
            {
                Model = modelId, // Usiamo il modello direttamente
                BackgroundModeEnabled = true, // Non bloccante, facciamo polling
                Tools =
                {
                    // Passiamo ESPLICITAMENTE il code interpreter tool
                    // Usiamo un container automatico (senza file pre-caricati)
                    ResponseTool.CreateCodeInterpreterTool(
                        new CodeInterpreterToolContainer(
                            CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration([])
                        )
                    )
                },
                InputItems =
                {
                    (ResponseItem)ResponseItem.CreateUserMessageItem(prompt)
                }
            };

            Console.WriteLine($"🚀 Invio richiesta al modello {modelId} con tool code_interpreter...");

            

          

            var res=await responsesClient.CreateResponseAsync(createOptions);

            // 5. Polling finché il modello non ha finito (il code interpreter può impiegarci qualche secondo)
            while (res.Value.Status != ResponseStatus.Completed)
            {
                Console.WriteLine($"⏳ Il modello sta ancora elaborando (status: {res.Value.Status})... riprovo fra 2 sec.");
                await Task.Delay(2000);
                res = await responsesClient.GetResponseAsync(res.Value.Id);
            }

            Console.WriteLine("✅ Elaborazione completata!");

            AnalyzeResponse(res.Value);

            // 7. Cerchiamo eventuali file generati (il grafico a barre) e li scarichiamo
            var items = res.Value.OutputItems.ToList();
            bool fileScaricato = false;

            foreach (var outputItem in items)
            {
                var messageResponseItem = outputItem as MessageResponseItem;
                if (messageResponseItem == null || messageResponseItem.Content.Count == 0)
                {
                    continue;
                }

                // Cerchiamo annotazioni di file nell'output
                var fileAnnotation = messageResponseItem.Content
                    .SelectMany(c => c.OutputTextAnnotations)
                    .Where(ann => ann is ContainerFileCitationMessageAnnotation)
                    .Cast<ContainerFileCitationMessageAnnotation>()
                    .FirstOrDefault();

                if (fileAnnotation == null)
                {
                    continue;
                }

                // Scarichiamo il file generato
                Console.WriteLine($"📎 Trovato file generato: {fileAnnotation.FileId}");
                var containerClient = _OpenAIClient.GetContainerClient();
               
                var downloadResult = await containerClient.DownloadContainerFileAsync(
                    fileAnnotation.ContainerId,
                    fileAnnotation.FileId
                );

                
                var fileContentArray = downloadResult.Value.ToArray();
                string outputPath = "c:\\temp\\grafico-barre-da-modello.png";
                await File.WriteAllBytesAsync(outputPath, fileContentArray);
                Console.WriteLine($"💾 File salvato in: {outputPath}");
                fileScaricato = true;
            }

            if (!fileScaricato)
            {
                Console.WriteLine("⚠️ Nessun file generato trovato nell'output. Il modello potrebbe aver restituito solo testo.");
            }

       
        }
    }
}