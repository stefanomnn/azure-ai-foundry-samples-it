using Microsoft.Extensions.Configuration;

namespace AzureAiSamples
{
    internal class Variabili
    {
       
        public static string GPT_5_Mini { get; private set; } = string.Empty;
        public static string GPT_5_Full { get; private set; } = string.Empty;
        public static string ImageGenerationModel { get; private set; }
        public static string VideoGenerationModel { get; private set; }
        public static string TextEmbeddingModel_Small { get; private set; }
        public static string TextEmbeddingModel_Large { get; private set; }
        public static string AudioAnalysis { get; private set; }
         public static string AgentDemo { get; private set; } = string.Empty;
        public static string DocumentIntelligenceCustomAnalyzer { get; private set; }
        public static string FoundryResourceName { get; private set; } = string.Empty;
        public static string FoundryProjectName { get; private set; } = string.Empty;

        public static string CognitiveServiceUrl =>$"https://{FoundryResourceName}.cognitiveservices.azure.com";
        public static string FoundryResourceUrl =>$"https://{FoundryResourceName}.services.ai.azure.com";
        public static string ProjectUrl =>$"{FoundryResourceUrl}/api/projects/{FoundryProjectName}";
        public static string CognitiveServiceAPI_KEY { get; private set; } = string.Empty;

        
        public static string ContentSafetyUrl { get; private set; }
        public static string ContentSafetyKey { get; private set; }
        public static string? ApplicationInsightsConnectionString { get; private set; }
        public static string? TenantId { get; private set; }

        // Video Indexer
        public static string VideoIndexerAccountId { get; private set; } = string.Empty;
        public static string VideoIndexerToken { get; private set; }
        public static string VideoIndexerRegion { get; private set; }
        public static string VideoIndexerLocation { get; private set; } = string.Empty;

        public static void Load()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            FoundryProjectName= configuration["AzureAI:FoundryProjectName"] ?? string.Empty;
            FoundryResourceName = configuration["AzureAI:FoundryResourceName"] ?? string.Empty;
            CognitiveServiceAPI_KEY = configuration["AzureAI:CognitiveServiceAPI_KEY"] ?? string.Empty;
            GPT_5_Mini = configuration["Models:GPT5Mini"] ?? string.Empty;
            GPT_5_Full = configuration["Models:GPT5Full"] ?? string.Empty;
            ImageGenerationModel = configuration["Models:ImageGenerationModel"] ?? string.Empty;
            VideoGenerationModel = configuration["Models:VideoGenerationModel"] ?? string.Empty;

            TextEmbeddingModel_Small = configuration["Models:EmbeddingModelSmall"] ?? string.Empty;
            TextEmbeddingModel_Large = configuration["Models:EmbeddingModelLarge"] ?? string.Empty;
            AudioAnalysis = configuration["Models:AudioAnalysis"] ?? string.Empty;
       
            VideoIndexerAccountId = configuration["AzureAI:VideoIndexerAccountId"] ?? string.Empty;
            VideoIndexerToken = configuration["AzureAI:VideoIndexerToken"] ?? string.Empty;
            VideoIndexerRegion = configuration["AzureAI:VideoIndexerRegion"] ?? string.Empty;

            

            AgentDemo = configuration["Models:AgentDemo"] ?? string.Empty;
            DocumentIntelligenceCustomAnalyzer = configuration["Models:DocumentIntelligenceCustomAnalyzer"] ?? string.Empty;

            ContentSafetyUrl= configuration["AzureAI:ContentSafetyUrl"] ?? string.Empty;
            ContentSafetyKey = configuration["AzureAI:ContentSafetyKey"] ?? string.Empty;

            ApplicationInsightsConnectionString = configuration["AzureAI:ApplicationInsightsConnectionString"];
            TenantId = configuration["AzureAI:TenantId"];

            // Video Indexer
            VideoIndexerAccountId = configuration["AzureAI:VideoIndexerAccountId"] ?? string.Empty;
            VideoIndexerLocation = configuration["AzureAI:VideoIndexerLocation"] ?? string.Empty;
        }
    }
}