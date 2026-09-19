using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAiSamples
{

    /// <summary>
    /// mostra l'utilizzo del tool FILE REFERENCE,
    /// che permette di:
    /// - precaricare dei files in un ambiente isolato (store). il file verrà automaticamente indicizzato
    /// - istruire l'LLM a cercare le risposte all'interno dei files dello store
    /// cosa si puo imparare:
    /// - come costruire uno store, con expiration policy
    /// - aggiungere un file allo store
    /// - aggiungere il tool alla chiama del modello
    /// </summary>
    internal class RunModelWithFileSearchTool
    {
        private OpenAIClient _OpenAIClient;

        public RunModelWithFileSearchTool(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static RunModelWithFileSearchTool FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new RunModelWithFileSearchTool(projectClient.GetProjectOpenAIClient());
        }

        public static RunModelWithFileSearchTool FromApiKey(string openAiURL, string password)
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
            return new RunModelWithFileSearchTool(openAiClient);
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

            var vectoreStoreClient = _OpenAIClient.GetVectorStoreClient();

            //crea uno store con una policy di autodistruzione:
            //Elimina automaticamente questo Vector Store (e tutti i file associati al suo interno) se nessuno lo utilizza (o non viene fatto accesso) per 2 giorni
            var store = await vectoreStoreClient.CreateVectorStoreAsync(new OpenAI.VectorStores.VectorStoreCreationOptions
            {
                Name = "LavatriciStore",
                ExpirationPolicy = new OpenAI.VectorStores.VectorStoreExpirationPolicy(OpenAI.VectorStores.VectorStoreExpirationAnchor.LastActiveAt, 2)
            });


            // carica il file locale sul server,
            // al file sul server verrà dato un FileId
            OpenAIFileClient fileClient = _OpenAIClient.GetOpenAIFileClient(); // oppure dal client principale
            using Stream fileStream = new FileStream("assets\\lavatrice.pdf", FileMode.Open, FileAccess.Read);
            var uploadResult = await fileClient.UploadFileAsync(
                file: fileStream,
                filename: "lavatrice.pdf",
                purpose: FileUploadPurpose.Assistants
            );
            string fileId = uploadResult.Value.Id;

            //carica il file nello store
            await vectoreStoreClient.AddFileBatchToVectorStoreAsync(store.Value.Id, new[] { fileId });


            ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

            // 3. Leggiamo il prompt dal file
            string prompt = @"che programma devo usare per lavare un capo di lana?. scrivimi il riferimento al file da cui hai trovato le informazioni
                ";

            // 4. Creiamo la richiesta passando ESPLICITAMENTE il tool file_sarch            
            var createOptions = new CreateResponseOptions
            {
                Model = modelId,
                BackgroundModeEnabled = true, // Non bloccante, facciamo polling
                Tools =
                {
                    ResponseTool.CreateFileSearchTool(new [] {store.Value.Id })

                },
                InputItems =
                {
                    (ResponseItem)ResponseItem.CreateUserMessageItem(prompt)
                }
            };

            Console.WriteLine($"Invio richiesta al modello {modelId} con tool file_search...");

            var res = await responsesClient.CreateResponseAsync(createOptions);

            // 5. Polling finché il modello non ha finito (la ricerca può impiegarci qualche secondo)
            while (res.Value.Status != ResponseStatus.Completed)
            {
                Console.WriteLine($"⏳ Il modello sta ancora elaborando (status: {res.Value.Status})... riprovo fra 2 sec.");
                await Task.Delay(2000);
                res = await responsesClient.GetResponseAsync(res.Value.Id);
            }

            Console.WriteLine("✅ Elaborazione completata!");
            AnalyzeResponse(res.Value);

            HashSet<string> filesReferences = new HashSet<string>();
            foreach (var outputItem in res.Value.OutputItems)
            {
                var messageResponseItem = outputItem as MessageResponseItem;
                if (messageResponseItem == null || messageResponseItem.Content.Count == 0)
                {
                    continue;
                }
                foreach (var ann in
                      messageResponseItem.Content // content è un array ..
                     .SelectMany(c => c.OutputTextAnnotations) // con un sottoarray di annotation =>appiattiamo tutto
                     .OfType<ContainerFileCitationMessageAnnotation>())
                {
                    filesReferences.Add(ann.Filename);
                }
            }

            Console.WriteLine($"Files citati dal modello: {string.Join(",", filesReferences)}");

            await vectoreStoreClient.DeleteVectorStoreAsync(store.Value.Id);

        }
    }
}