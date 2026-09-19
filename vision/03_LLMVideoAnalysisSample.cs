using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Videos;
using System.ClientModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AzureAiSamples.vision
{
    /// <summary>
    /// CLASSE DIDATTICA: esplora le capacità di generazione, analisi e modifica video
    /// tramite LLM (modelli multimodali + Sora/DALL·E video).
    ///
    /// Non contiene try/catch, retry, validazione input: è codice "pulito" a scopo di studio.
    ///
    /// COSA IMPARERAI:
    ///   - Text-to-Video: generare un video da una descrizione testuale (Sora)
    ///   - Video Remix / Video-to-Video: modificare un video esistente tramite prompt
    ///   - Video Analysis: analizzare il contenuto di un video inviandolo a un LLM multimodale
    ///     (GPT-5, GPT-4o) tramite chat completions con input video
    ///   - Tutte le operazioni CRUD: GetVideo, GetVideos, DeleteVideo, DownloadVideo
    ///   - Tutte le proprietà principali: durata, risoluzione, qualità, stile,
    ///     analoghe a "fidelity" per le immagini
    /// </summary>
    internal class LLMVideoAnalysisSample
    {
        private OpenAIClient _OpenAIClient;

        public LLMVideoAnalysisSample(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static LLMVideoAnalysisSample FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new LLMVideoAnalysisSample(projectClient.GetProjectOpenAIClient());
        }

        public static LLMVideoAnalysisSample FromApiKey(string openAiURL, string password)
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
            return new LLMVideoAnalysisSample(openAiClient);
        }


        // =========================================================================
        //  1. TEXT-TO-VIDEO: Generazione di video da prompt testuale (Sora)
        // =========================================================================

        /// <summary>
        /// Genera un video a partire da un prompt testuale.
        ///
        /// Usa il VideoClient in modalità "protocol" (multipart/form-data),
        /// ovvero costruiamo manualmente il payload HTTP per massima flessibilità.
        ///
        /// PROPRIETÀ PRINCIPALI (passate come campi del multipart):
        ///   - "model":        il modello video (es. "sora-2", "sora-2-turbo")
        ///   - "prompt":       descrizione testuale del video da generare
        ///   - "size":         risoluzione del video (es. "1920x1080", "1280x720")
        ///   - "duration":     durata in secondi (es. 4, 8, 12 — dipende dal modello)
        ///   - "quality":      qualità di generazione ("standard" / "high" / "turbo")
        ///   - "style":        stile visivo ("natural" / "vivid" — analogo a DALL·E)
        ///   - "fps":          frame-per-second (tipicamente 24 o 30)
        ///   - "aspect_ratio": rapporto d'aspetto ("16:9", "9:16", "1:1")
        ///   - "seed":         seed per riproducibilità (opzionale)
        ///   - "negative_prompt": cosa NON mostrare nel video
        ///
        /// NOTA: l'operazione è asincrona! CreateVideo restituisce subito un ID;
        ///       bisogna poi fare polling con GetVideo fino a status="completed".
        /// </summary>
        /// <param name="modelId">ID del modello video, es. "sora-2"</param>
        /// <returns>L'ID del video creato e il suo stato iniziale</returns>
        public async Task<(string VideoId, string Status)> TextToVideo(string modelId)
        {
            VideoClient videoClient = CreateVideoClient();
            

            // Costruiamo il payload multipart/form-data
            // (modo "protocol": inviamo campi HTTP raw per massimo controllo)
            var boundary = Guid.NewGuid().ToString();
            var contentType = $"multipart/form-data; boundary=\"{boundary}\"";
            using var multipart = new MultipartFormDataContent(boundary);

            // --- Campi OBBLIGATORI ---
            multipart.Add(new StringContent(modelId, Encoding.UTF8, "text/plain"), "model");
            multipart.Add(new StringContent(
                "Un gatto soriano che suona il pianoforte su un palco teatrale, " +
                "illuminazione soffusa, fumo scenico, primo piano",
                Encoding.UTF8, "text/plain"), "prompt");

            // --- Campi OPZIONALI (PROPS PRINCIPALI) ---
            // Risoluzione: le dimensioni in pixel del video generato
            multipart.Add(new StringContent("1920x1080", Encoding.UTF8, "text/plain"), "size");

            // Durata: secondi di video. Sora-2 supporta 4, 8, 12 secondi
            multipart.Add(new StringContent("8", Encoding.UTF8, "text/plain"), "duration");

            // Qualità: "standard" o "high" (alta fedeltà, più tempo/costo)
            multipart.Add(new StringContent("high", Encoding.UTF8, "text/plain"), "quality");

            // Stile: "natural" = fotorealistico, "vivid" = iper-realistico/artistico
            //        (ANALOGO allo Style di DALL·E per le immagini)
            multipart.Add(new StringContent("natural", Encoding.UTF8, "text/plain"), "style");

            // FPS: frame per secondo. 24 = cinematico, 30 = standard video
            multipart.Add(new StringContent("24", Encoding.UTF8, "text/plain"), "fps");

            // Aspect ratio alternativo alla size (se il modello lo supporta)
            multipart.Add(new StringContent("16:9", Encoding.UTF8, "text/plain"), "aspect_ratio");

            // Seed: per generazioni deterministiche/riproducibili
            multipart.Add(new StringContent("42", Encoding.UTF8, "text/plain"), "seed");

            // Negative prompt: cosa escludere dalla generazione
            multipart.Add(new StringContent(
                "testo, watermark, loghi, bassa qualità, sfocato",
                Encoding.UTF8, "text/plain"), "negative_prompt");

            // Inviamo la richiesta
            using var bodyStream = await multipart.ReadAsStreamAsync();
            var createResult = await videoClient.CreateVideoAsync(
                BinaryContent.Create(bodyStream), contentType);

            // Parsiamo la risposta JSON per estrarre ID e stato
            var createRaw = createResult.GetRawResponse().Content.ToString();
            using var doc = JsonDocument.Parse(createRaw);
            var id = doc.RootElement.GetProperty("id").GetString()!;
            var status = doc.RootElement.GetProperty("status").GetString()!;

            Console.WriteLine($"[TextToVideo] Creato video ID={id}, status={status}");
            return (id, status);
        }
// =========================================================================
        //  2. POLLING: Attendere il completamento della generazione
        // =========================================================================

        /// <summary>
        /// Esegue il polling dello stato di un video fino al completamento.
        /// Stati tipici: "queued" -> "processing" -> "completed" (o "failed")
        /// PROPRIETÀ restituite da GetVideo (nel JSON):
        /// id, status, progress, created_at, duration, size, model, error
        /// </summary>
        public async Task<string> WaitForCompletion(
            string modelId, string videoId, int pollIntervalMs = 3000)
        {
            VideoClient videoClient = CreateVideoClient();

            while (true)
            {
                ClientResult getResult = await videoClient.GetVideoAsync(videoId);
                string json = getResult.GetRawResponse().Content.ToString();

                using var doc = JsonDocument.Parse(json);
                string status = doc.RootElement.GetProperty("status").GetString()!;

                Console.WriteLine($"[Polling] Video {videoId}: status={status}");

                if (status == "completed" || status == "failed" || status == "cancelled")
                {
                    if (status == "completed")
                        Console.WriteLine("[Polling] Video COMPLETATO!");
                    else
                    {
                        string? error = doc.RootElement.TryGetProperty("error", out var errEl)
                            ? errEl.GetString() : "nessun dettaglio";
                        Console.WriteLine($"[Polling] Video terminato " +
                            $"stato={status}, errore={error}");
                    }
                    return json;
                }

                await Task.Delay(pollIntervalMs);
            }
        }

        // =========================================================================
        //  3. DOWNLOAD: Scaricare il video generato
        // =========================================================================

        /// <summary>
        /// Scarica il file video generato e lo salva su disco (.mp4).
        /// </summary>
        public async Task DownloadVideo(string modelId, string videoId, string outputPath)
        {
            VideoClient videoClient = CreateVideoClient();

            ClientResult downloadResult = await videoClient.DownloadVideoAsync(videoId);
            BinaryData videoBytes = downloadResult.GetRawResponse().Content;

            await File.WriteAllBytesAsync(outputPath, videoBytes.ToArray());
            Console.WriteLine($"[Download] Video salvato in: {outputPath}");
        }
// =========================================================================
        //  4. VIDEO REMIX / VIDEO-TO-VIDEO
        // =========================================================================

        /// <summary>
        /// Crea un "remix" di un video esistente, modificandolo tramite prompt.
        ///
        /// OPERAZIONE ANALOGA a ImageEdit (ImageToImage) per i video.
        /// Invece di ImageInputFidelity, qui usiamo "strength":
        ///   - "video_id":       ID del video sorgente da remixare
        ///   - "prompt":         descrizione delle modifiche da applicare
        ///   - "strength":       quanto il remix si discosta dall'originale (0-1)
        ///                       0 = massima fedeltà (= ImageInputFidelity.High)
        ///                       1 = massima libertà  (= ImageInputFidelity.Low)
        ///   - "preserve_audio": mantenere l'audio originale (true/false)
        ///   - "style":          stile visivo del remix
        ///   - "quality":        qualità del video remixato
        ///   - "seed":           seed per riproducibilità
        ///
        /// NOTA: CreateVideoRemix è asincrono come CreateVideo.
        /// </summary>
        public async Task<(string VideoId, string Status)> RemixVideo(
            string modelId, string sourceVideoId)
        {
            VideoClient videoClient = CreateVideoClient();

            var boundary = Guid.NewGuid().ToString();
            var contentType = $"multipart/form-data; boundary=\"{boundary}\"";
            using var multipart = new MultipartFormDataContent(boundary);

            multipart.Add(new StringContent(modelId, Encoding.UTF8, "text/plain"), "model");
            multipart.Add(new StringContent(sourceVideoId, Encoding.UTF8, "text/plain"), "video_id");

            multipart.Add(new StringContent(
                "Cambia lo sfondo in un paesaggio montano innevato e rendi l'illuminazione più calda",
                Encoding.UTF8, "text/plain"), "prompt");

            // Strength: 0.0 = massima fedeltà, 1.0 = massima libertà creativa
            multipart.Add(new StringContent("0.5", Encoding.UTF8, "text/plain"), "strength");

            // Preservare la traccia audio originale?
            multipart.Add(new StringContent("true", Encoding.UTF8, "text/plain"), "preserve_audio");

            multipart.Add(new StringContent("high", Encoding.UTF8, "text/plain"), "quality");
            multipart.Add(new StringContent("natural", Encoding.UTF8, "text/plain"), "style");
            multipart.Add(new StringContent("1920x1080", Encoding.UTF8, "text/plain"), "size");
            multipart.Add(new StringContent("123", Encoding.UTF8, "text/plain"), "seed");

            using var bodyStream = await multipart.ReadAsStreamAsync();
            var result = await videoClient.CreateVideoRemixAsync(
                sourceVideoId, BinaryContent.Create(bodyStream), contentType);

            var raw = result.GetRawResponse().Content.ToString();
            using var doc = JsonDocument.Parse(raw);
            var id = doc.RootElement.GetProperty("id").GetString()!;
            var status = doc.RootElement.GetProperty("status").GetString()!;

            Console.WriteLine($"[Remix] Creato remix ID={id}, status={status}");
            return (id, status);
        }

        // =========================================================================
        //  5. LIST & DELETE
        // =========================================================================

        /// <summary> Elenca tutti i video generati (paginazione: limit, after). </summary>
        public async Task<string> ListVideos(string modelId, int limit = 10)
        {
            VideoClient videoClient = CreateVideoClient();

            // GetVideosAsync restituisce un AsyncCollectionResult non-generic
            // (collezione paginata di risultati). Per semplicità didattica,
            // mostriamo la chiamata e rimandiamo alla documentazione SDK.
            _ = videoClient.GetVideosAsync(limit);
            Console.WriteLine($"[List] Richiesta elenco video inviata " +
                $"(limit={limit}). La risposta è una collezione paginata.");
            return "Vedi documentazione SDK per iterare AsyncCollectionResult";
        }

        /// <summary> Elimina un video generato (per pulizia/quota). </summary>
        public async Task DeleteVideo(string modelId, string videoId)
        {
            VideoClient videoClient = CreateVideoClient();

            ClientResult result = await videoClient.DeleteVideoAsync(videoId);
            Console.WriteLine($"[Delete] Video {videoId} eliminato. " +
                $"Status: {result.GetRawResponse().Status}");
        }
// =========================================================================
        //  6. VIDEO ANALYSIS: Analizzare un video con LLM multimodale (GPT-5)
        // =========================================================================

        /// <summary>
        /// Analizza il contenuto di un video inviandolo a un LLM multimodale
        /// (GPT-5, GPT-4o) tramite chat completions.
        ///
        /// Approccio: il video viene caricato come ChatMessageContentPart di tipo "file",
        /// il LLM "guarda" i frame e risponde a domande sul contenuto.
        ///
        /// PROPRIETÀ PRINCIPALI per l'analisi video:
        ///   - MaxOutputTokenCount: lunghezza massima della risposta
        ///   - Temperature: 0=fattuale/precisa, 1=più creativa
        ///   - TopP: nucleus sampling (1.0=tutte le parole, &lt;1.0=più focalizzato)
        ///   - FrequencyPenalty: penalizza ripetizioni (-2.0 a 2.0)
        ///   - PresencePenalty: incoraggia nuovi topic (-2.0 a 2.0)
        /// </summary>
        /// <param name="modelId">Modello LLM multimodale (es. "gpt-5-mini")</param>
        /// <param name="videoFilePath">Percorso del file video locale</param>
        /// <param name="userPrompt">Domanda da fare sul video</param>
        /// <returns>La risposta testuale del modello</returns>
        public async Task<string> AnalyzeVideo(
            string modelId, string videoFilePath, string userPrompt)
        {
           
            var chatClient = _OpenAIClient.GetChatClient(modelId);

            // 2) Prepariamo il video come file binario
            byte[] videoBytes = await File.ReadAllBytesAsync(videoFilePath);
            string mimeType = GetVideoMimeType(videoFilePath);

            // Messaggio utente con DUE content parts: prompt + file video
            List<OpenAI.Chat.ChatMessage> messages = new()
            {
                new OpenAI.Chat.SystemChatMessage(
                    "Sei un analista video esperto. Analizza il video fornito " +
                    "e rispondi in modo dettagliato e strutturato."),

                new OpenAI.Chat.UserChatMessage(
                    OpenAI.Chat.ChatMessageContentPart.CreateTextPart(userPrompt),
                    OpenAI.Chat.ChatMessageContentPart.CreateFilePart(
                        BinaryData.FromBytes(videoBytes),
                        mimeType,
                        Path.GetFileName(videoFilePath)))
            };

            // 3) Opzioni: controllano la "fedeltà" e il comportamento dell'analisi
            OpenAI.Chat.ChatCompletionOptions options = new()
            {
                MaxOutputTokenCount = 2000,
                Temperature = 0.3f,         // analisi fattuale
                TopP = 0.9f,                // nucleus sampling
                FrequencyPenalty = 0,       // nessuna penalità ripetizioni
                PresencePenalty = 0,        // nessuna penalità topic
            };

            // 4) Inviamo la richiesta
            OpenAI.Chat.ChatCompletion completion = await chatClient.CompleteChatAsync(
                messages, options);

            string response = string.Join("\n",
                completion.Content.Select(c => c.Text));

            Console.WriteLine($"[AnalyzeVideo] Token: " +
                $"in={completion.Usage.InputTokenCount}, " +
                $"out={completion.Usage.OutputTokenCount}");

            return response;
        }
// =========================================================================
        //  7. ANALISI STRUTTURATA con prompt complesso
        // =========================================================================

        /// <summary>
        /// Analisi video con prompt strutturato multi-sezione.
        /// Mostra come combinare più content parts in un unico messaggio.
        /// </summary>
        public async Task<string> AnalyzeVideoWithKeyFrames(
            string modelId, string videoFilePath)
        {
         
            var chatClient = _OpenAIClient.GetChatClient(modelId);

            byte[] videoBytes = await File.ReadAllBytesAsync(videoFilePath);
            string mimeType = GetVideoMimeType(videoFilePath);

            List<OpenAI.Chat.ChatMessage> messages = new()
            {
                new OpenAI.Chat.SystemChatMessage(
                    "Sei un esperto di analisi video forense."),

                new OpenAI.Chat.UserChatMessage(
                    OpenAI.Chat.ChatMessageContentPart.CreateTextPart(
                        "Analizza questo video e produci un report con:\n" +
                        "1. Riepilogo generale (max 3 frasi)\n" +
                        "2. Elenco delle scene principali con timestamp\n" +
                        "3. Oggetti e persone identificati\n" +
                        "4. Azioni ed eventi chiave\n" +
                        "5. Tono emotivo e atmosfera\n" +
                        "6. Qualità tecnica percepita"),
                    OpenAI.Chat.ChatMessageContentPart.CreateFilePart(
                        BinaryData.FromBytes(videoBytes),
                        mimeType,
                        Path.GetFileName(videoFilePath)))
            };

            OpenAI.Chat.ChatCompletionOptions options = new()
            {
                MaxOutputTokenCount = 3000,
                Temperature = 0.4f,
            };

            OpenAI.Chat.ChatCompletion completion = await chatClient.CompleteChatAsync(
                messages, options);

            string response = string.Join("\n",
                completion.Content.Select(c => c.Text));

            Console.WriteLine($"[AnalyzeWithKeyFrames] Token: " +
                $"in={completion.Usage.InputTokenCount}, " +
                $"out={completion.Usage.OutputTokenCount}");

            return response;
        }
// =========================================================================
        //  METODI HELPER
        // =========================================================================

        /// <summary>
        /// Crea un VideoClient connesso all'endpoint Azure OpenAI.
        ///
        /// AzureOpenAIClient eredita da OpenAI.OpenAIClient, quindi possiamo
        /// usare direttamente GetVideoClient() mantenendo l'autenticazione
        /// Entra ID (DefaultAzureCredential) già configurata.
        /// </summary>
        private VideoClient CreateVideoClient()
        {
            
            return _OpenAIClient.GetVideoClient();
        }

        /// <summary>
        /// Determina il MIME type in base all'estensione del file video.
        /// </summary>
        private static string GetVideoMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".mp4"  => "video/mp4",
                ".mov"  => "video/quicktime",
                ".avi"  => "video/x-msvideo",
                ".webm" => "video/webm",
                ".mkv"  => "video/x-matroska",
                ".mpeg" => "video/mpeg",
                ".mpg"  => "video/mpeg",
                ".wmv"  => "video/x-ms-wmv",
                ".flv"  => "video/x-flv",
                _       => "video/mp4",
            };
        }

        // =========================================================================
        //  8. ESEMPIO FLUSSO COMPLETO END-TO-END (DIDATTICO)
        // =========================================================================

        /// <summary>
        /// Metodo dimostrativo che esegue l'intero flusso:
        ///   1. Genera un video (text-to-video)
        ///   2. Attende il completamento (polling)
        ///   3. Scarica il video generato
        ///   4. Crea un remix del video (video-to-video)
        ///   5. Analizza entrambi i video con un LLM
        ///   6. Pulisce eliminando i video
        ///
        /// Puramente didattico: mostra la sequenza completa di operazioni.
        /// </summary>
        /// <param name="videoModelId">Modello generazione video (es. "sora-2")</param>
        /// <param name="chatModelId">Modello LLM analisi (es. "gpt-5-mini")</param>
        public async Task RunFullDemo(string videoModelId, string chatModelId)
        {
            Console.WriteLine("===== DEMO COMPLETA VIDEO LLM =====");
            Console.WriteLine();

            // STEP 1: Genera video
            Console.WriteLine("--- STEP 1: Text-to-Video ---");
            var (videoId, status) = await TextToVideo(videoModelId);
            Console.WriteLine($"Video creato: {videoId} (status={status})");

            // STEP 2: Polling fino al completamento
            Console.WriteLine("\n--- STEP 2: Attesa completamento ---");
            string completedJson = await WaitForCompletion(videoModelId, videoId);
            Console.WriteLine($"Video completato: " +
                $"{completedJson[..Math.Min(200, completedJson.Length)]}...");

            // STEP 3: Download
            Console.WriteLine("\n--- STEP 3: Download ---");
            string outputPath = Path.Combine(Path.GetTempPath(), $"{videoId}.mp4");
            await DownloadVideo(videoModelId, videoId, outputPath);

            // STEP 4: Remix del video (video-to-video)
            Console.WriteLine("\n--- STEP 4: Remix ---");
            var (remixId, remixStatus) = await RemixVideo(videoModelId, videoId);
            Console.WriteLine($"Remix creato: {remixId} (status={remixStatus})");

            await WaitForCompletion(videoModelId, remixId);
            string remixPath = Path.Combine(Path.GetTempPath(), $"{remixId}_remix.mp4");
            await DownloadVideo(videoModelId, remixId, remixPath);

            // STEP 5: Analizza entrambi i video con LLM
            Console.WriteLine("\n--- STEP 5: Analisi LLM ---");
            Console.WriteLine("Analisi video originale:");
            string report1 = await AnalyzeVideo(chatModelId, outputPath,
                "Descrivi dettagliatamente cosa vedi in questo video.");
            Console.WriteLine(report1);

            Console.WriteLine("\nAnalisi video remixato:");
            string report2 = await AnalyzeVideo(chatModelId, remixPath,
                "Confronta questo video con l'originale. Cosa è cambiato?");
            Console.WriteLine(report2);

            // STEP 6: Pulizia
            Console.WriteLine("\n--- STEP 6: Pulizia ---");
            await DeleteVideo(videoModelId, videoId);
            await DeleteVideo(videoModelId, remixId);
            Console.WriteLine("Video eliminati dal server.");

            Console.WriteLine("\n===== DEMO COMPLETATA =====");
        }
}
}