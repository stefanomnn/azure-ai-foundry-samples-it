using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace AzureAiSamples.speech {

    /// <summary>
    /// esempio di utilizzo della libreria Microsoft.CognitiveServices.Speech per trascrivere un file audio o l'audio del microfono in tempo reale.
    /// </summary>
    public class SpeechFileStreamSample
    {
        private readonly string _foundryUrl;
        private readonly string _apiKey;

        public SpeechFileStreamSample(string foundryUrl, string apiKey)
        {
            _foundryUrl = foundryUrl;
            _apiKey = apiKey;
        }

        public async Task RecognizeFromFileAsync(string audioFilePath)
        {
            var config = SpeechConfig.FromEndpoint(new Uri(_foundryUrl), _apiKey);
            config.SpeechRecognitionLanguage = "it-IT";

            // Configura la sorgente audio leggendo dal file (es. WAV)
            using var audioInput = AudioConfig.FromWavFileInput(audioFilePath);
            using var recognizer = new SpeechRecognizer(config, audioInput);

            // Evento scatenato mentre il sistema elabora i testi parziali
            recognizer.Recognizing += (s, e) =>
            {
                Console.WriteLine($"[In corso...] Parziale: {e.Result.Text}");
            };

            // Evento scatenato quando una frase è stata completata e riconosciuta
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"[COMPLETO] Testo: {e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine("[AVVISO] Nessun parlato riconosciuto.");
                }
            };

            // Avvia la trascrizione continua
            await recognizer.StartContinuousRecognitionAsync();

            Console.WriteLine("Trascrizione del file in corso... Premi un tasto per fermare.");
            Console.ReadKey();

            // Interrompi il processo
            await recognizer.StopContinuousRecognitionAsync();
        }

        public async Task RecognizeFromMicrophoneAsync()
        {
            var config = SpeechConfig.FromEndpoint(new Uri(_foundryUrl), _apiKey);

            config.SpeechRecognitionLanguage = "it-IT";

            // Configura la sorgente audio sul microfono predefinito del sistema
            using var audioInput = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(config, audioInput);

            // Gestione dei risultati parziali in tempo reale mentre l'utente parla
            recognizer.Recognizing += (s, e) =>
            {
                Console.Write($"\r[Live] {e.Result.Text}                  ");
            };

            // Gestione della frase definitiva riconosciuta
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"\n[Testo Finale] {e.Result.Text}");
                }
            };

            // Gestione di eventuali errori o chiusura della sessione
            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"\n[Annullato] Motivo: {e.Reason}");
                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Codice errore: {e.ErrorCode}, Dettagli: {e.ErrorDetails}");
                }
            };

            // Avvia l'ascolto continuo dal microfono
            await recognizer.StartContinuousRecognitionAsync();

            Console.WriteLine("Parla pure nel microfono... Premi INVIO per terminare.");
            Console.ReadLine();

            // Ferma l'ascolto
            await recognizer.StopContinuousRecognitionAsync();
        }
    }
}