# Azure AI Foundry — Sample C# in italiano 🇮🇹

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)


Raccolta di **sample didattici in C#** per integrare un'applicazione con **Azure AI Foundry**. Ogni file mostra **un solo concetto**, con commenti in italiano, zero boilerplate e il minimo sforzo cognitivo per capire come funziona l'API.

> In inglese esistono migliaia di repository simili. In italiano, questo è l'unico.

## ⚡ Primi passi (5 minuti)

1. **Clona** la repo e copia `appsettings.json` → `appsettings.Development.json`
2. Nel file `.Development.json`, compila **almeno** queste 3 chiavi:
   - `AzureAI:FoundryResourceName` — nome della tua risorsa Foundry
   - `AzureAI:CognitiveServiceAPI_KEY` — chiave Cognitive Services
   - `Models:GPT5Mini` — nome del deployment GPT
3. Esegui `dotnet run`, scegli `[A] API Key`, poi il sample **1** *(Chiamata base)*
4. 🎉 In 2 minuti hai fatto la prima chiamata GPT-5 da C#. Da qui esplora gli altri sample.

## 📋 Requisiti

### Obbligatori

| Requisito | Dettaglio |
|---|---|
| **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** | Runtime e compilatore C# |
| **Sottoscrizione Azure** | Account Azure attivo con permessi di creazione risorse |
| **Azure AI Foundry** | Risorsa Foundry con un progetto creato e i seguenti modelli **deployati**: |
| | • Modello **chat** (es. `gpt-5-mini`, `gpt-5.2`) |
| | • Modello **embedding** (es. `text-embedding-3-small`) |
| | • Modello **generazione immagini** (es. `gptImageI5`) |
| | • Modello **generazione video** (es. `sora2`) |
| | • Modello **audio** (es. `gpt-audio-mini`, `gpt-4o-transcribe`) |

### Opzionali (per sample specifici)

| Servizio Azure | Sample interessati |
|---|---|
| **Azure AI Content Safety** | `ContentSafety/` (tutti i 12 sample) |
| **Azure AI Speech** | `speech/` (STT, TTS, traduzione vocale) |
| **Azure AI Translator** | `textAnalysis/` (traduzione testi e documenti) |
| **Azure AI Video Indexer** | `vision/02_VideoIndexerSample` |
| **Application Insights** | `otherTopics/OpenTelemetrySample` |

## 📁 Struttura

```
├── 01_RunModelSample.cs                  # Chiamata base + contesto + parametri
├── 02_RunModelSampleMixedInput.cs        # Input multimodale (testo + immagine)
├── 03_RunModelSampleStructuredOutput.cs  # JSON strutturato + refusal + retry
├── 04_RunModelSampleStreaming.cs         # Streaming token-per-token
├── 05_RunModelWithCustomTools.cs         # Function calling con tool personalizzato
├── 06_RunModelWithWebSearchTool.cs       # Tool Web Search
├── 07_RunModelWithCodeInterpreterTool.cs # Tool Code Interpreter
├── 08_RunModelWithFileSearchTool.cs      # Tool File Search + Vector Store
├── 09_RunModelWithMcpServerTool.cs       # Tool MCP Server
├── 10_RunModelUsingConversationsProtocol.cs # Conversazioni stateful
├── 20_RunFoundryAgent.cs                # Agent temporaneo preconfigurato
│
├── ContentSafety/                       # 12 API REST per moderazione contenuti
├── contentUnderstanding/                # 7 sample Document & Content Intelligence
├── speech/                              # 5 sample STT, TTS, traduzione vocale
├── vision/                              # 4 sample immagini, video, video indexer
├── textAnalysis/                        # 3 sample NLP e traduzione
├── otherTopics/                         # Embedding, token, telemetria
│
├── assets/                              # File di test (immagini, audio, video, PDF)
├── Program.cs                           # Entry point con menu interattivo
├── Variabili.cs                         # Configurazione da appsettings.json
└── appsettings.json                     # NON committare! (usare .Development.json)
```

## ⚙️ Configurazione (`appsettings.json`)

```json
{
  "AzureAI": {
    "TenantId": "your-tenant-id",
    "FoundryResourceName": "your-foundry-resource-name",
    "FoundryProjectName": "your-project-name-in-foundry",
    "CognitiveServiceAPI_KEY": "API-KEY-cognitive-services",
    "ContentSafetyUrl": "https://your-content-safety.cognitiveservices.azure.com",
    "ContentSafetyKey": "CONTENT-SAFETY-KEY",
    "ApplicationInsightsConnectionString": "InstrumentationKey=...;IngestionEndpoint=...",
    "VideoIndexerAccountId": "account-id-video-indexer",
    "VideoIndexerToken": "token-generato-da-management-api",    
    "VideoIndexerRegion": "eastus2"
  },
  "Models": {
    "GPT5Mini": "gpt-5-mini",
    "GPT5Full": "gpt-5.2-945002",
    "EmbeddingModelSmall": "text-embedding-3-small",
    "EmbeddingModelLarge": "text-embedding-3-large-703779",
    "ImageGenerationModel": "gptImageI5",
    "AudioAnalysis": "gpt-audio-mini",
    "VideoGenerationModel": "sora2",
    "DocumentIntelligenceCustomAnalyzer": "cf_fields_extractor"
  }
}
```

