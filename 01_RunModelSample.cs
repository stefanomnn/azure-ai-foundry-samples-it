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


/// <summary>
/// mostra come fare una chiamata testuale a un modello,e mostra la gestione del contesto.
/// mostra anche come estrarre il conteggio dei tokens
/// </summary>
internal class RunModelSample
{
    private readonly OpenAIClient _OpenAIClient;

    public RunModelSample(OpenAIClient openAiClient)
    {
        _OpenAIClient = openAiClient;
    }

    public static  RunModelSample FromUserCredentials (string foundryProjectUrl, string tenantId)
    {

        // 1. Autenticazione e creazione client
        AIProjectClient projectClient = new AIProjectClient(
            new Uri(foundryProjectUrl),
            new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { 
                TenantId=tenantId,
            })
        );
        return new RunModelSample( projectClient.GetProjectOpenAIClient());
    }

    public static RunModelSample FromApiKey(string openAiURL, string password)
    {
        if (openAiURL.EndsWith("/")) {
            openAiURL = openAiURL.Substring(0, openAiURL.Length - 1);
        }
        if (!openAiURL.EndsWith("/openai/v1")) {
            openAiURL += "/openai/v1";
        }
       
        var openAiClient = new OpenAIClient(new ApiKeyCredential(password), new OpenAIClientOptions
        {
            Endpoint = new Uri(openAiURL)
        });
        return new RunModelSample(openAiClient);
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

        // ottiene il client ResponsesClient, che è il protocollo di comunicazione piu moderno rispetto a GetChatClient()
        ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

        string initialPrompt = "dimmi una breve freddura";
        Console.WriteLine($"🚀 Invio richiesta al modello {modelId}: " + initialPrompt);
        var res = await responsesClient.CreateResponseAsync(modelId, initialPrompt);

        AnalyzeResponse(res.Value);
        string responsetToInitialPrompt = res.Value.GetOutputText();


        // gestion del contesto nella stessa sessione: l'LLM deve rispondere a una domanda "rileggendo" i messaggi precedenti
        string continuePrompt = "ancora una.";

        if (!_OpenAIClient.Endpoint.ToString().Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
        {
            // METODO 1: passiamo l'id della conversazione precedente come parametro. Il modello capisce che deve leggere i messaggi precedenti.
            // attenzione: non tutti i provider supportano la gestione del contesto, quindi questo metodo potrebbe non funzionare con tutti i provider.
            // caso concreto: azure foundry supporta la gestione del contesto, quindi possiamo usare questo metodo.
            // nello specifico, i messaggi vengono salvati in automatico per 30 giorni.
            // i messaggi vengono salvati "per utente", quindi due utenti diversi non vedono gli stessi messaggi        

            Console.WriteLine($"🚀 Invio richiesta al modello {modelId}, che richiede la conoscenza della conversazione precedente\r\n: " + continuePrompt);
            var res2 = await responsesClient.CreateResponseAsync(modelId, continuePrompt, res.Value.Id);

            AnalyzeResponse(res.Value);
        }
        //METODO 2: ricostruzione manuale della conversazione, necessario se il provider è totalmente stateless e non supporta la gestione del contesto.
        // In questo caso, dobbiamo passare al modello tutti i messaggi precedenti, in modo che possa "ricordare" la conversazione.
        // esempio concreto: openrouter.ai non supporta la gestione del contesto, quindi dobbiamo ricostruire manualmente la conversazione.
        var continuePompt2 = new CreateResponseOptions
        {
            Model = modelId,
        };
        continuePompt2.InputItems.Add(ResponseItem.CreateUserMessageItem(initialPrompt));

        //CreateAssistantMessageItem: vuol dire che il testo che stiamo fornendo deriva da una risposta del modello, quindi il modello deve considerarlo come tale.
        continuePompt2.InputItems.Add(ResponseItem.CreateAssistantMessageItem(responsetToInitialPrompt));
        continuePompt2.InputItems.Add(ResponseItem.CreateUserMessageItem("ancora una."));

        var res3 = await responsesClient.CreateResponseAsync(continuePompt2);
        var res3Txt = res3.Value.GetOutputText();
        AnalyzeResponse(res3.Value);
    }

    
    public async Task RunWithOptions(string modelId)
    {

        // ottiene il client ResponsesClient, che è il protocollo di comunicazione piu moderno rispetto a GetChatClient()
        ResponsesClient responsesClient = _OpenAIClient.GetResponsesClient();

        string initialPrompt = "dimmi una barzelletta sui programmatori";
        Console.WriteLine($"🚀 Invio richiesta al modello {modelId}: " + initialPrompt);
        var res = await responsesClient.CreateResponseAsync(new CreateResponseOptions
        {
            Model = modelId,
            Temperature=(float)0.3, // 0 = piu prevedibile, 1 = piu creativo
            TopP=(float)0.70, // soglia minima di probabilita delle parole
            MaxOutputTokenCount= 1000, // limito l'output e quindi la spesa
            Instructions="Se l'utente ti chiede qualcosa che non riguarda le barzellette, rispondi 'non posso rispondere a questo argomento'",            
            InputItems = {
                 ResponseItem.CreateUserMessageItem(initialPrompt)
            }
        });

        AnalyzeResponse(res.Value);

        //  Leggiamo l'output del modello e i tokens usati
        string responsetToInitialPrompt = res.Value.GetOutputText();
        Console.WriteLine($"📝 Output testuale del modello:\n{responsetToInitialPrompt}");
        Console.WriteLine($"📝 Tokens: Input={res.Value.Usage.InputTokenCount}, Output={res.Value.Usage.OutputTokenCount}");

    }
}
