using AzureAiSamples.contentUnderstanding;
using AzureAiSamples.otherTopics;
using AzureAiSamples.speech;
using AzureAiSamples.textAnalysis;
using AzureAiSamples.vision;

namespace AzureAiSamples;

// --- Enum con tutte le feature ------------------------------------------
internal enum FeatureType
{
    // LLM e Agenti
    ChiamataBase,
    InputMultimodale,
    OutputStrutturato,
    Streaming,
    FunctionCalling,
    WebSearch,
    CodeInterpreter,
    FileSearch,
    McpServer,
    ConversazioniStateful,    // solo Project
    AgentTemporaneo,          // solo Project

    // Content Safety
    ContentSafetyText,
    ContentSafetyTextBlocklist,
    ContentSafetyImage,
    ContentSafetyBlocklist,
    ContentSafetyPromptShield,
    ContentSafetyGroundedness,
    ContentSafetyProtectedText,
    ContentSafetyProtectedCode,
    ContentSafetyMultimodal,
    ContentSafetyCustomRapid,
    ContentSafetyCustomStandard,
    ContentSafetyTaskAdherence,

    // Speech
    FastTranscription,
    SpeechToText,
    SpeechToTextLLM,
    TextToSpeech,

    // Vision
    ImageAnalysis,
    GenerazioneImmagini,
    GenerazioneVideo,

    // NLP e Traduzione
    TextAnalysis,
    TraduzioneTesti,
    TraduzioneDocumenti,

    // Altro
    Embedding,
    StimaToken,
    OpenTelemetry,
  
    // Content Understanding
    CuOcrRead,
    CuLayout,
   
    CuDocumentSearch,
    CuImageSearch,
    CuAudioAnalysis,
    CuVideoAnalysis,
    CuCustomAnalyzer,

    // Speech
    SpeechTranslation,

    // Altro
    VideoIndexer,
}

internal class Program
{
    // --- Lista ordinata: (Feature, Categoria, Etichetta, SoloProject?) ---
    // Per aggiungere una nuova feature basta inserire una riga qui.
    // L'indice visualizzato a menu e' calcolato dinamicamente.

