using Azure;
using Azure.AI.Vision.ImageAnalysis;
using System.Text;

namespace AzureAiSamples
{
    internal class ClassicalVisionAnalysisSample
    {
        private readonly string _EndpointURL;
        private readonly string _ApiKey;

        public ClassicalVisionAnalysisSample(string endpointURL, string apiKey)
        {
            _EndpointURL = endpointURL;
            _ApiKey = apiKey;
        }

        /// <summary>
        /// Analizza un'immagine utilizzando tutte le funzionalità disponibili
        /// di Azure AI Vision Image Analysis:
        /// caption, dense captions, tags, object detection, people detection,
        /// OCR e smart crops.
        /// </summary>
        /// <param name="filePath">Percorso del file immagine.</param>
        /// <returns>Descrizione testuale dei risultati dell'analisi.</returns>
        public async Task<string> AnalyzeImage(string filePath)
        {
            ImageAnalysisClient client = new ImageAnalysisClient(
                new Uri(_EndpointURL),
                new AzureKeyCredential(_ApiKey));

            using FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read);

            VisualFeatures features =
                //VisualFeatures.Caption | // riassunto su cosa rappresenta una immagine (es. paesaggio)
                //VisualFeatures.DenseCaptions | // riassunto per zone (es. in alto a destra c'è il sole)
                VisualFeatures.Tags |    // parole chiave associate all'immagine
                VisualFeatures.Objects | // oggetti riconosciuti, con relative coordinate . gli oggetti possono avere a loro volta dei tags
                VisualFeatures.People | // presenza di persone o volti , con relative coordinate e livello di confidenza. non RICONOSCE volti, nè età genere, sentimento etc.
                VisualFeatures.Read | // OCR: prova a estrarre il testo
                VisualFeatures.SmartCrops; //Analizza l'immagine, trova qual è il vero punto di interesse (il soggetto principale) e dammi le coordinate per ritagliarla correttamente in vari formati (es. quadrato, verticale, orizzontale) mantenendo dentro il soggetto. estituisce una lista di coordinate (rettangoli) calcolate su diverse aspect ratio (proporzioni)

            // attenzione Caption e DenseCaptions non funzionano in tutte le regions,
            // gemini dice:le funzionalità di generazione di didascalie (VisualFeatures.Caption e VisualFeatures.DenseCaptions) richiedono obbligatoriamente che la risorsa Computer Vision sia creata in una regione di Azure dotata di supporto GPU.
            //  Dato che la tua risorsa si trova in East US 2(regione che non supporta i modelli di Caption / DenseCaptions per Computer Vision), la chiamata fallisce non appena flagghi quella specifica opzione.

                ImageAnalysisResult result = await client.AnalyzeAsync(BinaryData.FromStream(stream),features);

            var output = new StringBuilder();

            // ------------------------------------------------------------
            // CAPTION
            // ------------------------------------------------------------

            output.AppendLine("=== CAPTION ===");

            if (result.Caption != null)
            {
                output.AppendLine(
                    $"Text: {result.Caption.Text}");

                output.AppendLine(
                    $"Confidence: {result.Caption.Confidence:F2}");
            }


            // ------------------------------------------------------------
            // DENSE CAPTIONS
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== DENSE CAPTIONS ===");

            if (result.DenseCaptions != null)
            {
                foreach (var caption in result.DenseCaptions.Values)
                {
                    output.AppendLine(
                        $"Text: {caption.Text}");

                    output.AppendLine(
                        $"Confidence: {caption.Confidence:F2}");

                    output.AppendLine(
                        $"BoundingBox: " +
                        $"X={caption.BoundingBox.X}, " +
                        $"Y={caption.BoundingBox.Y}, " +
                        $"Width={caption.BoundingBox.Width}, " +
                        $"Height={caption.BoundingBox.Height}");

                    output.AppendLine();
                }
            }


            // ------------------------------------------------------------
            // TAGS
            // ------------------------------------------------------------

            output.AppendLine("=== TAGS ===");

            if (result.Tags != null)
            {
                foreach (var tag in result.Tags.Values)
                {
                    output.AppendLine(
                        $"{tag.Name} - Confidence: {tag.Confidence:F2}");
                }
            }


            // ------------------------------------------------------------
            // OBJECTS
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== OBJECTS ===");

            if (result.Objects != null)
            {
                foreach (var detectedObject in result.Objects.Values)
                {
                    // anche i singoli oggetti hanno specifici tag
                    foreach (var tag in detectedObject.Tags)
                    {
                        output.AppendLine(
                            $"{tag.Name} - " +
                            $"Confidence: {tag.Confidence:F2}");

                        output.AppendLine(
                            $"BoundingBox: " +
                            $"X={detectedObject.BoundingBox.X}, " +
                            $"Y={detectedObject.BoundingBox.Y}, " +
                            $"Width={detectedObject.BoundingBox.Width}, " +
                            $"Height={detectedObject.BoundingBox.Height}");
                    }

                    output.AppendLine(
                        $"BoundingBox: " +
                        $"X={detectedObject.BoundingBox.X}, " +
                        $"Y={detectedObject.BoundingBox.Y}, " +
                        $"Width={detectedObject.BoundingBox.Width}, " +
                        $"Height={detectedObject.BoundingBox.Height}");
                }
            }


            // ------------------------------------------------------------
            // PEOPLE
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== PEOPLE ===");

            if (result.People != null)
            {
                foreach (var person in result.People.Values)
                {
                    output.AppendLine(
                        $"Confidence: {person.Confidence:F2}");

                    output.AppendLine(
                        $"BoundingBox: " +
                        $"X={person.BoundingBox.X}, " +
                        $"Y={person.BoundingBox.Y}, " +
                        $"Width={person.BoundingBox.Width}, " +
                        $"Height={person.BoundingBox.Height}");
                }
            }


            // ------------------------------------------------------------
            // READ / OCR
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== READ / OCR ===");

            if (result.Read != null)
            {
                foreach (var block in result.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        output.AppendLine(
                            $"Line: {line.Text}");

                        foreach (var word in line.Words)
                        {
                            output.AppendLine(
                                $"  Word: {word.Text} " +
                                $"Confidence: {word.Confidence:F2}");
                        }
                    }
                }
            }


            // ------------------------------------------------------------
            // SMART CROPS
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== SMART CROPS ===");

            if (result.SmartCrops != null)
            {
                foreach (var crop in result.SmartCrops.Values)
                {
                    output.AppendLine(
                        $"BoundingBox: " +
                        $"X={crop.BoundingBox.X}, " +
                        $"Y={crop.BoundingBox.Y}, " +
                        $"Width={crop.BoundingBox.Width}, " +
                        $"Height={crop.BoundingBox.Height}");
                }
            }


            // ------------------------------------------------------------
            // MODEL
            // ------------------------------------------------------------

            output.AppendLine();
            output.AppendLine("=== MODEL ===");
            output.AppendLine(result.ModelVersion);

            return output.ToString();
        }
    }
}