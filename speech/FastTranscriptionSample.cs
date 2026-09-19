
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureAiSamples.speech
{
    /// <summary>
    /// funzionalita speech to text (fast transcription) , che dato un file audio estrae il testo (eventualmente con l'aiuto di un LLM)
    /// la versione REST è piu avanti dell'sdk => NON usiamo l'sdk!
    /// </summary>
    public class FastTranscriptionSample
    {
        public class TranscriptionResponse
        {
            [JsonPropertyName("durationMilliseconds")]
            public long DurationMilliseconds { get; set; }

            [JsonPropertyName("combinedPhrases")]
            public List<CombinedPhrase> CombinedPhrases { get; set; } = [];

            [JsonPropertyName("phrases")]
            public List<Phrase> Phrases { get; set; } = [];
        }

        public class CombinedPhrase
        {
            [JsonPropertyName("channel")]
            public int Channel { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        public class Phrase
        {
            [JsonPropertyName("channel")]
            public int Channel { get; set; }

            [JsonPropertyName("speaker")]
            public int Speaker { get; set; }

            [JsonPropertyName("offsetMilliseconds")]
            public long OffsetMilliseconds { get; set; }

            [JsonPropertyName("durationMilliseconds")]
            public long DurationMilliseconds { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("words")]
            public List<Word> Words { get; set; } = [];

            [JsonPropertyName("locale")]
            public string Locale { get; set; } = string.Empty;

            [JsonPropertyName("confidence")]
            public double Confidence { get; set; }
        }

        public class Word
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

    [JsonPropertyName("offsetMilliseconds")]
            public long OffsetMilliseconds { get; set; }

            [JsonPropertyName("durationMilliseconds")]
            public long DurationMilliseconds { get; set; }
        }

        private readonly string _EndpointURL;
        private readonly string _ApiKey;

        public FastTranscriptionSample(string endpointURL, string apiKey)
        {
            _EndpointURL = endpointURL;
            _ApiKey = apiKey;

        }

        public async Task<string> DoTranscriptionRest(string audioFilePath)
        {
            using var httpClient = new HttpClient();
            using var fileStream = new FileStream(audioFilePath, FileMode.Open, FileAccess.Read);

            // Build multipart form content
            using var formContent = new MultipartFormDataContent();

            // Audio file part
            var audioContent = new StreamContent(fileStream);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            formContent.Add(audioContent, "audio", Path.GetFileName(audioFilePath));

            // Definition JSON con TUTTE le opzioni (schema ufficiale Swagger):
            // https://github.com/Azure/azure-rest-api-specs/blob/main/specification/cognitiveservices/data-plane/Speech/SpeechToText/stable/2025-10-15/speechtotext.json
            //
            // TranscribeDefinition (8 proprietà totali):
            //   locales: lingua presente nel file audio (array di stringhe - magari è un mix italiano e inglese)
            //   audioUrl: non usata, in quanto il file audio viene inviato direttamente come multipart/form-data
            //   profanityFilterMode: gestione delle parole volgari (None, Removed, Tags, Masked)
            //   enhancedMode (vedi sotto)
            //   phraseList: (2 proprietà): phrases, biasingWeight (double):
            //                lista di parole da favorire (es. nomi propri, acronimi, ecc.)
            //                ESEMPIO: Marchi come "ILIAD" o sigle tecniche come "IP"
            //                non fanno parte del vocabolario comune di una lingua.
            //                Senza un suggerimento, il sistema cercherà di trasformare quel suono nella parola comune più vicina
            //                che conosce (ad esempio trascrivendo "iliade" invece del marchio, o sillabando le lettere "i pi").
            //                Oltre, è possibile impostare un biasingWeight (peso di influenza) per indicare quanto il sistema dovrebbe favorire queste parole rispetto al vocabolario comune.
            //   diarization: (2 proprietà): enabled, maxSpeakers (2-35!) - se abilitato, il sistema cercherà di distinguere i diversi parlanti nel file audio e assegnare loro etichette separate.
            //   channels: un file audio puo avere piu canali (es. stereo - cuffia sinistra e stereo - cuffia destra). Selezionando i canali, il sistema trascriverà solo quelli selezionati. (array di interi)
            //   models: in speech è possibile creare modelli personalizzati (custom) per trascrivere meglio un certo tipo di audio (es. un certo accento, o un certo dominio tecnico). In questo caso, si può specificare il nome del modello da usare. Se non specificato, il sistema userà il modello generico.
            //          L'addestramento di un modello personalizzato consiste nell'"insegnare" all'intelligenza artificiale a riconoscere meglio la voce, il gergo o i termini specifici del tuo settore, fornendole esempi reali da analizzare.
            //          Il processo prevede solitamente l'invio ad Azure di due tipi di dati:

            //          * **Audio con trascrizioni corrette: **Registrazioni di riunioni, chiamate o dettati reali della tua azienda(insieme al testo esatto di ciò che viene detto), in modo che il modello impari ad abituarsi a specifici accenti, inflessioni o alla qualità audio dei tuoi microfoni.
            //          * **Testi di dominio(Dataset di testo):**Documenti, manuali, elenchi di prodotti o glossari pieni di parole tecniche, acronimi o nomi propri usati nel tuo campo, così il motore impara che quei termini esistono e qual è la loro probabilità di comparsa.
            //          Il sistema elabora questi dati per creare una versione del motore di riconoscimento vocale cucita su misura per le tue esigenze, riducendo drasticamente gli errori sulle parole insolite.

            // enhancedMode (4 proprietà): enabled, task, targetLanguage, prompt
            //   task: "transcribe" | "translate"
            //   targetLanguage: de, en, es, fr, it, ko, ja, pt, zh (solo con task=translate)
            // ATTENZIONE: enhancedMode NON è supportato ovunque. eventualmente disabilitarlo eliminando il nodo
            //   Usare una risorsa in: eastus, westus, westus2, northeurope, southeastasia, centralindia

            // 2026-09-17: enhanced mode non è supportato purtroppo. dobbiamo lasciare enabled=false
            var definitionJson = """
            {
                "locales": ["it-IT"],
                "profanityFilterMode": "Masked",
                "channels": [0, 1],
                "enhancedMode": {
                    "enabled": false, 
                    "task": "transcribe",
                    "prompt": [
                        "Scrivi le date in formato dd/MM/yyyy.",
                        "Mantieni la punteggiatura originale."
                    ]
                },
                "diarization": {
                    "enabled": true,
                    "maxSpeakers": 2
                },
                "phraseList": {
                    "phrases": ["ILIAD", "IP"],
                    "biasingWeight": 1.0
                }
            }
            """;

            var definitionContent = new StringContent(definitionJson, Encoding.UTF8, "application/json");
            formContent.Add(definitionContent, "definition");

            // Build the request
            // Formato URL: https://{ResourceName}.cognitiveservices.azure.com/speechtotext/transcriptions:transcribe?api-version=2025-10-15
            var baseUri = new Uri(_EndpointURL);
            var requestUri = new Uri(baseUri, "speechtotext/transcriptions:transcribe?api-version=2025-10-15");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _ApiKey);
            request.Content = formContent;

            // Send and read response
            var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP {response.StatusCode}: {responseBody}");
            }
            var result = JsonSerializer.Deserialize<TranscriptionResponse>(responseBody);

            var text = new StringBuilder();
            foreach (var item in result.Phrases)
            {
                text.AppendLine(item.Text);
            }
            return text.ToString();
        }

        
    }
}
