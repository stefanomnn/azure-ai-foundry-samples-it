using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Layout Analysis con Azure AI Content Understanding.
/// 
/// COSA IMPARERAI:
///   - prebuilt-layout: estrae la STRUTTURA del documento preservando gerarchia
///   - Output: markdown con titoli, paragrafi, tabelle, figure
///   - Single-task: solo layout, senza field extraction. NON richiede model deployments
/// 
/// AI-103 KEY POINT:
///   prebuilt-layout preserva la struttura gerarchica: h1/h2/h3, tabelle in
///   markdown nativo, figure con didascalia. Direttamente consumabile da LLM.
/// </summary>
internal class PrebuiltLayout
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public PrebuiltLayout(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> AnalyzeLayout(string filePath)
    {
        const string analyzerId = "prebuilt-layout";

        Console.WriteLine("📐 Content Understanding: Layout (prebuilt-layout — SINGLE-TASK)");
        Console.WriteLine($"   File: {Path.GetFileName(filePath)}");
        Console.WriteLine($"   Analyzer: {analyzerId}");
        Console.WriteLine("   ⚠️  NON richiede model deployments");
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
        sb.AppendLine("=== RISULTATO LAYOUT (prebuilt-layout) ===");
        sb.AppendLine();

        foreach (var content in operation.Value.Contents)
        {
            // Pagine (cast a DocumentContent)
            if (content is DocumentContent doc && doc.Pages?.Count > 0)
            {
                sb.AppendLine($"📐 {doc.Pages.Count} pagine");
            }

            // Markdown strutturato: gerarchia, tabelle e figure già renderizzate
            if (!string.IsNullOrWhiteSpace(content.Markdown))
                sb.AppendLine(content.Markdown);
        }

        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}