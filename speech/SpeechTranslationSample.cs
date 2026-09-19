using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace AzureAiSamples.speech;

/// <summary>
/// SAMPLE DIDATTICO: Speech Translation con Azure AI Speech.
/// 
/// COSA IMPARERAI:
///   - Tradurre speech in tempo reale: parli in italiano, ottieni testo in altre lingue
///   - SpeechTranslationConfig: lingua source + lingue target in unico flusso
///   - TranslationRecognizer: riconoscimento vocale con traduzione simultanea
///   - Due modalità: da microfono (real-time) e da file audio (batch)
/// 
/// AI-103 KEY POINT:
///   "Translate speech into other languages by using language models and Foundry Tools"
///   → SpeechTranslationConfig combina STT + traduzione in un'unica chiamata.
///     Non servono due passaggi separati: il servizio fa tutto insieme.
/// 
/// CONFRONTO:
///   - Speech → STT → Translator API: due chiamate, più latenza
///   - Speech → STT → LLM: più flessibile ma costoso
///   - SpeechTranslationConfig: UNA chiamata, integrato, bassa latenza
///     → Ottimale per call center, meeting, live caption
/// </summary>
public class SpeechTranslationSample
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public SpeechTranslationSample(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }
/// <summary>
    /// Traduzione speech da microfono in tempo reale.
    /// L'utente parla e vede simultaneamente originale + traduzioni.
    /// </summary>
    public async Task TranslateFromMicrophoneAsync(
        string sourceLanguage = "it-IT",
        params string[] targetLanguages)
    {
        if (targetLanguages.Length == 0)
            targetLanguages = new[] { "en" };

        Console.WriteLine("🎤 SPEECH TRANSLATION — Da Microfono in Tempo Reale");
        Console.WriteLine($"   Source: {sourceLanguage} → Target: {string.Join(", ", targetLanguages)}");
        Console.WriteLine("   Parla nel microfono... Premi INVIO per terminare.\n");

        var config = SpeechTranslationConfig.FromEndpoint(
            new Uri(_EndpointUrl), _ApiKey);
        config.SpeechRecognitionLanguage = sourceLanguage;
        foreach (var lang in targetLanguages)
            config.AddTargetLanguage(lang);

        using var audioInput = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new TranslationRecognizer(config, audioInput);

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatedSpeech)
            {
                Console.WriteLine($"\n📝 IT: \"{e.Result.Text}\"");
                foreach (var (lang, translation) in e.Result.Translations)
                    Console.WriteLine($"🌍 {lang}: \"{translation}\"");
                Console.WriteLine();
            }
        };

        recognizer.Recognizing += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatingSpeech)
                Console.Write($"\r[Live] {e.Result.Text}...");
        };

        recognizer.Canceled += (s, e) =>
        {
            if (e.Reason == CancellationReason.Error)
                Console.WriteLine($"\n❌ Errore: {e.ErrorCode} — {e.ErrorDetails}");
        };

        await recognizer.StartContinuousRecognitionAsync();
        Console.ReadLine();
        await recognizer.StopContinuousRecognitionAsync();
        Console.WriteLine("✅ Traduzione terminata.");
    }

    /// <summary>
    /// Traduzione speech da file audio (batch).
    /// Per podcast, meeting registrati, audiolibri.
    /// </summary>
    public async Task TranslateFromFileAsync(
        string audioFilePath,
        string sourceLanguage = "it-IT",
        params string[] targetLanguages)
    {
        if (targetLanguages.Length == 0)
            targetLanguages = new[] { "en" };

        Console.WriteLine("📁 SPEECH TRANSLATION — Da File Audio (Batch)");
        Console.WriteLine($"   File: {Path.GetFileName(audioFilePath)}");
        Console.WriteLine($"   Source: {sourceLanguage} → Target: {string.Join(", ", targetLanguages)}\n");

        var config = SpeechTranslationConfig.FromEndpoint(
            new Uri(_EndpointUrl), _ApiKey);
        config.SpeechRecognitionLanguage = sourceLanguage;
        foreach (var lang in targetLanguages)
            config.AddTargetLanguage(lang);

        using var audioInput = AudioConfig.FromWavFileInput(audioFilePath);
        using var recognizer = new TranslationRecognizer(config, audioInput);

        var allResults = new List<(string Original, Dictionary<string, string> Translations)>();

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatedSpeech)
                allResults.Add((e.Result.Text, new Dictionary<string, string>(e.Result.Translations)));
        };

        recognizer.Canceled += (s, e) =>
        {
            if (e.Reason == CancellationReason.Error)
                Console.WriteLine($"❌ Errore: {e.ErrorCode} — {e.ErrorDetails}");
        };

        await recognizer.StartContinuousRecognitionAsync();

        // Attesa stimata in base alla dimensione del file
        await Task.Delay(TimeSpan.FromSeconds(
            new FileInfo(audioFilePath).Length / 16000.0 * 2 + 5));
        await recognizer.StopContinuousRecognitionAsync();

        Console.WriteLine($"✅ Frasi tradotte: {allResults.Count}\n");
        for (int i = 0; i < allResults.Count; i++)
        {
            var (original, translations) = allResults[i];
            Console.WriteLine($"--- Frase {i + 1} ---");
            Console.WriteLine($"  📝 IT: \"{original}\"");
            foreach (var (lang, translation) in translations)
                Console.WriteLine($"  🌍 {lang}: \"{translation}\"");
            Console.WriteLine();
        }
    }
}