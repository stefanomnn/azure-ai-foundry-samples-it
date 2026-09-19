using Azure;
using Azure.AI.Language.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

// reference:
// https://learn.microsoft.com/en-us/rest/api/language/analyze-text/analyze-text/analyze-text?view=rest-language-analyze-text-2026-05-01&viewFallbackFrom=rest-language-analyze-text-2025-05-15-preview&tabs=HTTP


namespace AzureAiSamples.textAnalysis
{
    internal class DetectionResult
    {
        public string Language { get; set; }
        public double ConfidenceScore { get; set; }
    }

    internal class SentimentResult
    {
        public string Text { get; set; }
        public decimal PositiveScore { get; set; }
        public decimal NegativeScore { get; set; }
        public decimal NeutralScore { get; set; }

        public List<SentimentResult> Sentences { get; set; } = new();
        public string Overall { get; internal set; }

        public override string ToString()
        {
            return Overall + " (Positive: " + PositiveScore + ", Negative: " + NegativeScore + ", Neutral: " + NeutralScore + ")";
        }
    }


    public class EntityRecognitionResult
    {
        public string Text { get; set; }
        public string Category { get; set; }
        public decimal ConfidenceScore { get; set; }

        public override string ToString()
        {
            return $"Text: {Text}, Category: {Category}, ConfidenceScore: {ConfidenceScore}";
        }
    }

    public class LinkedEntityResult
    {
        public string Name { get; internal set; }
        public string DataSource { get; internal set; }
        public string ReferenceUrl { get; internal set; }
        public string Lang { get; internal set; }
        public decimal? ConfidenzeScore { get; internal set; }

        public override string ToString()
        {
            return $"Name: {Name}, DataSource: {DataSource}, Lang: {Lang}";
        }
    }

    public class PiiEntityResult
    {
        public string Text { get; set; }
        public string Category { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string RedactedText { get; set; }

        public override string ToString()
        {
            return $"Text: {Text}, Category: {Category}, Confidence: {ConfidenceScore}";
        }
    }

    internal class TextAnalysisSample
    {
        private readonly string _EndpointURL;
        private readonly string _ApiKey;

        public TextAnalysisSample(string endpointURL, string apiKey)
        {
            _EndpointURL = endpointURL;
            _ApiKey = apiKey;
        }

        TextAnalysisClient ClientFactory()
        {
            Uri endpoint = new Uri(_EndpointURL);
            AzureKeyCredential credential = new(_ApiKey);

            var client = new TextAnalysisClient(endpoint, credential);
            return client;
        }


        /// <summary>
        /// identifica la lingua del testo e restituisce il nome della lingua e il punteggio di confidenza.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task DetectLanguage(string text)
        {
            Console.WriteLine("****** language detection");
            Console.WriteLine(text);

            TextAnalysisClient client = ClientFactory();


            AnalyzeTextInput body = new TextLanguageDetectionInput()
            {
                TextInput = new LanguageDetectionTextInput()
                {
                    LanguageInputs =
            {
                new LanguageInput("A", text)
            }
                }
            };



            Response<AnalyzeTextResult> analisisResult = await client.AnalyzeTextAsync(body);

            AnalyzeTextLanguageDetectionResult AnalyzeTextLanguageDetectionResult = (AnalyzeTextLanguageDetectionResult)analisisResult.Value;
            var firstDoc = AnalyzeTextLanguageDetectionResult.Results.Documents.First();

            var str = JsonSerializer.Serialize(firstDoc);
            Console.WriteLine(str);


        }

        /// <summary>
        /// sentiment analysis del testo,sia globale che delle singole frasi, con punteggi di confidenza per positivo, negativo e neutro.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task  SentimentAnalysis(string text)
        {
            Console.WriteLine("****** sentiment analysis");
            Console.WriteLine(text);
            TextAnalysisClient client = ClientFactory();


            TextSentimentAnalysisInput body = new TextSentimentAnalysisInput()
            {
                TextInput = new MultiLanguageTextInput()
                {
                    MultiLanguageInputs =
                    {
                        new MultiLanguageInput("A", text)
                    }
                }
            };



            Response<AnalyzeTextResult> analisisResult = await client.AnalyzeTextAsync(body);
            AnalyzeTextSentimentResult sentimentResult = (AnalyzeTextSentimentResult)analisisResult.Value;
            var firstDoc = sentimentResult.Results.Documents.First();

            var result = new SentimentResult
            {
                PositiveScore = (decimal)firstDoc.ConfidenceScores.Positive,
                NegativeScore = (decimal)firstDoc.ConfidenceScores.Negative,
                NeutralScore = (decimal)firstDoc.ConfidenceScores.Neutral,
            };
            result.Overall = firstDoc.Sentiment.ToString();

            foreach (var s in firstDoc.Sentences)
            {
                result.Sentences.Add(new SentimentResult
                {
                    Text = s.Text,
                    NegativeScore = (decimal)s.ConfidenceScores.Negative,
                    PositiveScore = (decimal)s.ConfidenceScores.Positive,
                    NeutralScore = (decimal)s.ConfidenceScores.Neutral,
                    Overall = s.Sentiment.ToString(),
                });
            }

            var str = JsonSerializer.Serialize(firstDoc);
            Console.WriteLine(str);

        }


