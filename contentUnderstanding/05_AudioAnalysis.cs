using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Analisi Audio con Content Understanding.
/// 
/// COSA IMPARERAI:
///   - prebuilt-audio: trascrizione audio con comprensione semantica
///   - Output: trascrizione + diarization speaker + riassunto + sentiment
///   - Pipeline multimodale: audio → testo strutturato con insight
/// 
/// AI-103 KEY POINT:
///   Content Understanding processa anche AUDIO. Non è solo OCR per documenti.
///   prebuilt-audio combina STT + comprensione semantica in unico flusso.
/// 
/// CONFRONTO con Azure Speech SDK:
///   - Speech SDK: trascrizione accurata, diarization, phrase list (deterministico)
///   - CU Audio: trascrizione + riassunto generativo + sentiment + topic
/// </summary>
internal class AudioAnalysis
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public AudioAnalysis(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> AnalyzeAudio(string audioFilePath)
    {
        const string analyzerId = "prebuilt-audio";

        Console.WriteLine("🎤 Content Understanding: Analisi Audio (prebuilt-audio)");
        Console.WriteLine($"   File: {Path.GetFileName(audioFilePath)}");
        Console.WriteLine("   Output: trascrizione + diarization + riassunto");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        var input = new AnalysisInput { Data = BinaryData.FromFile(audioFilePath) };
        var operation = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerId, new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine("=== ANALISI AUDIO (prebuilt-audio) ===");
        sb.AppendLine();

        foreach (var content in operation.Value.Contents)
        {
            sb.AppendLine($"📌 Contenuto audio");

            // Campi: trascrizione, speaker, riassunto, sentiment, topic
            if (content.Fields?.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- 🏷️  Campi estratti ---");
                foreach (var field in content.Fields)
                {
                    string value = field.Value.Value?.ToString() ?? "(vuoto)";
                    if (value.Length > 300) value = value[..300] + "...";
                    sb.AppendLine($"  • {field.Key}: \"{value}\"");
                    if (field.Value.Confidence.HasValue)
                        sb.AppendLine($"    confidence: {field.Value.Confidence.Value:P0}");
                }
            }

            // Markdown (trascrizione formattata)
            if (!string.IsNullOrWhiteSpace(content.Markdown))
            {
                sb.AppendLine();
                sb.AppendLine("--- 📝 Trascrizione ---");
                string preview = content.Markdown.Length > 500
                    ? content.Markdown[..500] + "..." : content.Markdown;
                sb.AppendLine(preview);
            }
            sb.AppendLine();
        }

        sb.AppendLine("💡 Casi d'uso: call center analytics, trascrizione riunioni, sottotitoli");
        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}