| Chiave | Dove trovarla |
|---|---|
| `TenantId` | Entra ID → Overview → Tenant ID |
| `FoundryResourceName` | Nome della risorsa Azure AI Foundry |
| `FoundryProjectName` | Nome del progetto dentro Foundry |
| `CognitiveServiceAPI_KEY` | Risorsa Cognitive Services → Keys |
| `ContentSafetyUrl` / `ContentSafetyKey` | Risorsa Content Safety → Overview / Keys |
| `ApplicationInsightsConnectionString` | Application Insights → Overview |
| `VideoIndexerAccountId` | Video Indexer → Overview |
| `VideoIndexerToken` | Video Indexer → Management → Management API (genera token) |
| `VideoIndexerRegion`  | Regione della risorsa Video Indexer |
| `Models.*` | Nomi dei deployment dentro Azure AI Foundry |

## 🚀 Come eseguire

Avvia il progetto (`dotnet run`). Il menu interattivo guida alla scelta del sample:

```
=== AZURE AI FOUNDRY — SAMPLES C# ===

Autenticazione:
  [P] Project URL  (InteractiveBrowserCredential)
  [A] API Key      (OpenAI / OpenRouter / ...)
  [E] Esci

> P

=== SAMPLES — Modalità: Project URL ===

─── LLM e Agenti ─────────────────────
 1. Chiamata base + contesto + parametri
 2. Input multimodale (testo + immagine)
...
───────────────────────────────────────
 0. Cambia autenticazione

>
```

**Modalità Project URL**: apre il browser per autenticarsi via Entra ID. I sample 10 (Conversazioni) e 11 (Agent) funzionano solo in questa modalità.

**Modalità API Key**: usa `FoundryResourceUrl` + `CognitiveServiceAPI_KEY` da `appsettings.json`. Compatibile con qualsiasi endpoint OpenAI-compatibile (Foundry, OpenAI, OpenRouter...).

## 📋 Tabella completa dei sample

### Blocco 1 — Inferenza LLM (01–10)

| # | Classe | Metodo | Cosa mostra |
|---|---|---|---|
| 01 | `RunModelSample` | `Run()` | Chiamata testuale base, gestione contesto (2 metodi), token |
| 01 | `RunModelSample` | `RunWithOptions()` | Temperature, TopP, MaxOutputTokenCount, Instructions |
| 02 | `RunModelSampleMixedInput` | `Run()` | Input misto: testo + immagine in base64 |
| 03 | `RunModelSampleStructuredOutput` | `Run()` | JSON Schema + refusal detection + validazione + retry |
| 04 | `RunModelSampleStreaming` | `Run()` | Streaming token-per-token via ChatClient |
| 05 | `RunModelWithCustomTools` | `Run()` | Function calling custom + ciclo tool call + ToolChoice |
| 06 | `RunModelWithWebSearchTool` | `Run()` | Web Search tool + geolocalizzazione + polling |
| 07 | `RunModelWithCodeInterpreterTool` | `Run()` | Code Interpreter + download file generato |
| 08 | `RunModelWithFileSearchTool` | `Run()` | File Search + Vector Store CRUD + expiration |
| 09 | `RunModelWithMcpServerTool` | `Run()` | MCP Server tool + approval policy |
| 10 | `RunModelUsingConversationsProtocol` | `Run()` | Conversation stateful + recupero messaggi |
| 20 | `RunFoundryAgent` | `Run()` | Agent temporaneo: create → usa → delete |

### Blocco 2 — Content Safety (12 API REST)

| # | Classe | Metodo | Cosa mostra |
|---|---|---|---|
| 01 | `TextAnalysisSample` | `Run()` | Testo: Hate, Sexual, Violence, SelfHarm (8 livelli) |
| 02 | `TextAnalysisWithBlocklistSample` | `Run()` | Testo + blocklist personalizzata |
| 03 | `ImageAnalysisSample` | `Run()` | Analisi immagine base64 |
| 04 | `BlocklistManagementSample` | `Run()` | CRUD blocklist (6 operazioni) |
| 05 | `PromptShieldSample` | `RunForText()` / `RunForDocument()` | Jailbreak + indirect prompt injection |
| 06 | `GroundednessAnalysisSample` | `RunQNA()` / `RunSummarizationCheck()` | Groundedness QnA e Summarization |
| 07 | `ProtectedMaterialTextSample` | `Run()` | Copyright testo |
| 08 | `ProtectedMaterialCodeSample` | `Run()` | Copyright codice (licenze open source) |
| 09 | `MultimodalAnalysisSample` | `Run()` | Immagine + testo + OCR |
| 10 | `CustomCategoriesRapidSample` | `Run()` | Categorie custom rapide (incident) |
| 11 | `CustomCategoriesStandardSample` | `Run()` | Categorie custom standard (training) |
| 12 | `TaskAdherenceSample` | `Run()` | Verifica agenti AI |

