using Drastic.Tools;
using Drastic.Whisper.Models;
using Drastic.Whisper.Services;
using Drastic.Whisper.UI.Services;
using Drastic.Whisper.UI.Tools;
using Drastic.Whisper.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Drastic.WhisperSample.ViewModels
{
    public class DebugViewModel : TranscriptionViewModel
    {
        private AsyncCommand? sampleCommand;
        private string? urlField;
        private string? modelFile;
        private AsyncCommand? debugCommand;
        private string tempPath = Path.Combine(Path.GetTempPath(), "DrasticWhisper");
        private IWhisperService whisper;
        private CancellationTokenSource? cts;
        private double progress;
        private WhisperLanguage selectedLanguage;
        private string videoTitle = string.Empty;
        private ITranscodeService transcodeService;
        private YouTubeService youTubeService;
        private AsyncCommand? recordCommand;

        public DebugViewModel(IServiceProvider services)
            : base(services)
        {
            this.youTubeService = new YouTubeService();
            this.whisper = this.Services.GetRequiredService<IWhisperService>()!;
            this.whisper.OnProgress = this.OnProgress;
            this.whisper.OnNewWhisperSegment += this.Whisper_OnNewWhisperSegment;
            this.WhisperLanguages = WhisperLanguage.GenerateWhisperLangauages();
            this.selectedLanguage = this.WhisperLanguages[0];
            this.transcodeService = this.Services.GetRequiredService<ITranscodeService>();
            this.UrlField = "https://www.youtube.com/shorts/baYXWcVHZ-s";
        }

        public IReadOnlyList<WhisperLanguage> WhisperLanguages { get; }

        public WhisperLanguage SelectedLanguage
        {
            get
            {
                return this.selectedLanguage;
            }

            set
            {
                this.SetProperty(ref this.selectedLanguage, value);
                this.RaiseCanExecuteChanged();
            }
        }

        public string VideoTitle
        {
            get
            {
                return this.videoTitle;
            }

            set
            {
                this.SetProperty(ref this.videoTitle, value);
                this.RaiseCanExecuteChanged();
            }
        }

        public string? ModelFile
        {
            get
            {
                return this.modelFile;
            }

            set
            {
                this.SetProperty(ref this.modelFile, value);
                this.RaiseCanExecuteChanged();
            }
        }

        public string? UrlField
        {
            get
            {
                return this.urlField;
            }

            set
            {
                this.SetProperty(ref this.urlField, value);
                this.RaiseCanExecuteChanged();
            }
        }

        public bool IsIndeterminate => this.whisper.IsIndeterminate;

        public double Progress
        {
            get
            {
                return this.progress;
            }

            set
            {
                this.SetProperty(ref this.progress, value);
            }
        }

        public AsyncCommand SampleCommand => this.sampleCommand ??= new AsyncCommand(this.SampleAsync, null, this.Dispatcher, this.ErrorHandler);
        public AsyncCommand DebugCommand => this.debugCommand ??= new AsyncCommand(this.DebugAsync, () => !string.IsNullOrEmpty(this.UrlField), this.Dispatcher, this.ErrorHandler);

        public AsyncCommand RecordCommand => this.recordCommand ??= new AsyncCommand(this.RecordAsync, null, this.Dispatcher, this.ErrorHandler);

        public async Task RecordAsync()
        {
            if (string.IsNullOrEmpty(this.modelFile))
            {
                this.whisper.InitModel(WhisperSample.EmbeddedModels.LoadTinyModel(), this.SelectedLanguage);
            }
            else
            {
                this.whisper.InitModel(this.modelFile, this.SelectedLanguage);
            }
        }

        public Task SampleAsync()
        {
            this.Setup();

            return this.PerformBusyAsyncTask(
               async () =>
               {
                   if (string.IsNullOrEmpty(this.modelFile))
                   {
                       // Tinyのモデルの場合、Whisperに英語を使用するように強制します。
                       // 英語唯一サポートされているからです。
                       this.whisper.InitModel(WhisperSample.EmbeddedModels.LoadTinyModel(), this.WhisperLanguages.First(n => n.LanguageCode == "en"));
                   }
                   else
                   {
                       this.whisper.InitModel(this.modelFile, this.SelectedLanguage);
                   }

                   await this.whisper.ProcessAsync(WhisperSample.Samples.LoadJFK(), this.cts?.Token);
               },
               Whisper.UI.Translations.Common.GeneratingSubtitles);
        }

        private void Setup()
        {
            if (this.cts is not null)
            {
                this.cts.Cancel();
            }

            this.cts = new CancellationTokenSource();

            this.progress = 0;
            this.Subtitles.Clear();
            Directory.CreateDirectory(this.tempPath);
            this.VideoTitle = string.Empty;

            if (Directory.Exists(this.tempPath))
            {
                DirectoryInfo directory = new DirectoryInfo(this.tempPath);
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }
            }
        }

        public async Task DebugAsync()
        {
            ArgumentNullException.ThrowIfNull(nameof(this.ModelFile));
            ArgumentNullException.ThrowIfNull(nameof(this.UrlField));

            this.Setup();

            if (this.youTubeService.IsValidUrl(this.urlField ?? string.Empty))
            {
                await this.ParseAsync(await this.youTubeService.GetAudioUrlAsync(this.UrlField!), this.cts?.Token ?? CancellationToken.None);
                return;
            }

            if (File.Exists(this.UrlField))
            {
                await this.LocalFileParseAsync(this.UrlField!, this.cts?.Token ?? CancellationToken.None);
                return;
            }
        }

        public override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            this.DebugCommand.RaiseCanExecuteChanged();
            this.SampleCommand.RaiseCanExecuteChanged();
        }

        private async Task ParseAsync(string filepath, CancellationToken token)
        {
            string? audioFile = string.Empty;

            audioFile = await this.transcodeService.ProcessFile(filepath);
            if (string.IsNullOrEmpty(audioFile) || !File.Exists(audioFile))
            {
                return;
            }

            await this.PerformBusyAsyncTask(
                async () =>
                {
                    await this.GenerateCaptionsAsync(audioFile, token);
                },
                Whisper.UI.Translations.Common.GeneratingSubtitles);
        }

        private async Task LocalFileParseAsync(string filepath, CancellationToken token)
        {
            string? audioFile = string.Empty;

            if (!File.Exists(filepath))
            {
                return;
            }

            if (!DrasticWhisperFileExtensions.Supported.Contains(Path.GetExtension(filepath)))
            {
                return;
            }

            await this.ParseAsync(filepath, token);
        }

        private Task GenerateCaptionsAsync(string audioFile, CancellationToken token)
        {
            return this.PerformBusyAsyncTask(
                async () =>
                {
                    if (string.IsNullOrEmpty(this.modelFile))
                    {
                        this.whisper.InitModel(WhisperSample.EmbeddedModels.LoadTinyModel(), this.SelectedLanguage);
                    }
                    else
                    {
                        this.whisper.InitModel(this.modelFile, this.SelectedLanguage);
                    }

                    await this.whisper.ProcessAsync(audioFile, token);
                },
                Whisper.UI.Translations.Common.GeneratingSubtitles);
        }

        private void Whisper_OnNewWhisperSegment(object? sender, OnNewSegmentEventArgs segment)
        {
            var e = segment.Segment;
            System.Diagnostics.Debug.WriteLine($"CSSS {e.Start} ==> {e.End} : {e.Text}");
            var item = new SrtSubtitleLine() { Start = e.Start, End = e.End, Text = e.Text, LineNumber = this.Subtitles.Count() + 1 };

            this.Dispatcher.Dispatch(() => this.Subtitles.Add(item));
        }

        public void OnProgress(double progress)
            => this.Progress = progress * 100;
    }
}

