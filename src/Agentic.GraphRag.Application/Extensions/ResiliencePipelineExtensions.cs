using Microsoft.Extensions.AI;
using Polly;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Application.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
internal static class ResiliencePipelineExtensions
{
    extension(ResiliencePipeline resiliencePipeline)
    {
        public async Task<ReadOnlyMemory<float>> GetTextEmbedding(
            string text,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            CancellationToken cancellationToken)
        {
            var embedding = await resiliencePipeline.ExecuteAsync(
            async ct =>
            {
                return await embeddingGenerator
                    .GenerateVectorAsync(text, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

            return embedding;
        }
    }
}
