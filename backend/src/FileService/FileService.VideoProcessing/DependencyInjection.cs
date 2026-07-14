using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.VideoProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddVideoProcessing(this IServiceCollection services)
    {
        services.AddScoped<IProcessingPipeline, ProcessingPipeline>();
        services.AddScoped<IProcessingStepHandler, MockExtractMetadataStep>();
        services.AddScoped<IProcessingStepHandler, MockUploadResultsStep>();
        services.AddScoped<IProcessingStepHandler, MockPrepareOutputsStep>();

        return services;
    }
}