        /// <summary>
        /// Estrae dal testo le parole e le brevi frasi che rappresentano i concetti
        /// più rilevanti, consentendo di individuare rapidamente gli argomenti principali
        /// trattati nel contenuto.
        /// </summary>
        public async Task ExtractKeyPhrases(string text)
        {
            Console.WriteLine("****** ExtractKeyPhrases");
            Console.WriteLine(text);
            TextAnalysisClient client = ClientFactory();

            TextKeyPhraseExtractionInput body = new TextKeyPhraseExtractionInput()
            {
                TextInput = new MultiLanguageTextInput()
                {
                    MultiLanguageInputs =
            {
                new MultiLanguageInput("A", text)
            }
                }
            };

            Response<AnalyzeTextResult> analisisResult = await client.AnalyzeTextAsync(body);
            AnalyzeTextKeyPhraseResult keyPhrasesResult = (AnalyzeTextKeyPhraseResult)analisisResult.Value;

            var firstDoc = keyPhrasesResult.Results.Documents.First();

            var str = JsonSerializer.Serialize(firstDoc);
            Console.WriteLine(str);


            var keyPhrases = new List<string>();

            foreach (var phrase in firstDoc.KeyPhrases)
            {
                keyPhrases.Add(phrase);
            }
            
        }


        /// <summary>
        /// Analizza il testo per identificare entità nominate, come persone, organizzazioni,
        /// località, date, indirizzi e altre informazioni rilevanti.
        /// A differenza del rilevamento PII, lo scopo è riconoscere e classificare le entità
        /// presenti nel testo, non identificare esclusivamente informazioni personali.
        /// Restituisce il testo dell'entità, la relativa categoria e il livello di confidenza.
        /// </summary>
        public async Task<List<EntityRecognitionResult>> RecognizeEntities(string text)
        {
            Console.WriteLine("****** RecognizeEntities");
            Console.WriteLine(text);

            TextAnalysisClient client = ClientFactory();

            TextEntityRecognitionInput body = new TextEntityRecognitionInput()
            {
                TextInput = new MultiLanguageTextInput()
                {
                    MultiLanguageInputs =
            {
                new MultiLanguageInput("A", text)
            }
                }
            };



            Response<AnalyzeTextResult> analisisResult = await client.AnalyzeTextAsync(body);

            Azure.AI.Language.Text.AnalyzeTextEntitiesResult entitiesResult = (Azure.AI.Language.Text.AnalyzeTextEntitiesResult)analisisResult.Value;

            EntityActionResult firstDoc = entitiesResult.Results.Documents.First();
            Console.WriteLine(JsonSerializer.Serialize(firstDoc));

            var results = new List<EntityRecognitionResult>();

            foreach (var e in firstDoc.Entities)
            {
                results.Add(new EntityRecognitionResult
                {
                    Text = e.Text,
                    Category = $"{e.Category}/{e.Subcategory}",
                    ConfidenceScore = (decimal)e.ConfidenceScore
                });
            }



            return results;            
        }


        /// <summary>
        /// Analizza il testo per identificare entità nominate e collegarle a entità
        /// presenti in una base di conoscenza esterna.
        /// Per ogni entità riconosciuta restituisce il nome, la fonte dei dati,
        /// l'URL dell'entità, la lingua e il livello di confidenza del collegamento.
        /// A differenza della Named Entity Recognition, che si limita a classificare
        /// l'entità (ad esempio "Location" o "Person"), l'Entity Linking cerca di
        /// identificare l'entità reale a cui il testo fa riferimento.
        /// </summary>
        public async Task<List<LinkedEntityResult>> RecognizeLinkedEntities(string text)
        {
            Console.WriteLine("****** RecognizeLinkedEntities");
            Console.WriteLine(text);

            TextAnalysisClient client = ClientFactory();

            TextEntityLinkingInput body = new TextEntityLinkingInput()
            {
                TextInput = new MultiLanguageTextInput()
                {
                    MultiLanguageInputs =
            {
                new MultiLanguageInput("A", text)
            }
                }
            };



            Response<AnalyzeTextResult> analisisResult = await client.AnalyzeTextAsync(body);

            Azure.AI.Language.Text.AnalyzeTextEntityLinkingResult entitiesResult = (Azure.AI.Language.Text.AnalyzeTextEntityLinkingResult)analisisResult.Value;

            EntityLinkingActionResult firstDoc = entitiesResult.Results.Documents.First();
            Console.WriteLine(JsonSerializer.Serialize(firstDoc));


            var results = new List<LinkedEntityResult>();

            foreach (var e in firstDoc.Entities)
            {
                var match = e.Matches.FirstOrDefault();

                results.Add(new LinkedEntityResult
                {
                    Name = e.Name,
                    DataSource = e.DataSource,
                    ReferenceUrl = e.Url,
                    Lang = e.Language,
                    ConfidenzeScore = match == null ? null : (decimal)match.ConfidenceScore
                });
            }



            return results;

        }

