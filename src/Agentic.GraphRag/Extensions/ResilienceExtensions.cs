using Agentic.GraphRag.Application.Settings;
using Polly;
using Polly.Retry;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Agentic.GraphRag.Extensions;

internal static class ResilienceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOllamaResilienceHandler()
        {
            services.ConfigureHttpClientDefaults(http =>
            {
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
                http.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

                // Turn on resilience by default
                http.AddStandardResilienceHandler(config =>
                {
                    // Extend the HTTP Client timeout for Ollama
                    config.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);

                    // Must be at least double the AttemptTimeout to pass options validation
                    config.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                    config.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
                });

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            return services;
        }

        [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Logging should be infrequent")]
        public IServiceCollection AddTooManyRequestsResiliencePipeline() =>
           services.AddResiliencePipeline(ResiliencePipelineNames.RateLimitHitRetry, (pipelineBuilder, context) =>
           {
               pipelineBuilder.AddRetry(new RetryStrategyOptions
               {
                   ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>(x =>
                            x is { StatusCode: HttpStatusCode.TooManyRequests
                                            or HttpStatusCode.RequestTimeout
                                            or >= (HttpStatusCode)500
                            })
                        .Handle<System.ClientModel.ClientResultException>(x =>
                            x is { Status: (int)HttpStatusCode.TooManyRequests
                                        or (int)HttpStatusCode.RequestTimeout
                                        or >= (int)(HttpStatusCode)500}),
                   /* Linear backoff is simpler and sufficient for many cases. We are using a 20 second linear delay here.
                    * For GitHub rate limiting, the Retry-After header is often provided, making linear backoff effective.
                    * Consider switching to Exponential if you observe persistent rate limiting issues.
                    */
                   Delay = TimeSpan.FromSeconds(20),
                   //TODO: Remove either Delay or  DelayGenerator after we confirm that we can get Retry-After header in
                   DelayGenerator = d =>
                   {
                       //TODO: Can we get request  Retry-After header here?
                       return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(20));
                   },
                   MaxRetryAttempts = 5,
                   BackoffType = DelayBackoffType.Linear,
                   UseJitter = true,
                   OnRetry = args =>
                   {
                       context.ServiceProvider.GetService<ILogger>()?
                           //.LogWarning("{PolicyName} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. Status code {StatusCode}. {ExceptionMessage}",
                           .LogWarning("{Pipeline} retry policy will attempt retry {Retry} in {Delay}ms after a transient error or timeout. Outcome type is {OutcomeType}.",
                               ResiliencePipelineNames.RateLimitHitRetry,
                               args.AttemptNumber,
                               args.RetryDelay.TotalMilliseconds,
                               args.Outcome.GetType().Name
                               //args.Outcome is HttpResponseMessage ? ((HttpResponseMessage)args.Outcome).Result?.StatusCode : "unknown",
                               //args.Outcome.Exception.GetAllMessages()
                               );

                       return ValueTask.CompletedTask;
                   }
               });
           });
    }
}
