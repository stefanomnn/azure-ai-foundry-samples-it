using System;
using System.Collections.Generic;
using System.Text;

namespace AzureAiSamples
{
    using Microsoft.Extensions.Options;
    using OpenAI;
    using OpenAI.Images;
    using System;
    using System.ClientModel;
    using System.ClientModel.Primitives;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    // 1. Definisci la policy correttamente ereditando da Pipelinecriptive/PipelinePolicy
    public class HttpLoggingPolicy : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            Console.WriteLine($"[HTTP REQUEST] {message.Request.Method} {message.Request.Uri}");

            // Passa il controllo al policy successivo nella pipeline
            ProcessNext(message, pipeline, currentIndex);

            Console.WriteLine($"[HTTP RESPONSE] Status: {message.Response.Status}");
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            Console.WriteLine($"[HTTP REQUEST] {message.Request.Method} {message.Request.Uri}");

            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);

            Console.WriteLine($"[HTTP RESPONSE] Status: {message.Response.Status}");
        }
    }

}
