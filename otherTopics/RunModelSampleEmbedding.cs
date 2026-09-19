using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAiSamples.otherTopics
{
    /// <summary>
    /// Mostra come generare EMBEDDING da testi e calcolare la similarità
    /// coseno tra vettori. È il mattone base per:
    /// - RAG (Retrieval-Augmented Generation)
    /// - Ricerca semantica
    /// - Clustering di documenti
    /// - Rilevamento duplicati
    ///
    /// NOTA: usa EmbeddingClient con un modello di embedding (es. text-embedding-3-small).
    /// Il modello di embedding è diverso dai modelli GPT — produce vettori, non testo.
    /// </summary>
    internal class RunModelSampleEmbedding
    {
        private OpenAIClient _OpenAIClient;

        public RunModelSampleEmbedding(string openAiURL, string password)
        {
            if (openAiURL.EndsWith("/"))
            {
                openAiURL = openAiURL.Substring(0, openAiURL.Length - 1);
            }
            if (!openAiURL.EndsWith("/openai/v1"))
            {
                openAiURL += "/openai/v1";
            }
            _OpenAIClient = new OpenAIClient(new ApiKeyCredential(password), new OpenAIClientOptions
            {
                Endpoint = new Uri(openAiURL)
            });
        }


        public async Task Run(string emdeddingModelId)
        {   
            // Otteniamo un EmbeddingClient
            //    Il modello di embedding è diverso dai modelli di chat!
            //    text-embedding-3-small è economico e veloce (1536 dimensioni)
            //    text-embedding-3-large è più preciso ma più costoso (3072 dimensioni)
            EmbeddingClient embeddingClient = _OpenAIClient.GetEmbeddingClient(emdeddingModelId);

            //  Prepariamo un piccolo "database" di documenti da cercare
            string[] documents = new[]
            {
                "La carbonara è un piatto tipico della cucina romana a base di uova, guanciale, pecorino e pepe.",
                "Il tiramisù è un dolce italiano fatto con savoiardi, caffè, mascarpone e cacao.",
                "La pizza margherita è nata a Napoli nel 1889 in onore della regina Margherita di Savoia.",
                "Il risotto alla milanese è un piatto lombardo preparato con riso, zafferano e brodo.",
                "Le orecchiette alle cime di rapa sono un piatto tipico della cucina pugliese.",
                "Il machine learning è una branca dell'intelligenza artificiale che usa algoritmi statistici.",
                "I transformer sono un'architettura di deep learning introdotta nel paper 'Attention is All You Need'.",
                "Azure AI Foundry è una piattaforma Microsoft per sviluppare applicazioni di intelligenza artificiale."
            };

            // 4. Query di ricerca
            string query = "Piatti tradizionali della cucina italiana";

            Console.WriteLine("🚀 Generazione embedding per i documenti e la query...\n");

            // 5. Generiamo gli embedding per tutti i documenti
            List<float[]> documentEmbeddings = new();
            foreach (string doc in documents)
            {
                OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(doc);
                float[] vector = embedding.ToFloats().ToArray();
                documentEmbeddings.Add(vector);
            }

            // 6. Generiamo l'embedding per la query
            OpenAIEmbedding queryEmbedding = await embeddingClient.GenerateEmbeddingAsync(query);
            float[] queryVector = queryEmbedding.ToFloats().ToArray();

            Console.WriteLine($"📐 Dimensione vettori: {queryVector.Length}");
            Console.WriteLine($"📝 Query: \"{query}\"\n");

            // 7. Calcoliamo la similarità coseno tra query e ogni documento
            var similarities = new List<(int Index, string Document, double Score)>();
            for (int i = 0; i < documents.Length; i++)
            {
                double similarity = CosineSimilarity(queryVector, documentEmbeddings[i]);
                similarities.Add((i, documents[i], similarity));
            }

            // 8. Ordiniamo per similarità decrescente e mostriamo i risultati
            var ranked = similarities.OrderByDescending(s => s.Score).ToList();

            Console.WriteLine("📊 Risultati della ricerca semantica (ordinati per rilevanza):\n");
            for (int i = 0; i < ranked.Count; i++)
            {
                var (index, document, score) = ranked[i];
                string bar = new string('█', (int)(score * 50));
                Console.WriteLine($"{i + 1}. [Score: {score:F4}] {bar}");
                Console.WriteLine($"   {document}\n");
            }

            // 9. Bonus: mostriamo anche la similarità tra due documenti correlati
            Console.WriteLine("--- Bonus: similarità tra documenti ---");
            double docSimilarity = CosineSimilarity(documentEmbeddings[0], documentEmbeddings[1]);
            Console.WriteLine($"Carbonara vs Tiramisù: {docSimilarity:F4} (entrambi piatti italiani)");

            double docSimilarity2 = CosineSimilarity(documentEmbeddings[0], documentEmbeddings[5]);
            Console.WriteLine($"Carbonara vs Machine Learning: {docSimilarity2:F4} (argomenti diversi)");
        }

        /// <summary>
        /// Calcola la similarità coseno tra due vettori.
        /// Restituisce un valore tra -1 e 1, dove 1 = identici, 0 = ortogonali, -1 = opposti.
        /// </summary>
        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("I vettori devono avere la stessa dimensione");

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}