using Azure;
using Azure.AI.Translation.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AzureAiSamples.textAnalysis
{
    internal class TranslateTextSample
    {
        private readonly string _EndpointURL;
        private readonly string _ApiKey;

        public TranslateTextSample(string endpointURL, string apiKey)
        {
            _EndpointURL = $"{endpointURL}";
            _ApiKey = apiKey;
        }

        /// <summary>
        /// mostra come usare il servizio translate di foundry
        /// </summary>
        /// <param name="text"></param>
        /// <param name="targetLanguage"></param>
        /// <returns></returns>
        public  async Task TranslateString(string text, string targetLanguage="en-US")
        {
            Console.WriteLine($"Translation feature... traduco il testo {text} in lingua {targetLanguage}");
            string fullUrl = $"{_EndpointURL}/translator/text/translate?api-version=2025-10-01-preview";
            var client = new TextTranslationClient(
                new AzureKeyCredential(_ApiKey),
                new Uri(fullUrl)
                        
            );            

            var target = new TranslationTarget(language: targetLanguage, deploymentName: "gpt-5.1"); // lasciare deploymentname: null per usare il motore classico di azure, piu antico ma piu veloce economico
            var input = new TranslateInputItem(text, new[] { target});
            input.Language = "it-it";
            Response<TranslatedTextItem> response = await client.TranslateAsync(input);

            var str = new StringBuilder();
            foreach (var transl in response.Value.Translations) { 
                str.Append(transl.Text);
            }
            Console.WriteLine(str);
        }

        /// 
        /// Esegue il Dictionary Lookup utilizzando direttamente l'API REST di Azure Translator.
        /// Documentazione ufficiale: https://learn.microsoft.com/azure/ai-services/translator/reference/v3-0-dictionary-lookup
        /// in parole povere, traduce una SINGOLA parola da una lingua a all'altra con tutti i possibili contesti.
        /// es. FLY può essere tradotto in:
        /// - SOSTANTIVO "VOLO"
        /// - VERBO "VOLARE"
        /// 
        /// altro esempio: LIGHT può essere:
        /// - SOSTANTIVO "LUCE"
        /// - AGGETTIVO "LEGGERO"
        /// - VERBO "ILLUMINARE"
        /// 
        /// NB: il servizio traslator offre questa funzionalità, ma l'API non la espone => dobbiamo usare l'HTTP grezzo
        public async Task LookupDictionaryRestAsync(string word, string fromLanguage = "en", string toLanguage = "it")
        {
            Console.WriteLine("*************** LookupDictionary feature..");

            // 1. Costruzione dell'endpoint di lookup con i parametri di lingua
            // nota che l'url è fisso non c'è identificativo di risorsa
            string requestUrl = $"https://api.cognitive.microsofttranslator.com/dictionary/lookup?api-version=3.0&from={fromLanguage}&to={toLanguage}";
            
            // 2. Preparazione del corpo della richiesta (Array JSON con il testo da cercare)
            var body = new[] { new { Text = word } };
            string jsonBody = JsonSerializer.Serialize(body);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 3. Configurazione degli header richiesti da Azure AI Services (Key + Region se richiesta dalla risorsa)
            requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", _ApiKey);
            requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", "eastus2");

          
            // Se usi una risorsa multi-servizio o regionalizzata, potresti dover aggiungere anche:
            // requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", "tuaregione"); // es. westeurope

            // 4. Esecuzione della chiamata HTTP
            using var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            // 5. Restituzione del JSON grezzo con i risultati del dizionario (translation, back-translations, ecc.)
            string str= await response.Content.ReadAsStringAsync();
            Console.WriteLine(str);
        }

        /// <summary>
        /// Mostra come usare la traslitterazione con il servizio Translator di Azure.
        /// La traslitterazione consiste nello scrivere una parola in un alfabeto diverso (senza però tradurla).
        /// Esempio: "富士山" (giapponese) scritto in caratteri latini diventa "Fujisan".
        /// Nota di naming: il parametro chiamato 'script' indica in inglese un 'sistema di scrittura' o 'grafia'
        /// </summary>
        /// 
        public async Task TransliterateString()
        {
            string text = "富士山";
            string language = "ja";
            string fromScript = "Jpan"; // alfabeto sorgente
            string toScript = "Latn"; // alfabeto destinazione

            string fullUrl = $"{_EndpointURL}/translator/text/translate?api-version=2025-10-01-preview";

            var client = new TextTranslationClient(
                new AzureKeyCredential(_ApiKey),
                new Uri(fullUrl)
            );
            Console.WriteLine("*************** Transliterate String...");

            // Esegue la translitterazione passando lingua, script di origine, script di destinazione e il testo
            Response<IReadOnlyList<TransliteratedText>> response = await client.TransliterateAsync(language, fromScript, toScript, text);

            // Restituisce il testo convertito nello script desiderato
            var str= string.Join(Environment.NewLine, response.Value.Select(transl=>transl.Text));
            Console.WriteLine(str);
        }
    }
}
