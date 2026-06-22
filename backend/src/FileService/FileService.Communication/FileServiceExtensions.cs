using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace FileService.Communication;

// Чтобы указать: services.AddFilesService(configuration); в другом микросервисе.
public static class FileServiceExtensions
{
    public static IServiceCollection AddFilesServiceHttpCommunication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileServiceOptions>(configuration.GetSection(nameof(FileServiceOptions)));

        IHttpClientBuilder httpClientBuilder = services
            .AddHttpClient<IFileCommunicationService, FileHttpClient>((serviceProvider, clientConfig) =>
            {
                FileServiceOptions fileOptions = serviceProvider
                    .GetRequiredService<IOptions<FileServiceOptions>>()
                    .Value;

                clientConfig.BaseAddress = new Uri(fileOptions.Url);
                clientConfig.Timeout = TimeSpan.FromSeconds(fileOptions.TimeoutSeconds);
            });

        httpClientBuilder
            .AddStandardResilienceHandler()
            .Configure(options =>
            {
                FileServiceOptions fileOptions = configuration
                    .GetSection(FileServiceOptions.SectionName)
                    .Get<FileServiceOptions>()
                    ?? new FileServiceOptions();

                // Ограничиваем время всей операции
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(fileOptions.TimeoutSeconds);

                // Ограничвает одну попытку
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(fileOptions.AttemptTimeoutSeconds);

                // Определяет количество повторных попыток
                options.Retry.MaxRetryAttempts =
                    fileOptions.RetryCount;

                // Задает базовую паузу перед повторной попыткой
                options.Retry.Delay =
                    TimeSpan.FromMilliseconds(fileOptions.RetryDelayMilliseconds);

                // Каждая следующая пауза становится длиннее
                options.Retry.BackoffType =
                    DelayBackoffType.Exponential;

                // Добавляет небольшую случайность к задержкам
                options.Retry.UseJitter = true;

                // Определяет долю временных ошибок после которой цепь будет открыта 
                // (если как миниимум половина учитываемых запросов завершилась временной ошибкой,
                // circuit breaker может открыться)
                options.CircuitBreaker.FailureRatio =
                    fileOptions.CircuitBreakerFailureRatio;

                // Минимальное количество запросов для принятия решения
                options.CircuitBreaker.MinimumThroughput =
                    fileOptions.CircuitBreakerMinimumThroughput;

                // Временное окно, внутри которого считаются запросы и ошибки
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(fileOptions.CircuitBreakerSamplingSeconds);

                // В течение n секунд запрос сразу завершится с BrokenCircuitException
                options.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(fileOptions.CircuitBreakerBreakSeconds);
            });

        return services;
    }
}