using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Analisi Video con Content Understanding.
/// 
/// COSA IMPARERAI:
///   - prebuilt-videoSearch: analisi video con segmentazione temporale
///   - Ogni segmento ha: intervallo, trascrizione, key frame, riassunto
///   - Pipeline multimodale: frame visivi + trascrizione audio + riassunti
/// 
/// AI-103 KEY POINT:
///   "Implement video analysis workflows to process and interpret video segments"
///   → prebuilt-videoSearch restituisce SEGMENTI temporali, ciascuno con
///     descrizione, trascrizione e metadati.
/// 
/// CONFRONTO:
///   - Video Indexer: analisi ricchissima (volti, brand, emozioni, OCR)
///   - CU Video: analisi semantica, segmentazione, output RAG-ready
/// </summary>
internal class VideoAnalysis
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public VideoAnalysis(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> AnalyzeVideo(string videoFilePath)
    {
        const string analyzerId = "prebuilt-videoSearch";

        Console.WriteLine("🎬 Content Understanding: Analisi Video (prebuilt-videoSearch)");
        Console.WriteLine($"   File: {Path.GetFileName(videoFilePath)}");
        Console.WriteLine("   Pipeline: frame visivi + trascrizione audio + riassunti");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        var input = new AnalysisInput { Data = BinaryData.FromFile(videoFilePath) };
        Console.WriteLine("⏳ Analisi video in corso (può richiedere diversi minuti)...");
        var operation = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerId, new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine("=== ANALISI VIDEO (prebuilt-videoSearch) ===");
        sb.AppendLine();

        int seg = 0;
        foreach (var content in operation.Value.Contents)
        {
            seg++;
            sb.AppendLine($"--- 🎞️  SEGMENTO {seg} ---");

            // Campi del segmento: Summary, Transcript, Sentiment, Topics, ecc.
            if (content.Fields?.Count > 0)
            {
                foreach (var field in content.Fields)
                {
                    string value = field.Value.Value?.ToString() ?? "(vuoto)";
                    if (value.Length > 300) value = value[..300] + "...";
                    sb.AppendLine($"  • {field.Key}: \"{value}\"");
                    if (field.Value.Confidence.HasValue)
                        sb.AppendLine($"    confidence: {field.Value.Confidence.Value:P0}");
                }
            }

            // Markdown (trascrizione + key frames)
            if (!string.IsNullOrWhiteSpace(content.Markdown))
            {
                string preview = content.Markdown.Length > 400
                    ? content.Markdown[..400] + "..." : content.Markdown;
                sb.AppendLine($"  Contenuto:\n{preview}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"✅ Segmenti analizzati: {seg}");
        sb.AppendLine("💡 Casi d'uso: indicizzazione archivio video, highlight automatici, moderazione");
        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}