using Azure;
using Azure.AI.Translation.Document;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureAiSamples.textAnalysis
{
    /// <summary>
    /// traduzione di documenti con il servizio di traduzione di Foundry
    /// </summary>
    internal class TranslateDocumentSample
    {
        private readonly string _endpointURL;
        private readonly string _apiKey;

        public TranslateDocumentSample(string endpointURL, string apiKey)
        {
            _endpointURL = endpointURL;
            _apiKey = apiKey;
        }

        public async Task TranslateDocumentAsync(string inputFilePath, string outputFilePath, string targetLanguage = "en")
        {
            Console.WriteLine("*************** TranslateDocumentAsync..");
            Console.WriteLine("input doc: " + inputFilePath);
            Console.WriteLine("target lang: " + targetLanguage);

            // Inizializza il client per la traduzione di singoli documenti
            var client = new SingleDocumentTranslationClient(
                new Uri(_endpointURL),
                new AzureKeyCredential(_apiKey)
            );

            using Stream fileStream = File.OpenRead(inputFilePath);

            // Determina il MIME type o passa lo stream del file
            string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; // Esempio per .docx
            var sourceDocument = new MultipartFormFileData(Path.GetFileName(inputFilePath), fileStream, contentType);

            var content = new DocumentTranslateContent(sourceDocument);

            // Esegue la traduzione sincrona del documento
            Response<BinaryData> response = await client.TranslateAsync(targetLanguage, content);

            // Salva il file tradotto nel percorso di output
            using FileStream outputFileStream = File.Create(outputFilePath);
            await response.Value.ToStream().CopyToAsync(outputFileStream);
            Console.WriteLine("traduzione terminata. Output doc: " + outputFilePath);
        }
    }
}
