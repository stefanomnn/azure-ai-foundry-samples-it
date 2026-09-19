using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AzureAiSamples.otherTopics;

/// <summary>
/// Mostra come tracciare le chiamate LLM con OpenTelemetry ed esportare
/// i dati su Azure Application Insights.
///
/// TABELLE DA CONSULTARE IN APPLICATION INSIGHTS (Kusto):
///   requests      → operation radice (es. "LLM_Call")
///   dependencies  → sotto-operazioni (es. HTTP POST a foundry)
///   traces        → log generati via ActivityEvent (es. "prompt inviato")
///   exceptions    → eccezioni catturate automaticamente
///
/// QUERY KUSTO DI ESEMPIO:
///   dependencies
///   | where name contains "LLM"
///   | project timestamp, name, duration, resultCode
///   | order by timestamp desc
/// </summary>
internal class OpenTelemetrySample
{
    private static readonly ActivitySource _Source = new("ai-foundry-samples", "1.0.0");

    /// <summary>
    /// Configura OpenTelemetry e simula una chiamata LLM tracciata.
    /// </summary>
    /// <param name="appInsightsConnectionString">
    /// Connection string di Application Insights (es. "InstrumentationKey=...;IngestionEndpoint=...")
    /// Si trova nel portale Azure → Application Insights → Overview → Connection string.
    /// </param>
    public void Run(string appInsightsConnectionString)
    {
        // ── SETUP: TracerProvider con exporter Azure Monitor ──────────
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("ai-foundry-samples", serviceInstanceId: Environment.MachineName))
            .AddSource("ai-foundry-samples")   // aggancia l'ActivitySource qui sopra
            .AddAzureMonitorTraceExporter(o => o.ConnectionString = appInsightsConnectionString)
            .Build();

        // ── SIMULAZIONE: chiamata LLM tracciata ──────────────────────
        SimulateLlmCall("gpt-5-mini", "quanto fa 42 * 13?", 250);
        SimulateLlmCall("gpt-5-full", "scrivi una poesia sulla programmazione", 1200);

        // ── FLUSH: forza invio degli span ad App Insights ──────────
        tracerProvider.ForceFlush();
        Console.WriteLine("✅ Trace inviati ad Application Insights.");
        Console.WriteLine("📊 Vai su App Insights → Logs e prova:");
        Console.WriteLine("   dependencies | where name contains 'LLM' | order by timestamp desc");
    }

    private static void SimulateLlmCall(string modelId, string prompt, int latencyMs)
    {
        // Activity è uno SPAN: diventa una riga nella tabella "requests" (o "dependencies")
        using var activity = _Source.StartActivity("LLM_Call", ActivityKind.Client);

        activity?.SetTag("gen_ai.model_id", modelId);
        activity?.SetTag("gen_ai.prompt_length", prompt.Length);
        activity?.SetTag("gen_ai.temperature", 0.3);
        activity?.SetTag("gen_ai.max_tokens", 1000);
        activity?.SetTag("gen_ai.system", "azure-foundry");

        // ActivityEvent diventa una riga nella tabella "traces"
        activity?.AddEvent(new ActivityEvent("prompt_inviato",
            tags: new ActivityTagsCollection { ["prompt_preview"] = prompt[..Math.Min(50, prompt.Length)] }));

        // Simula latenza rete + LLM
        Thread.Sleep(latencyMs);

        activity?.AddEvent(new ActivityEvent("output_ricevuto",
            tags: new ActivityTagsCollection { ["tokens_output"] = 150 }));

        Console.WriteLine($"[Trace] LLM_Call: model={modelId}, durata={activity?.Duration.TotalMilliseconds:F0}ms");
    }
}