        /// <summary>
        /// Analizza il testo alla ricerca di **informazioni personali identificabili**
        /// (PII - Personally Identifiable Information), come nomi, indirizzi, numeri di telefono,
        /// codici fiscali e altri dati personali.
        /// Restituisce le entità PII individuate con il relativo testo, categoria e livello di confidenza.
        /// Azure AI Language è inoltre in grado di produrre una versione del testo in cui le informazioni
        /// personali individuate vengono redatte.
        /// </summary>
        public async Task<List<PiiEntityResult>> ExtractPiiEntities(string text)
        {
            Console.WriteLine("****** ExtractPiiEntities");
            Console.WriteLine(text);

            TextAnalysisClient client = ClientFactory();

            TextPiiEntitiesRecognitionInput body = new TextPiiEntitiesRecognitionInput()
            {
                TextInput = new MultiLanguageTextInput()
                {
                    MultiLanguageInputs =
                    {
                        new MultiLanguageInput("A", text)
                    }
                }
            };

            var analisisResult = await client.AnalyzeTextAsync(body);
            var piiResult = (Azure.AI.Language.Text.AnalyzeTextPiiResult)analisisResult.Value;
            var firstDoc = piiResult.Results.Documents.First();
            Console.WriteLine(JsonSerializer.Serialize(firstDoc));

            var results = new List<PiiEntityResult>();

            foreach (var entity in firstDoc.Entities)
            {
                results.Add(new PiiEntityResult
                {
                    Text = entity.Text,
                    Category = entity.Category,
                    ConfidenceScore = (decimal)entity.ConfidenceScore,
                    RedactedText = firstDoc.RedactedText
                });
            }

            return results;
        }


        /// <summary>
        /// La extractive summarization è un riassunto che non inventa né riscrive il testo: prende le frasi più importanti dal documento originale e le mette insieme.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<List<string>> ExtractiveSummarization(string text)
        {
            Console.WriteLine("****** ExtractiveSummarization");
            Console.WriteLine(text);

            TextAnalysisClient client = ClientFactory();
            var input = new MultiLanguageTextInput
            {
                MultiLanguageInputs =
    {
        new MultiLanguageInput("A", text)
        {
            Language = "it"
        }
    }
            };

            var actions = new List<AnalyzeTextOperationAction>
{
    new ExtractiveSummarizationOperationAction()
};

            var operation = await client.AnalyzeTextOperationAsync(

                input,
                actions);

            AnalyzeTextOperationState result = operation.Value;

            var extractiveResult = result.Actions.Items
      .OfType<ExtractiveSummarizationOperationResult>()
      .Single();

            var summaryResult = extractiveResult.Results;
            Console.WriteLine(JsonSerializer.Serialize(summaryResult));
            
            var summary = summaryResult.Documents
                .SelectMany(d => d.Sentences)
                .Select(s => s.Text)
                .ToList();

            return summary;
        }


        /// <summary>
        /// Genera un riassunto astrattivo del testo, creando nuove frasi che sintetizzano
        /// i concetti principali del documento invece di selezionare direttamente frasi
        /// presenti nel testo originale.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Le frasi generate che compongono il riassunto.</returns>
        public async Task<List<string>> AbstractiveSummarization(string text)
        {
            Console.WriteLine("****** AbstractiveSummarization");
            Console.WriteLine(text);
            TextAnalysisClient client = ClientFactory();

            var input = new MultiLanguageTextInput
            {
                MultiLanguageInputs =
        {
            new MultiLanguageInput("A", text)
            {
                Language = "it"
            }
        }
            };

            var actions = new List<AnalyzeTextOperationAction>
    {
        new AbstractiveSummarizationOperationAction()
    };

            var operation = await client.AnalyzeTextOperationAsync(
                input,
                actions);

            AnalyzeTextOperationState result = operation.Value;

            var abstractiveResult = result.Actions.Items
                .OfType<AbstractiveSummarizationOperationResult>()
                .Single();

            var summaryResult = abstractiveResult.Results;
            Console.WriteLine(JsonSerializer.Serialize(summaryResult));

            var summary = summaryResult.Documents
                .SelectMany(d => d.Summaries)
                .Select(s => s.Text)
                .ToList();

            return summary;
        }
    }
}