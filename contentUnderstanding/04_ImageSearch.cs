using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Visual Understanding con Content Understanding.
/// 
/// COSA IMPARERAI:
///   - prebuilt-imageSearch: analisi visiva di immagini standalone
///   - Descrizione semantica + insight visivi (vs. tags deterministici di Computer Vision)
///   - Ottimizzato per catalogazione e ricerca visiva
/// 
/// AI-103 KEY POINT:
///   "Implement visual understanding by configuring Azure Content Understanding
///    in Foundry Tools to extract visual characteristics"
///   → prebuilt-imageSearch è l'analyzer per la comprensione visiva.
/// 
/// CONFRONTO con Classical Vision (Azure.AI.Vision.ImageAnalysis):
///   - Classical Vision: tags, oggetti, OCR (deterministico)
///   - CU imageSearch: descrizione semantica, insight generativi
///   → Complementari: Vision per detection, CU per comprensione
/// </summary>
internal class ImageSearch
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public ImageSearch(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> AnalyzeImage(string imagePath)
    {
        const string analyzerId = "prebuilt-imageSearch";

        Console.WriteLine("🖼️  Content Understanding: Visual Understanding (prebuilt-imageSearch)");
        Console.WriteLine($"   Immagine: {Path.GetFileName(imagePath)}");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        var input = new AnalysisInput { Data = BinaryData.FromFile(imagePath) };
        var operation = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerId, new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine("=== VISUAL UNDERSTANDING (prebuilt-imageSearch) ===");
        sb.AppendLine();

        foreach (var content in operation.Value.Contents)
        {
            sb.AppendLine($"📌 Tipo: documento");

            // Insight visivi (es. Summary, oggetti, caratteristiche)
            if (content.Fields?.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- 🏷️  Insight visivi ---");
                foreach (var field in content.Fields)
                {
                    sb.AppendLine($"  • {field.Key}: \"{field.Value.Value}\"");
                    if (field.Value.Confidence.HasValue)
                        sb.AppendLine($"    confidence: {field.Value.Confidence.Value:P0}");
                }
            }

            // Descrizione testuale dell'immagine
            if (!string.IsNullOrWhiteSpace(content.Markdown))
            {
                sb.AppendLine();
                sb.AppendLine("--- 📝 Descrizione ---");
                sb.AppendLine(content.Markdown);
            }
            sb.AppendLine();
        }

        sb.AppendLine("💡 Output pronto per: ricerca visiva, catalogazione, input LLM");
        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}