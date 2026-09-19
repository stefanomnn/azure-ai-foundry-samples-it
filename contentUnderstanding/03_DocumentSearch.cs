using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Output RAG-Ready con Content Understanding.
/// 
/// COSA IMPARERAI:
///   - prebuilt-documentSearch: output ottimizzato per RAG e vector store
///   - Ogni content è un CHUNK granulare (paragrafo, tabella, figura)
///   - Metadati (Fields) + Markdown = pronto per vector store o LLM
/// 
/// AI-103 KEY POINT:
///   "Produce clean, grounded representations to use with agents and RAG"
///   → prebuilt-documentSearch genera output chunk-per-chunk, direttamente
///     indicizzabile in AI Search, pronto per RAG.
/// </summary>
internal class DocumentSearch
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public DocumentSearch(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> AnalyzeForRag(string filePath)
    {
        const string analyzerId = "prebuilt-documentSearch";

        Console.WriteLine("🔍 Content Understanding: RAG-Ready (prebuilt-documentSearch)");
        Console.WriteLine($"   File: {Path.GetFileName(filePath)}");
        Console.WriteLine("   Output: chunk granulari con metadati (pronti per vector store)");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        var input = new AnalysisInput { Data = BinaryData.FromFile(filePath) };
        var operation = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerId, new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine("=== OUTPUT RAG-READY (prebuilt-documentSearch) ===");
        sb.AppendLine("📌 Ogni 'content' è un chunk indipendente per vector store / LLM\n");

        int i = 0;
        foreach (var content in operation.Value.Contents)
        {
            i++;
            sb.AppendLine($"--- 📦 CHUNK {i} ---");

            // Metadati (front-matter concettuale, diventa YAML nel caso reale)
            if (content.Fields?.Count > 0)
            {
                sb.AppendLine("  fields:");
                foreach (var f in content.Fields)
                    sb.AppendLine($"    {f.Key}: \"{f.Value.Value}\"");
            }

            // Pagine di origine (grounding)
            if (content is DocumentContent doc && doc.Pages?.Count > 0)
                sb.AppendLine($"  pages: {string.Join(", ", doc.Pages.Select(p => p.PageNumber))}");

            // Contenuto markdown
            if (!string.IsNullOrWhiteSpace(content.Markdown))
            {
                string preview = content.Markdown.Length > 400
                    ? content.Markdown[..400] + "..." : content.Markdown;
                sb.AppendLine($"  --- markdown ---\n{preview}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"✅ Totale chunk: {i}");
        sb.AppendLine("💡 Ogni chunk è pronto per essere indicizzato in un vector store.");
        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}