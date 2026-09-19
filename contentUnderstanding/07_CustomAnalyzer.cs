using Azure;
using Azure.AI.ContentUnderstanding;
using System.Text;

namespace AzureAiSamples.contentUnderstanding;

/// <summary>
/// SAMPLE DIDATTICO: Custom Analyzer con Content Understanding.
/// 
/// COSA IMPARERAI:
///   - Creare analyzer personalizzato con ContentFieldSchema e ContentFieldDefinition
///   - BaseAnalyzerId = "prebuilt-document" (eredita OCR + layout)
///   - Campi tipizzati: String, Number, Date, Array con GenerationMethod.Extract/Classify
///   - CreateAnalyzerAsync si fa UNA VOLTA (poi si riusa l'analyzer)
/// 
/// AI-103 KEY POINT:
///   Custom analyzer = PRO-MODE. Ereditiamo prebuilt-document e aggiungiamo
///   i NOSTRI campi. Zero training, basta descrivere i campi.
/// </summary>
internal class CustomAnalyzer
{
    private readonly string _EndpointUrl;
    private readonly string _ApiKey;

    public CustomAnalyzer(string endpointUrl, string apiKey)
    {
        _EndpointUrl = endpointUrl;
        _ApiKey = apiKey;
    }

    public async Task<string> CreateAndAnalyze(string filePath, string analyzerName = "sample-fattura-analyzer")
    {
        Console.WriteLine("🔧 Content Understanding: Custom Analyzer (PRO-MODE)");
        Console.WriteLine($"   Analyzer: {analyzerName} | Base: prebuilt-document");
        Console.WriteLine();

        var clientOptions = new ContentUnderstandingClientOptions(
            ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);
        var client = new ContentUnderstandingClient(
            new Uri(_EndpointUrl), new AzureKeyCredential(_ApiKey), clientOptions);

        // ── STEP 1: Definizione schema campi ──────────────────────────
        var fieldSchema = new ContentFieldSchema(
            new Dictionary<string, ContentFieldDefinition>
            {
                ["nome_fornitore"] = new ContentFieldDefinition
                {
                    Type = ContentFieldType.String,
                    Description = "Nome dell'azienda che ha emesso la fattura"
                },
                ["data_fattura"] = new ContentFieldDefinition
                {
                    Type = ContentFieldType.Date,
                    Description = "Data emissione fattura (YYYY-MM-DD)"
                },
                ["numero_fattura"] = new ContentFieldDefinition
                {
                    Type = ContentFieldType.String,
                    Description = "Numero o codice identificativo fattura"
                },
                ["importo_totale"] = new ContentFieldDefinition
                {
                    Type = ContentFieldType.Number,
                    Description = "Importo totale, IVA inclusa"
                },
                ["tipo_documento"] = new ContentFieldDefinition
                {
                    Type = ContentFieldType.String,
                    Method = GenerationMethod.Classify,
                    Description = "Tipo documento (fattura, nota credito, ricevuta)"
                }
            })
        {
            Name = "schema_fattura",
            Description = "Schema per estrazione dati da fatture"
        };
        fieldSchema.Fields["tipo_documento"].Enum.Add("fattura");
        fieldSchema.Fields["tipo_documento"].Enum.Add("nota di credito");
        fieldSchema.Fields["tipo_documento"].Enum.Add("ricevuta");
// ── STEP 2: Creazione analyzer ──────────────────────────────
        var customAnalyzer = new ContentAnalyzer
        {
            BaseAnalyzerId = "prebuilt-document",
            Description = "Analyzer custom per fatture italiane",
            FieldSchema = fieldSchema
        };
        customAnalyzer.Models["completion"] = "gpt-5.2";
        customAnalyzer.Models["embedding"] = "text-embedding-3-large";

        Console.WriteLine("⚙️  STEP 1: Creazione custom analyzer...");
        try
        {
            await client.CreateAnalyzerAsync(
                WaitUntil.Completed, analyzerName, customAnalyzer);
            Console.WriteLine($"   ✅ Analyzer '{analyzerName}' creato!");
            Console.WriteLine("   Campi: nome_fornitore, data_fattura, numero_fattura, importo_totale, tipo_documento");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  {ex.Message}");
        }
        Console.WriteLine();

        // ── STEP 3: Esecuzione analyzer ────────────────────
        Console.WriteLine("🚀 STEP 2: Analisi del file...");
        var input = new AnalysisInput { Data = BinaryData.FromFile(filePath) };
        var analyzeOp = await client.AnalyzeAsync(
            WaitUntil.Completed, analyzerName, new List<AnalysisInput> { input });

        var sb = new StringBuilder();
        sb.AppendLine($"=== RISULTATO: {analyzerName} ===");
        sb.AppendLine();

        foreach (var content in analyzeOp.Value.Contents)
        {
            if (content.Fields?.Count > 0)
            {
                sb.AppendLine("--- 🏷️  CAMPI CUSTOM ESTRATTI ---");
                foreach (var field in content.Fields)
                {
                    sb.AppendLine($"  • {field.Key}: \"{field.Value.Value}\"");
                    if (field.Value.Confidence.HasValue)
                        sb.AppendLine($"    confidence: {field.Value.Confidence.Value:P0}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(content.Markdown))
            {
                sb.AppendLine("--- 📝 MARKDOWN (OCR + layout) ---");
                sb.AppendLine(content.Markdown.Length > 400
                    ? content.Markdown[..400] + "..." : content.Markdown);
            }
        }

        sb.AppendLine();
        sb.AppendLine("💡 AI-103: Custom analyzer = prebuilt-document + TUO field_schema. Zero training.");
        string result = sb.ToString();
        Console.WriteLine(result);
        return result;
    }
}