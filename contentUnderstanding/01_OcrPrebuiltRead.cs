using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: OCR puro con Azure AI Content Understanding.
/// 
/// COSA IMPARERAI:
///   - Usare l'analyzer prebuilt-read per estrarre SOLO il testo da un documento
///   - Questo è il "single-task mode": un analyzer che fa UNA cosa sola (OCR)
///   - NON richiede model deployments (nessun LLM/embedding)
///   - Output: markdown con il testo riconosciuto
/// 
/// AI-103 KEY POINT:
///   prebuilt-read = SINGLE-TASK. Fa solo OCR, zero analisi semantica.
///   È il mattone più semplice della pipeline Content Understanding.
/// 
/// CONFRONTO con prebuilt-document (PRO-MODE):
///   - prebuilt-read: solo testo grezzo
///   - prebuilt-document: OCR + layout + campi chiave-valore
/// </summary>
internal class OcrPrebuiltRead
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public OcrPrebuiltRead(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> ExtractText(string filePath)
    {
        const string analyzerId = "prebuilt-read";

        Console.WriteLine("📄 Content Understanding: OCR puro (prebuilt-read — SINGLE-TASK)");
        Console.WriteLine($"   File: {Path.GetFileName(filePath)}");
        Console.WriteLine($"   Analyzer: {analyzerId}");
        Console.WriteLine("   ⚠️  NON richiede model deployments (nessun LLM)");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        var input = new AnalysisInput { Data = BinaryData.FromFile(filePath) };

        var operation = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerId,
            new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine("=== RISULTATO OCR (prebuilt-read) ===");
        sb.AppendLine();

        foreach (var content in operation.Value.Contents)
        {
            // Cast a DocumentContent per accedere ai metadati pagina
            if (content is DocumentContent doc)
            {
                if (doc.Pages?.Count > 0)
                {
                    sb.AppendLine("--- 📐 Pagine ---");
                    foreach (var page in doc.Pages)
                        sb.AppendLine($"  Pagina {page.PageNumber}: {page.Width}x{page.Height}px");
                    sb.AppendLine();
                }
            }

            if (!string.IsNullOrWhiteSpace(content.Markdown))
                sb.AppendLine(content.Markdown);
        }

        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}