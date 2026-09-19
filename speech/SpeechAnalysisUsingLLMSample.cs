using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using AzureAiSamples.vision;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;

namespace AzureAiSamples.speech
{
    internal class SpeechAnalysisUsingLLMSample
    {
       
        private readonly OpenAIClient _OpenAIClient;
       

        public SpeechAnalysisUsingLLMSample(OpenAIClient openAiClient)
        {
            _OpenAIClient = openAiClient;
        }

        public static SpeechAnalysisUsingLLMSample FromUserCredentials(string foundryProjectUrl, string tenantId)
        {


            // 1. Autenticazione e creazione client
            AIProjectClient projectClient = new AIProjectClient(
                new Uri(foundryProjectUrl),
                new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                })
            );
            return new SpeechAnalysisUsingLLMSample(projectClient.GetProjectOpenAIClient());
        }

        public static SpeechAnalysisUsingLLMSample FromApiKey(string openAiURL, string password)
        {
            if (openAiURL.EndsWith("/"))
            {
                openAiURL = openAiURL.Substring(0, openAiURL.Length - 1);
            }
            if (!openAiURL.EndsWith("/openai/v1"))
            {
                openAiURL += "/openai/v1";
            }
            var opt = OpenAICLientOptionsFactory.GetOptions(openAiURL);
            var openAiClient = new OpenAIClient(new ApiKeyCredential(password),opt);
            return new SpeechAnalysisUsingLLMSample(openAiClient);
        }


        /// <summary>
        /// analizza un file audio usando le chat api. le responses api non supportano l'audio.
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="audioFilePath"></param>
        /// <returns></returns>
        public async Task<string> AnalyzeAudioWithModelAsync(string modelId, string audioFilePath)
        {
            byte[] audioBytes = await File.ReadAllBytesAsync(audioFilePath);

            string jsonPrompt =
                "Analizza l'audio e restituisci ESCLUSIVAMENTE un oggetto JSON valido, " +
                "senza blocchi di codice markdown (es. ```json) e senza testo prima o dopo. " +
                "Il JSON deve avere questa struttura esatta: " +
                "{" +
                "  \"Transcription\": \"la trascrizione integrale dell'audio\"," +
                "  \"EnglishTranslation\": \"traduzione in inglese\"," +
                "  \"Summary\": \"riassunto del contenuto\"," +
                "  \"SentimentScore\": 0" +
                "}";

            ChatClient chatClient = _OpenAIClient.GetChatClient(modelId);

            UserChatMessage userMessage = new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(jsonPrompt),
                ChatMessageContentPart.CreateInputAudioPart(
                    BinaryData.FromBytes(audioBytes),
                    ChatInputAudioFormat.Mp3));

            ChatCompletion completion = await chatClient.CompleteChatAsync(userMessage);

            return completion.Content[0].Text;
        }

      
    }
}