    private static readonly List<(FeatureType Feature, string Category, string Label, bool OnlyProject)> MenuItems = new()
    {
        // -- LLM e Agenti --
        (FeatureType.ChiamataBase,           "LLM e Agenti", "Chiamata base + contesto + parametri",   false),
        (FeatureType.InputMultimodale,       "LLM e Agenti", "Input multimodale (testo + immagine)",    false),
        (FeatureType.OutputStrutturato,      "LLM e Agenti", "Output strutturato + refusal + retry",    false),
        (FeatureType.Streaming,              "LLM e Agenti", "Streaming token-per-token",               false),
        (FeatureType.FunctionCalling,        "LLM e Agenti", "Function calling custom",                 false),
        (FeatureType.WebSearch,              "LLM e Agenti", "Web Search tool",                         false),
        (FeatureType.CodeInterpreter,        "LLM e Agenti", "Code Interpreter tool",                   false),
        (FeatureType.FileSearch,             "LLM e Agenti", "File Search + Vector Store",              false),
        (FeatureType.McpServer,              "LLM e Agenti", "MCP Server tool",                         false),
        (FeatureType.ConversazioniStateful,  "LLM e Agenti", "Conversazioni stateful",                  true),
        (FeatureType.AgentTemporaneo,        "LLM e Agenti", "Agent temporaneo",                        true),

        // -- Content Safety --
        (FeatureType.ContentSafetyText,          "Content Safety", "Text Analysis",                       false),
        (FeatureType.ContentSafetyTextBlocklist, "Content Safety", "Text + Blocklist",                    false),
        (FeatureType.ContentSafetyImage,         "Content Safety", "Image Analysis",                      false),
        (FeatureType.ContentSafetyBlocklist,     "Content Safety", "Blocklist Management",                false),
        (FeatureType.ContentSafetyPromptShield,  "Content Safety", "Prompt Shield (user + document)",     false),
        (FeatureType.ContentSafetyGroundedness,  "Content Safety", "Groundedness (QnA + Summarization)",  false),
        (FeatureType.ContentSafetyProtectedText, "Content Safety", "Protected Material (text)",           false),
        (FeatureType.ContentSafetyProtectedCode, "Content Safety", "Protected Material (code)",           false),
        (FeatureType.ContentSafetyMultimodal,    "Content Safety", "Multimodal Analysis",                 false),
        (FeatureType.ContentSafetyCustomRapid,   "Content Safety", "Custom Categories (rapid)",           false),
        (FeatureType.ContentSafetyCustomStandard,"Content Safety", "Custom Categories (standard)",        false),
        (FeatureType.ContentSafetyTaskAdherence, "Content Safety", "Task Adherence",                      false),

        // -- Content Understanding --
        (FeatureType.CuOcrRead,         "Content Understanding", "OCR puro (prebuilt-read) — SINGLE-TASK",          false),
        (FeatureType.CuLayout,          "Content Understanding", "Layout analysis (prebuilt-layout)",              false),
         (FeatureType.CuDocumentSearch,  "Content Understanding", "Output RAG-ready (prebuilt-documentSearch)",     false),
        (FeatureType.CuImageSearch,     "Content Understanding", "Visual Understanding (prebuilt-imageSearch)",    false),
        (FeatureType.CuAudioAnalysis,   "Content Understanding", "Analisi Audio (prebuilt-audio)",                 false),
        (FeatureType.CuVideoAnalysis,   "Content Understanding", "Analisi Video (prebuilt-videoSearch)",           false),
        (FeatureType.CuCustomAnalyzer,  "Content Understanding", "Custom Analyzer con field_schema",               false),

        // -- Speech --
        (FeatureType.SpeechTranslation,  "Speech",               "Speech Translation (parla IT → leggi EN)",       false),
        (FeatureType.FastTranscription, "Speech", "Fast Transcription (REST)",          false),
        (FeatureType.SpeechToText,      "Speech", "Speech-to-Text real-time (SDK)",     false),
        (FeatureType.SpeechToTextLLM,   "Speech", "Speech-to-Text via LLM",             false),
        (FeatureType.TextToSpeech,      "Speech", "Text-to-Speech",                     false),

        // -- Vision --
        (FeatureType.ImageAnalysis,       "Vision", "Image Analysis (classica)",        false),
        (FeatureType.GenerazioneImmagini, "Vision", "Generazione immagini (DALL-E)",    false),
        (FeatureType.GenerazioneVideo,    "Vision", "Generazione video (Sora)",         false),

        // -- NLP e Traduzione --
        (FeatureType.TextAnalysis,       "NLP e Traduzione", "Text Analysis (lingua, sentiment, NER...)", false),
        (FeatureType.TraduzioneTesti,    "NLP e Traduzione", "Traduzione testi",                          false),
        (FeatureType.TraduzioneDocumenti,"NLP e Traduzione", "Traduzione documenti",                     false),

        // -- Altro --
        (FeatureType.Embedding,            "Altro", "Embedding + similarita'",       false),
        (FeatureType.StimaToken,           "Altro", "Stima token (Tiktoken)",        false),
        (FeatureType.OpenTelemetry,        "Altro", "OpenTelemetry",                 false),
       (FeatureType.VideoIndexer,         "Altro", "Video Indexer (REST)",          false),
    };

