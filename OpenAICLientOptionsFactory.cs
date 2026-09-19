using OpenAI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AzureAiSamples
{
    internal class OpenAICLientOptionsFactory
    {
        public static OpenAIClientOptions GetOptions(string endpoint) {

            var opt = new OpenAIClientOptions()
            {
                Endpoint=new Uri(endpoint),
                ClientLoggingOptions = new System.ClientModel.Primitives.ClientLoggingOptions
                {
                    EnableLogging = true,
                    EnableMessageLogging = true,
                    EnableMessageContentLogging = true,

                }
            };

            opt.AddPolicy(new HttpLoggingPolicy(), System.ClientModel.Primitives.PipelinePosition.PerCall);

            return opt;
        }
    }
}
