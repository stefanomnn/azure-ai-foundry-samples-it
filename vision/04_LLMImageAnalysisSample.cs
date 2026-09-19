using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Images;
using System;
using System.ClientModel;
using System.Reflection;

namespace AzureAiSamples.vision
{
    /// <summary>
    /// mostra come generare immagini tramite LLM, partendo sia da un testo di input che da una immagine di input
    /// Richiede modello adatto, es. gpt-5-image
    /// </summary>
    internal class LLMImageAnalysisSample
    {
        private OpenAIClient _OpenAIClient;
 
        public LLMImageAnalysisSample(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static LLMImageAnalysisSample FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new LLMImageAnalysisSample(projectClient.GetProjectOpenAIClient());
        }

        public static LLMImageAnalysisSample FromApiKey(string openAiURL, string password)
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
            return new LLMImageAnalysisSample(openAiClient);
        }



        /// <summary>
        /// genera una immagine partendo da un prompt (es. generami un logo per una azienda)
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public async Task TextToImage(string modelId)
        {

            ImageClient imgClient = _OpenAIClient.GetImageClient(modelId);

            Console.WriteLine("Generazione immagine in corso...");
            string prompt = "generami un immagine con la scritta 'stefano'";
            var opt = new ImageGenerationOptions
            {

                Background = GeneratedImageBackground.Opaque, // sfondo opaco o trasparente?
                ModerationLevel = GeneratedImageModerationLevel.Low, // piu blando nel rifiutarsi di generare immagini "dubbie"
                OutputCompressionFactor = 0, // 0-100: percentuale di compressione
                OutputFileFormat = GeneratedImageFileFormat.Jpeg,
                /*
                 * DALL·E 3 accetta i valori "standard" e "hd". In questo caso usa GeneratedImageQuality.Standard o GeneratedImageQuality.High. 1
                   I modelli GPT per immagini (gpt-image-1/2) accettano "low", "medium", "high". In questo caso usa GeneratedImageQuality.LowQuality/MediumQuality/HighQuality. 2
                 */
                Quality = GeneratedImageQuality.LowQuality,
                //ResponseFormat= GeneratedImageFormat.Bytes,
                Size = GeneratedImageSize.Auto,

                /* parametro style non è attualmente supportato: ecco i possibili valori se funzionasse:
                 * Natural: Dice al modello di puntare a un aspetto fotorealistico, sobrio e naturale. Se generi un paesaggio o una foto di un oggetto, i colori saranno fedeli alla realtà, le luci saranno morbide e sembrerà una vera foto scattata da una macchina fotografica.

                   Vivid: Dice al modello di spingere sull'iper-realismo artistico, colori sgargianti, contrasti forti e impatto visivo drammatico. È perfetto per illustrazioni, concept art, grafica pubblicitaria o immagini d'impatto dove vuoi che i colori "saltino fuori" dallo schermo.
                 */
                //Style = GeneratedImageStyle.Natural,
            };

            var jsonOpt = System.Text.Json.JsonSerializer.Serialize(opt);
            Console.WriteLine($"prompt: {prompt}");
            Console.WriteLine($"options:");
            Console.WriteLine(jsonOpt);

            ClientResult<GeneratedImage> result = await imgClient.GenerateImageAsync(prompt, opt);
            
            BinaryData binData = result.Value.ImageBytes;            
            byte[] byteArray = binData.ToArray();

            string outputPath = $@"C:\temp\{modelId}-text-to-image.jpeg";
            Console.WriteLine($"Immagine generata con successo. dimensione: {byteArray.Length} bytes. l'immagine si trova in questo path: {outputPath}");
            File.WriteAllBytes(outputPath, byteArray);

        }


        void ForceLastVersion(OpenAIClientOptions opt)
        {
            string targetVersion = "2025-04-01-preview";

            // Recuperiamo la proprietà "Version" per leggere il valore attuale
            var property = typeof(OpenAIClientOptions).GetProperty("Version", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null)
            {
                string currentVersion = property.GetValue(opt) as string;

                // Se la versione attuale è già maggiore o uguale alla target, non facciamo nulla
                // (il confronto stringa alfabetico/alfanumerico funziona bene per date stile "YYYY-MM-DD")
                if (!string.IsNullOrEmpty(currentVersion) && string.Compare(currentVersion, targetVersion, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }
            }

            // Usiamo la reflection per forzare la stringa della versione nel backing field
            var backingField = typeof(OpenAIClientOptions).GetField("<Version>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

            if (backingField != null)
            {
                backingField.SetValue(opt, targetVersion);
            }
            else
            {
                property?.SetValue(opt, targetVersion);
            }
        }

        /// <summary>
        /// genera una immagine di output partendo da una di input (es. toglie lo sfondo, oscura dati sensibili...)
        /// </summary>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public async Task ImageToImage(string modelId)
        {



            ImageClient imgClient = _OpenAIClient.GetImageClient(modelId);


            const string filePath = "assets//cf_mario_rossi.jpg";
            using var fileStream = new FileStream(filePath, FileMode.Open);

            ClientResult<GeneratedImage> result = await imgClient.GenerateImageEditAsync(fileStream, Path.GetFileName(filePath), "modifica il nome presente nella foto in GIUSEPPE", new ImageEditOptions
            {
                InputFidelity = ImageInputFidelity.High, // quanto il modello deve attenersi all'immagine di input

                Size = GeneratedImageSize.Auto,

                
            });

          

            BinaryData binData = result.Value.ImageBytes;
            File.WriteAllBytes($@"C:\temp\{modelId}-image-to-image.jpeg", binData.ToArray());
        }
    }
}
