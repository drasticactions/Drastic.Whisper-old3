// <copyright file="TranscriptionViewModel.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using Drastic.ViewModels;
using Drastic.Whisper.Models;

namespace Drastic.Whisper.UI.ViewModels
{
    public class TranscriptionViewModel : BaseViewModel
    {
        public TranscriptionViewModel(IServiceProvider services)
            : base(services)
        {
        }

        public TranscriptionViewModel(string srtText, IServiceProvider services)
            : base(services)
        {
            var subtitle = new SrtSubtitle(srtText);
            this.Subtitles.Clear();
            foreach (var item in subtitle.Lines)
            {
                this.Subtitles.Add(item);
            }
        }

        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        public ObservableCollection<ISubtitleLine> Subtitles { get; } = new ObservableCollection<ISubtitleLine>();
    }
}
