using Drastic.Services;
using Drastic.Whisper.Services;
using Drastic.Whisper.UI.Services;
using Drastic.WhisperMauiSample.Services;
using Drastic.WhisperSample.ViewModels;
using Microsoft.Extensions.Logging;

namespace Drastic.WhisperMauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if MACCATALYST
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("ButtonChange", (handler, view) =>
        {
            handler.PlatformView.PreferredBehavioralStyle = UIKit.UIBehavioralStyle.Pad;
        });
#endif

#if ANDROID || IOS
        LibVLCSharp.Shared.Core.Initialize();
#endif
        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Drastic.Whisper");
        var generatedFilename = "generated";
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IAppDispatcher, MauiAppDispatcher>()
            .AddSingleton<IErrorHandlerService, MauiErrorHandler>()
            .AddSingleton<IWhisperService, DefaultWhisperService>()
            .AddSingleton<YouTubeService>()
#if ANDROID || IOS
            .AddSingleton<ITranscodeService>(new VlcTranscodeService(generatedFilename: generatedFilename))
#else
            .AddSingleton<ITranscodeService>(new FFMpegTranscodeService(basePath, generatedFilename))
#endif
            .AddSingleton<DebugViewModel>();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