### Blocco 3 — Speech

| Classe | Metodo | Cosa mostra |
|---|---|---|
| `FastTranscriptionSample` | `DoTranscriptionRest()` | STT batch: diarization, enhanced mode, phrase list |
| `SpeechFileStreamSample` | `RecognizeFromFileAsync()` / `RecognizeFromMicrophoneAsync()` | STT real-time (SDK) |
| `SpeechAnalysisUsingLLMSample` | `GetTranscription()` | Trascrizione + traduzione + sentiment via LLM |
| `TextToSpeechSample` | `SpeakText()` / `SpeakSsml()` / `SpeakDialog()` | TTS base, SSML, dialogo multi-voce |
| `SpeechTranslationSample` | `TranslateSpeechAsync()` | Traduzione vocale in tempo reale |

### Blocco 4 — Vision

| Classe | Metodo | Cosa mostra |
|---|---|---|
| `ClassicalVisionAnalysisSample` | `AnalyzeImage()` | Caption, dense captions, tags, oggetti, OCR, smart crops |
| `VideoIndexerSample` | `AnalyzeVideoAsync()` | Analisi video con Azure Video Indexer |
| `LLMImageAnalysisSample` | `TextToImage()` / `ImageToImage()` | Generazione e modifica immagini |
| `LLMVideoAnalysisSample` | `TextToVideo()` / `RemixVideo()` / `AnalyzeVideo()` / `RunFullDemo()` | Sora: generazione, remix, analisi, CRUD |

### Blocco 5 — NLP e Traduzione

| Classe | Metodo | Cosa mostra |
|---|---|---|
| `TextAnalysisSample` | `DetectLanguage()` / `SentimentAnalysis()` / `ExtractKeyPhrases()` / `NomiCitta()` / `RecognizePii()` / `ExtractiveSummarization()` / `AbstractiveSummarization()` | Lingua, sentiment, NER, PII, summarization |
| `TranslateTextSample` | `TranslateString()` / `LookupDictionaryRestAsync()` / `TransliterateString()` | Traduzione, dizionario, traslitterazione |
| `TranslateDocumentSample` | `TranslateDocumentAsync()` | Traduzione documenti (.docx) |

### Blocco 6 — Altro

| Classe | Cosa mostra |
|---|---|
| `RunModelSampleEmbedding` | Embedding + similarità coseno |
| `PredictOpenAIInputTokensSample` | Stima token con Tiktoken |
| `OpenTelemetrySample` | Tracciamento chiamate LLM con Application Insights |

### Blocco 7 — Content Understanding

| # | Classe | Metodo | Cosa mostra |
|---|---|---|---|
| 01 | `OcrPrebuiltRead` | `Run()` | OCR su documento (PDF, immagini) |
| 02 | `PrebuiltLayout` | `Run()` | Layout documenti: tabelle, paragrafi, titoli |
| 03 | `DocumentSearch` | `Run()` | Ricerca semantica su documenti |
| 04 | `ImageSearch` | `Run()` | Ricerca semantica su immagini |
| 05 | `AudioAnalysis` | `Run()` | Analisi contenuti audio (trascrizione + summarization) |
| 06 | `VideoAnalysis` | `Run()` | Analisi contenuti video |
| 07 | `CustomAnalyzer` | `Run()` | Analyzer custom (es. estrazione campi da moduli) |

## 📦 Pacchetti NuGet principali

| Pacchetto | Uso |
|---|---|
| `Azure.AI.OpenAI` | Inferenza, embedding, immagini, video |
| `Azure.AI.Projects` | Agenti, conversazioni, tool Foundry |
| `Azure.AI.ContentUnderstanding` | Document & Content Intelligence |
| `Azure.AI.Language.Text` | NLP (sentiment, NER, summarization) |
| `Azure.AI.Translation.Text` / `.Document` | Traduzione testi e documenti |
| `Azure.AI.Vision.ImageAnalysis` | Analisi immagini classica |
| `Azure.AI.Speech.Transcription` | Trascrizione batch (Fast Transcription) |
| `Microsoft.CognitiveServices.Speech` | STT real-time e TTS |
| `Microsoft.ML.Tokenizers` + Data packages | Stima token (Tiktoken: o200k, cl100k, p50k, r50k) |
| `Azure.Monitor.OpenTelemetry.Exporter` | Application Insights / tracing |
| `Azure.Identity` | Autenticazione Entra ID |
| `Microsoft.Extensions.Configuration.Json` | Caricamento `appsettings.json` |
| `Microsoft.Bcl.Memory` | Supporto memory/memory pool |

## 📝 Note

- **Design**: i sample sono volutamente auto-contenuti e ripetitivi — niente classi base astratte, niente helper condivisi. Ogni file `.cs` può essere copiato e incollato in un altro progetto senza dipendenze incrociate. L'obiettivo è **didattico**, non di eleganza architetturale.
- **Content Safety**: i sample usano API REST (non l'SDK) perché l'SDK `Azure.AI.ContentSafety` non copre tutte le funzionalità (es. groundedness). Vedi `ContentSafety/readme.txt`.