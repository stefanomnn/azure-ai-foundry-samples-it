using Microsoft.CognitiveServices.Speech;

namespace AzureAiSamples.speech
{
    /// <summary>
    /// SAMPLE DIDATTICO: Text-to-Speech (TTS) con Azure AI Speech.
    ///
    /// COSA IMPARERAI:
    ///   - Sintesi vocale base: testo → audio (altoparlante o file .wav)
    ///   - SSML (Speech Synthesis Markup Language): il "linguaggio XML strano"
    ///     per controllare voce, prosodia, pause, enfasi e molto altro
    ///   - Diverse voci neurali italiane (es. Isabella, Diego, Elsa)
    ///   - Regolazione di velocità (rate) e tono (pitch) via SSML
    ///
    /// Prerequisiti: una risorsa Azure AI Speech (Cognitive Services).
    /// </summary>
    public class TextToSpeechSample
    {
        private readonly string _endpointUrl;
        private readonly string _apiKey;

        public TextToSpeechSample(string endpointUrl, string apiKey)
        {
            _endpointUrl = endpointUrl;
            _apiKey = apiKey;
        }

        // ==============================================================
        //  1. SINTESI BASE: testo semplice → altoparlante
        // ==============================================================

        /// <summary>
        /// Forma più semplice di TTS: passi una stringa e la fa parlare
        /// dagli altoparlanti del PC. Nessun SSML, solo testo puro.
        /// </summary>
        public async Task SpeakTextAsync(string text, string? voiceName = null)
        {
            var config = SpeechConfig.FromEndpoint(new Uri(_endpointUrl), _apiKey);
            if (!string.IsNullOrEmpty(voiceName))
                config.SpeechSynthesisVoiceName = voiceName;

            using var synthesizer = new SpeechSynthesizer(config);

            Console.WriteLine($"🔊 Sintesi in corso: \"{Truncate(text, 60)}...\"");
            Console.WriteLine($"   Voce: {voiceName ?? "predefinita"}");

            using var result = await synthesizer.SpeakTextAsync(text);
            PrintResult(result);
        }

        // ==============================================================
        //  2. SINTESI BASE: testo semplice → file .wav
        // ==============================================================

        /// <summary>
        /// Sintesi vocale su file WAV (utile per salvare audiolibri,
        /// notifiche, messaggi automatici).
        /// </summary>
        public async Task SynthesizeToFileAsync(
            string text, string outputFilePath, string? voiceName = null)
        {
            var config = SpeechConfig.FromEndpoint(new Uri(_endpointUrl), _apiKey);
            if (!string.IsNullOrEmpty(voiceName))
                config.SpeechSynthesisVoiceName = voiceName;

            using var synthesizer = new SpeechSynthesizer(config, null);

            Console.WriteLine($"💾 Sintesi su file: \"{Truncate(text, 60)}...\"");
            Console.WriteLine($"   Output: {outputFilePath}");

            using var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                await File.WriteAllBytesAsync(outputFilePath, result.AudioData);
                Console.WriteLine($"   ✅ File salvato ({result.AudioData.Length} bytes)");
            }
            else
            {
                PrintError(result);
            }
        }

        // ==============================================================
        //  3. SSML: IL "LINGUAGGIO XML STRANO" ← IL CUORE DEL SAMPLE
        // ==============================================================
        //
        //  SSML = Speech Synthesis Markup Language.
        //  È un dialetto XML che permette di controllare OGNI aspetto
        //  della sintesi vocale: voce, velocità, tono, pause, enfasi,
        //  pronuncia fonetica, cambio lingua al volo, e molto altro.
        //
        //  STRUTTURA BASE di un documento SSML:
        //
        //  <speak version="1.0"
        //         xmlns="http://www.w3.org/2001/10/synthesis"
        //         xml:lang="it-IT">
        //      <voice name="it-IT-IsabellaNeural">
        //          ... qui dentro metti il testo e i tag di controllo ...
        //      </voice>
        //  </speak>
        //
        //  TAG PRINCIPALI:
        //    <voice name="...">             → sceglie la voce neurale
        //    <prosody rate="..." pitch="..."> → velocità e tono
        //    <break time="..."/>            → pausa (ms o secondi)
        //    <emphasis level="...">         → enfasi su una parola/frase
        //    <say-as interpret-as="...">    → interpretare numeri, date
        //    <lang xml:lang="...">          → cambio lingua al volo
        // ==============================================================

