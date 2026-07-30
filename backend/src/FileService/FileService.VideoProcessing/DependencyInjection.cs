using FileService.VideoProcessing.FfmpegProcess;
using FileService.VideoProcessing.Pipeline;
using FileService.VideoProcessing.Pipeline.Steps;
using FileService.VideoProcessing.ProcessRunner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.VideoProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddVideoProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VideoProcessingOptions>(
            configuration.GetSection(VideoProcessingOptions.SECTION_NAME));

        services.AddScoped<IFfmpegProcessRunner, FfmpegProcessRunner>();
        services.AddScoped<IProcessRunner, global::FileService.VideoProcessing.ProcessRunner.ProcessRunner>();

        services.AddScoped<IProcessingPipeline, ProcessingPipeline>();
        services.AddScoped<IProcessingStepHandler, InitializeStepHandler>();
        services.AddScoped<IProcessingStepHandler, ExtractMetadataStepHandler>();
        services.AddScoped<IProcessingStepHandler, MockPrepareOutputsStep>();
        services.AddScoped<IProcessingStepHandler, UploadHlsStepHandler>();
        services.AddScoped<IProcessingStepHandler, GeneratePreviewStepHandler>();
        services.AddScoped<IProcessingStepHandler, CleanupStepHandler>();
        services.AddScoped<VideoProcessingService>();

        return services;
    }
}