    static async Task Main(string[] args)
    {
        Variabili.Load();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== AZURE AI FOUNDRY � SAMPLES C# ===\n");
            Console.WriteLine("Autenticazione:");
            Console.WriteLine("  [P] Project URL  (InteractiveBrowserCredential)");
            Console.WriteLine("  [A] API Key      (OpenAI / OpenRouter / ...)");
            Console.WriteLine("  [E] Esci");
            Console.Write("\n> ");

            var auth = Console.ReadLine()?.Trim().ToUpper();
            if (auth == "E" || string.IsNullOrEmpty(auth)) break;
            if (auth != "P" && auth != "A")
            {
                Console.WriteLine("Scelta non valida. Premi un tasto...");
                Console.ReadKey(); continue;
            }

            bool useProject = auth == "P";
            string modo = useProject ? "Project URL" : "API Key";

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== SAMPLES � Modalita': {modo} ===\n");
                MostraMenu(useProject);
                Console.Write("\n> ");
                var scelta = Console.ReadLine()?.Trim();
                if (scelta == "0") break;
                Console.Clear();
                try { await EseguiSample(scelta, useProject); }
                catch (Exception ex) { Console.WriteLine($"\nERRORE: {ex.Message}"); }
                Console.WriteLine("\nPremi un tasto per tornare al menu...");
                Console.ReadKey();
            }
        }
    }

    static void MostraMenu(bool useProject)
    {
        var visibili = MenuItems
            .Where(x => useProject || !x.OnlyProject)
            .ToList();

        string? cat = null;
        int i = 0;

        foreach (var item in visibili)
        {
            i++;
            if (item.Category != cat)
            {
                if (cat != null) Console.WriteLine();
                cat = item.Category;
                Console.WriteLine($"--- {cat} {"-".PadRight(40 - cat.Length, '-')}");
            }

            string label = item.OnlyProject && !useProject
                ? $"{item.Label} (solo Project)"
                : item.Label;

            Console.WriteLine($"{i,2}. {label}");
        }

        Console.WriteLine("\n---------------------------------------");
        Console.WriteLine(" 0. Cambia autenticazione");
    }
    static async Task EseguiSample(string scelta, bool useProject)
    {
        var visibili = MenuItems.Where(x => useProject || !x.OnlyProject).ToList();
        if (!int.TryParse(scelta, out int idx) || idx < 1 || idx > visibili.Count)
        { Console.WriteLine("Scelta non valida."); return; }

        switch (visibili[idx - 1].Feature)
        {
            // ── LLM e Agenti ──
            case FeatureType.ChiamataBase:
                { var s = useProject ? RunModelSample.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelSample.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); await s.RunWithOptions(Variabili.GPT_5_Mini); break; }
            case FeatureType.InputMultimodale:
                { var s = useProject ? RunModelSampleMixedInput.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelSampleMixedInput.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini, "assets\\inferno_incipit.png"); break; }
            case FeatureType.OutputStrutturato:
                { var s = useProject ? RunModelSampleStructuredOutput.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelSampleStructuredOutput.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.Streaming:
                { var s = useProject ? RunModelSampleStreaming.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelSampleStreaming.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.FunctionCalling:
                { var s = useProject ? RunModelWithCustomTools.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelWithCustomTools.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.WebSearch:
                { var s = useProject ? RunModelWithWebSearchTool.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelWithWebSearchTool.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.CodeInterpreter:
                { var s = useProject ? RunModelWithCodeInterpreterTool.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelWithCodeInterpreterTool.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.FileSearch:
                { var s = useProject ? RunModelWithFileSearchTool.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelWithFileSearchTool.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.McpServer:
                { var s = useProject ? RunModelWithMcpServerTool.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : RunModelWithMcpServerTool.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.Run(Variabili.GPT_5_Mini); break; }
            case FeatureType.ConversazioniStateful:
                await new RunModelUsingConversationsProtocol(Variabili.ProjectUrl, Variabili.TenantId).Run(Variabili.GPT_5_Mini); break;
            case FeatureType.AgentTemporaneo:
                await new RunFoundryAgent(Variabili.ProjectUrl, Variabili.TenantId).Run(Variabili.GPT_5_Mini); break;

            // ── Content Safety ──
            case FeatureType.ContentSafetyText: await new ContentSafety.TextAnalysisSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyTextBlocklist: await new ContentSafety.TextAnalysisWithBlocklistSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyImage: await new ContentSafety.ImageAnalysisSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyBlocklist: await new ContentSafety.BlocklistManagementSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyPromptShield: { var s = new ContentSafety.PromptShieldSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey); await s.RunForText(); Console.WriteLine(); await s.RunForDocument(); break; }
            case FeatureType.ContentSafetyGroundedness: { var s = new ContentSafety.GroundednessAnalysisSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey); await s.RunQNA(); Console.WriteLine(); await s.RunSummarizationCheck(); break; }
            case FeatureType.ContentSafetyProtectedText: await new ContentSafety.ProtectedMaterialTextSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyProtectedCode: await new ContentSafety.ProtectedMaterialCodeSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyMultimodal: await new ContentSafety.MultimodalAnalysisSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyCustomRapid: await new ContentSafety.CustomCategoriesRapidSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyCustomStandard: await new ContentSafety.CustomCategoriesStandardSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;
            case FeatureType.ContentSafetyTaskAdherence: await new ContentSafety.TaskAdherenceSample(Variabili.ContentSafetyUrl, Variabili.ContentSafetyKey).Run(); break;

            // ── Content Understanding ──
            case FeatureType.CuOcrRead:
                {
                    var s = new OcrPrebuiltRead(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.ExtractText("assets\\invoice.pdf");
                    break;
                }
            case FeatureType.CuLayout:
                {
                    var s = new PrebuiltLayout(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.AnalyzeLayout("assets\\invoice.pdf");
                    break;
                }
           
            case FeatureType.CuDocumentSearch:
                {
                    var s = new DocumentSearch(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.AnalyzeForRag("assets\\invoice.pdf");
                    break;
                }
            case FeatureType.CuImageSearch:
                {
                    var s = new ImageSearch(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.AnalyzeImage("assets\\inferno_incipit.png");
                    break;
                }
            case FeatureType.CuAudioAnalysis:
                {
                    var s = new AudioAnalysis(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.AnalyzeAudio("assets\\iliad_iGcDieRl.mp3");
                    break;
                }
            case FeatureType.CuVideoAnalysis:
                {
                    var s = new VideoAnalysis(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.AnalyzeVideo("assets\\sample_video.mp4");
                    break;
                }
            case FeatureType.CuCustomAnalyzer:
                {
                    var s = new CustomAnalyzer(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.CreateAndAnalyze("assets\\cf_mario_rossi.jpg",Variabili.DocumentIntelligenceCustomAnalyzer);
                    break;
                }

            // ── Speech ──
            case FeatureType.FastTranscription: { var s = new FastTranscriptionSample(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY); Console.WriteLine(await s.DoTranscriptionRest("assets\\iliad_iGcDieRl.mp3")); break; }
            case FeatureType.SpeechToText: await new SpeechFileStreamSample(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY).RecognizeFromFileAsync("assets\\iliad_iGcDieRl.wav"); break;
            case FeatureType.SpeechToTextLLM: { var s = useProject ? SpeechAnalysisUsingLLMSample.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : SpeechAnalysisUsingLLMSample.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); Console.WriteLine(await s.AnalyzeAudioWithModelAsync(Variabili.AudioAnalysis, "assets\\iliad_iGcDieRl.mp3")); break; }
            case FeatureType.TextToSpeech: await new TextToSpeechSample(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY).SpeakDialogWithSsmlAsync(); break;
            case FeatureType.SpeechTranslation: { var s = new SpeechTranslationSample(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.TranslateFromMicrophoneAsync("it-IT", "en", "fr", "de"); break; }

            // ── Vision ──
            case FeatureType.ImageAnalysis: { var s = new ClassicalVisionAnalysisSample(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY); Console.WriteLine(await s.AnalyzeImage("assets\\inferno_incipit.png")); break; }
            case FeatureType.GenerazioneImmagini: { var s = useProject ? LLMImageAnalysisSample.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : LLMImageAnalysisSample.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.TextToImage(Variabili.ImageGenerationModel); break; }
            case FeatureType.GenerazioneVideo: { var s = useProject ? LLMVideoAnalysisSample.FromUserCredentials(Variabili.ProjectUrl, Variabili.TenantId) : LLMVideoAnalysisSample.FromApiKey(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY); await s.AnalyzeVideo(Variabili.GPT_5_Mini, "assets\\iliad_iGcDieRl.mp3", "Descrivi il contenuto."); break; }

            // ── NLP e Traduzione ──
            case FeatureType.TextAnalysis:
                {
                    var s = new TextAnalysisSample(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    string sampleText = "Microsoft Azure è una FANTASTICA piattaforma di cloud computing gestita da Microsoft. Il responsabile dello sviluppo, Mario Rossi, ha confermato che il nuovo data center verrà inaugurato a Milano il prossimo 15 ottobre 2026. Per ulteriori informazioni, contattare il supporto all'indirizzo mario.rossi@example.com o chiamare il numero +39 021234567.";
                    await s.DetectLanguage(sampleText);
                    await s.SentimentAnalysis(sampleText);
                    await s.AbstractiveSummarization(sampleText);
                    await s.ExtractiveSummarization(sampleText);
                    await s.ExtractKeyPhrases(sampleText);
                    await s.ExtractPiiEntities(sampleText);
                    await s.RecognizeEntities(sampleText);
                    await s.RecognizeLinkedEntities(sampleText);
                    break;
                }
            case FeatureType.TraduzioneTesti:
                {
                    var s = new TranslateTextSample(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.TranslateString("Ciao mondo!", "en-US");
                    await s.LookupDictionaryRestAsync("fly", "en", "it");
                    await s.TransliterateString();
                    break;
                }
            case FeatureType.TraduzioneDocumenti:
                {
                    var s = new TranslateDocumentSample(Variabili.CognitiveServiceUrl, Variabili.CognitiveServiceAPI_KEY);
                    await s.TranslateDocumentAsync("assets\\english document.docx", "c:\\temp\\english document translated.docx", "en");
                    break;
                }

            // ── Altro ──
            case FeatureType.Embedding: await new RunModelSampleEmbedding(Variabili.FoundryResourceUrl, Variabili.CognitiveServiceAPI_KEY).Run(Variabili.TextEmbeddingModel_Small); break;
            case FeatureType.StimaToken: new PredictOpenAIInputTokensSample().CalcByText(); break;
            case FeatureType.OpenTelemetry: new OpenTelemetrySample().Run(Variabili.ApplicationInsightsConnectionString); break;
           
            case FeatureType.VideoIndexer:
                {
                    var s = new VideoIndexerSample(Variabili.VideoIndexerToken, Variabili.VideoIndexerAccountId, Variabili.VideoIndexerLocation);
                    Console.WriteLine(await s.AnalyzeVideoAsync("assets\\sample_video.mp4"));
                    break;
                }

            default: Console.WriteLine("Feature non implementata."); break;
        }
    }
}