        /// <summary>
        /// Dimostrazione completa di SSML: voce, velocità, tono, pause,
        /// enfasi, pronuncia di numeri e cambio lingua al volo.
        /// Sintetizza su altoparlante.
        /// </summary>
        public async Task SpeakWithSsmlAsync()
        {
            // COSTRUIAMO IL NOSTRO SSML "STRANO" PASSO PER PASSO
            // ---------------------------------------------------
            // Ogni tag ha uno scopo preciso. Leggi i commenti.

            string ssml = $"""
                <speak version="1.0"
                       xmlns="http://www.w3.org/2001/10/synthesis"
                       xml:lang="it-IT">

                    <!-- voice: scegliamo Isabella, una voce neurale italiana -->
                    <voice name="it-IT-IsabellaNeural">

                        <!-- FRASE NORMALE -->
                        Ciao! Questa è una dimostrazione di sintesi vocale con SSML.

                        <!-- break: pausa di 800 millisecondi (quasi 1 secondo) -->
                        <break time="800ms"/>

                        <!-- PROSODY: controllo velocità (rate) e tono (pitch) -->

                        <!-- rate="slow": parla PIÙ LENTAMENTE del normale -->
                        <prosody rate="slow">
                            Ora sto parlando lentamente, per dare enfasi a questa frase.
                        </prosody>

                        <break time="500ms"/>

                        <!-- rate="fast": parla PIÙ VELOCEMENTE (es. disclaimer) -->
                        <prosody rate="fast">
                            Mentre ora sto parlando molto velocemente,
                            come si fa per le note legali alla fine di una pubblicità.
                        </prosody>

                        <break time="600ms"/>

                        <!-- pitch="high": tono ACUTO (voce più "allegra") -->
                        <prosody pitch="high">
                            Questo è un tono acuto! Sembro più allegra.
                        </prosody>

                        <break time="400ms"/>

                        <!-- pitch="low": tono GRAVE (voce più "seria") -->
                        <prosody pitch="low">
                            E questo è un tono grave. Sembro più seria e autorevole.
                        </prosody>

                        <break time="600ms"/>

                        <!-- EMPHASIS: enfasi su una parola -->
                        <!-- level: "strong", "moderate", "reduced" -->
                        Ora ti dico una cosa
                        <emphasis level="strong">davvero molto importante</emphasis>
                        da ricordare.

                        <break time="700ms"/>

                        <!-- SAY-AS: come interpretare numeri/caratteri -->

                        <!-- interpret-as="cardinal": "1234" → "milleduecentotrentaquattro" -->
                        Il numero <say-as interpret-as="cardinal">1234</say-as>
                        è letto come parola.

                        <break time="400ms"/>

                        <!-- interpret-as="digits": "1234" → "uno due tre quattro" -->
                        Il pin <say-as interpret-as="digits">1234</say-as>
                        è letto cifra per cifra.

                        <break time="400ms"/>

                        <!-- interpret-as="date": legge come data in italiano -->
                        Oggi è il
                        <say-as interpret-as="date" format="dm">14-09-2026</say-as>.

                        <break time="800ms"/>

                        <!-- CAMBIO LINGUA AL VOLO (dentro la stessa voce!) -->
                        E ora una frase in inglese:
                        <lang xml:lang="en-US">
                            Hello! This is a demonstration of
                            the Speech Synthesis Markup Language.
                            It is very powerful and flexible.
                        </lang>

                        <break time="500ms"/>

                        <!-- Torniamo in italiano per concludere -->
                        Tutto questo è possibile grazie al
                        <emphasis level="moderate">markup SSML</emphasis>,
                        un dialetto XML che dà il controllo totale
                        sulla sintesi vocale.

                    </voice>
                </speak>
                """;

            var config = SpeechConfig.FromEndpoint(new Uri(_endpointUrl), _apiKey);
            using var synthesizer = new SpeechSynthesizer(config);

            Console.WriteLine("🎭 SSML Demo — sintesi con controllo avanzato");
            Console.WriteLine("   Ascolta le differenze di velocità, tono, enfasi e lingua!\n");

            // SpeakSsmlAsync: invia SSML (NON testo puro) e riproduce
            using var result = await synthesizer.SpeakSsmlAsync(ssml);
            PrintResult(result);
        }

