using Microsoft.ML.Tokenizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureAiSamples.otherTopics
{
    /// <summary>
    /// stima il calcolo dei token di input usanddo la libreria Microsoft.ML.Tokenizers.
    /// riconosce principalmente le famiglie di LLM openai based - non funziona ad esempio con gemini/antropic
    /// </summary>
    internal class PredictOpenAIInputTokensSample
    {
        public  int CalcByImageSize(int width, int height, bool highDetail = true)
        {
            // Se si sceglie il dettaglio basso, il costo è sempre fisso
            if (!highDetail)
            {
                return 85; // Costo fisso per detail: "low"
            }

            // 1. Scala l'immagine affinché stia in 2048x2048
            if (width > 2048 || height > 2048)
            {
                double maxDim = Math.Max(width, height);
                width = (int)Math.Round((width / maxDim) * 2048);
                height = (int)Math.Round((height / maxDim) * 2048);
            }

            // 2. Scala affinché il lato corto sia al massimo 768px
            double minDim = Math.Min(width, height);
            if (minDim > 768)
            {
                width = (int)Math.Round((width / minDim) * 768);
                height = (int)Math.Round((height / minDim) * 768);
            }

            // 3. Calcola il numero di tile 512x512 necessari a coprire l'immagine
            int tilesX = (int)Math.Ceiling(width / 512.0);
            int tilesY = (int)Math.Ceiling(height / 512.0);
            int totalTiles = tilesX * tilesY;

            // 4. Formula standard OpenAI / Azure OpenAI
            int totalTokens = 85 + (170 * totalTiles);

            return totalTokens;
        }

        public void CalcByText() {

            string inputPrompt = "nel mezzo del cammin di nostra vita mi ritrovai per una selva oscura ché la diritta via era smarrita";
            var supportedModels = new List<string>
        {
            "gpt-4",
            "gpt-4o",
            "gpt-4o-mini",
            "gpt-5",        // Supportato ufficialmente nelle ultime specifiche della libreria
            "text-embedding-ada-002",
            "text-embedding-3-small",
            "text-embedding-3-large"
        };

            // 1. Inizializziamo il tokenizer specifico per il modello (es. gpt-4o)
            // Questo carica il vocabolario corretto per contare i token con precisione matematica.
            // reference: https://learn.microsoft.com/it-it/dotnet/ai/how-to/use-tokenizers
            foreach (var model in supportedModels) {
                TiktokenTokenizer tokenizer = TiktokenTokenizer.CreateForModel(model);

                // 2. Calcoliamo il numero esatto di token dell'input
                int inputTokenCount = tokenizer.CountTokens(inputPrompt);                

                Console.WriteLine($"[Preventivo] Il prompt contiene esattamente {inputTokenCount} token usando la famiglia di modelli {model}");
            }

           
        }
    }
}
