using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.VideoProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddVideoProcessing(this IServiceCollection services)
    {
        services.AddScoped<IProcessingPipeline, ProcessingPipeline>();
        services.AddScoped<IProcessingStepHandler, MockInitializeStep>();
        services.AddScoped<IProcessingStepHandler, MockExtractMetadataStep>();
        services.AddScoped<IProcessingStepHandler, MockPrepareOutputsStep>();
        services.AddScoped<IProcessingStepHandler, MockUploadResultsStep>();
        services.AddScoped<IProcessingStepHandler, MockGeneratePreviewStep>();
        services.AddScoped<IProcessingStepHandler, MockCleanupStep>();
        services.AddScoped<VideoProcessingService>();

        return services;
    }
}