        // ==============================================================
        //  3b. SSML: esempio "minimalista" facile da copiare
        // ==============================================================

        /// <summary>
        /// Versione MINIMA di SSML per chi vuole partire con poco.
        /// Solo 3 tag: voice, break, prosody.
        /// </summary>
        public async Task SpeakWithSsmlMinimalAsync(string text)
        {
            string ssml = $"""
                <speak version="1.0"
                       xmlns="http://www.w3.org/2001/10/synthesis"
                       xml:lang="it-IT">
                    <voice name="it-IT-IsabellaNeural">
                        <prosody rate="medium" pitch="medium">
                            {text}
                        </prosody>
                    </voice>
                </speak>
                """;

            var config = SpeechConfig.FromEndpoint(new Uri(_endpointUrl), _apiKey);
            using var synthesizer = new SpeechSynthesizer(config);

            Console.WriteLine($"🔊 SSML minimale: \"{Truncate(text, 60)}...\"");
            using var result = await synthesizer.SpeakSsmlAsync(ssml);
            PrintResult(result);
        }

        // ==============================================================
        //  4. SSML AVANZATO: più voci nella stessa sintesi (dialogo)
        // ==============================================================

        /// <summary>
        /// Esempio avanzato: usa DUE voci diverse nello stesso SSML
        /// per simulare un dialogo (es. assistente virtuale + utente).
        /// Ogni blocco &lt;voice&gt; può avere un name diverso.
        /// </summary>
        public async Task SpeakDialogWithSsmlAsync()
        {
            // Nota: Azure supporta il cambio voce all'interno dello stesso SSML.
            string ssml = """
                <speak version="1.0"
                       xmlns="http://www.w3.org/2001/10/synthesis"
                       xml:lang="it-IT">

                    <!-- Voce maschile: Diego -->
                    <voice name="it-IT-DiegoNeural">
                        Buongiorno! Sono Diego, il tuo assistente virtuale.
                        <break time="300ms"/>
                        Oggi ti aiuterò con il tuo promemoria.
                        <break time="500ms"/>
                    </voice>

                  

                    <!-- Voce femminile: Isabella -->
                    <voice name="it-IT-IsabellaNeural">
                        Ciao Diego! Grazie per l'aiuto.
                        <break time="200ms"/>
                        <emphasis level="strong">Ricordami</emphasis>
                        la riunione delle 15:00.
                        <break time="500ms"/>
                    </voice>

                    

                    <!-- Diego risponde -->
                    <voice name="it-IT-DiegoNeural">
                        Certo! Ho impostato un promemoria per le
                        <say-as interpret-as="time" format="hms12">
                            15:00:00
                        </say-as>.
                        <break time="300ms"/>
                        Buon lavoro!
                    </voice>

                </speak>
                """;

            var config = SpeechConfig.FromEndpoint(new Uri(_endpointUrl), _apiKey);
            using var synthesizer = new SpeechSynthesizer(config);

            Console.WriteLine("🎭 Dialogo con due voci neurali (Diego ↔ Isabella)\n");
            using var result = await synthesizer.SpeakSsmlAsync(ssml);
            PrintResult(result);
        }

        // ==============================================================
        //  UTILITY
        // ==============================================================

        private static void PrintResult(SpeechSynthesisResult result)
        {
            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Console.WriteLine($"   ✅ Sintesi completata ({result.AudioData.Length} bytes audio)");
                    break;
                case ResultReason.Canceled:
                    PrintError(result);
                    break;
            }
        }

        private static void PrintError(SpeechSynthesisResult result)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            Console.WriteLine($"   ❌ Sintesi annullata. Motivo: {cancellation.Reason}");
            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"   Dettaglio errore: {cancellation.ErrorDetails}");
                Console.WriteLine("   Suggerimenti:");
                Console.WriteLine("     - Verifica che endpoint e API key siano corretti");
                Console.WriteLine("     - Verifica che la risorsa Speech sia attiva");
                Console.WriteLine("     - Se usi una voce neurale, verifica che sia disponibile nella tua region");
            }
        }

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}