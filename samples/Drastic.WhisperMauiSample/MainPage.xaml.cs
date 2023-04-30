using Drastic.WhisperSample.ViewModels;

namespace Drastic.WhisperMauiSample;

public partial class MainPage : ContentPage
{
    private DebugViewModel vm;

    public MainPage(IServiceProvider provider)
    {
        this.InitializeComponent();
        this.BindingContext = this.vm = provider.GetRequiredService<DebugViewModel>();
    }

    private async void PickAFileButton_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new()
        {
            PickerTitle = Drastic.Whisper.UI.Translations.Common.OpenFileButton,
        };

        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null)
            {
                if (Drastic.Whisper.UI.Tools.DrasticWhisperFileExtensions.VideoExtensions.Contains(Path.GetExtension(result.FileName)) || Drastic.Whisper.UI.Tools.DrasticWhisperFileExtensions.AudioExtensions.Contains(Path.GetExtension(result.FileName)))
                {
                    this.vm.UrlField = result.FullPath;
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void PickAModelButton_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new()
        {
            PickerTitle = Drastic.Whisper.UI.Translations.Common.OpenModelButton,
        };

        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                if (result.FileName.EndsWith("bin", StringComparison.OrdinalIgnoreCase))
                {
                    this.vm.ModelFile = result.FullPath;
                }
            }
        }
        catch (Exception ex)
        {
        }
    }
}