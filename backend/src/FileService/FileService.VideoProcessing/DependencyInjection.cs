using FileService.VideoProcessing.Pipeline;
using FileService.VideoProcessing.Pipeline.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.VideoProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddVideoProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VideoProcessingOptions>(
            configuration.GetSection(VideoProcessingOptions.SECTION_NAME));

        services.AddScoped<IProcessingPipeline, ProcessingPipeline>();
        services.AddScoped<IProcessingStepHandler, InitializeStepHandler>();
        services.AddScoped<IProcessingStepHandler, ExtractMetadataStepHandler>();
        services.AddScoped<IProcessingStepHandler, MockPrepareOutputsStep>();
        services.AddScoped<IProcessingStepHandler, MockUploadResultsStep>();
        services.AddScoped<IProcessingStepHandler, MockGeneratePreviewStep>();
        services.AddScoped<IProcessingStepHandler, MockCleanupStep>();
        services.AddScoped<VideoProcessingService>();

        return services;
    }
}
