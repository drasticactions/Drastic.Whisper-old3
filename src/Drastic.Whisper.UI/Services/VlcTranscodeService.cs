﻿// <copyright file="VlcTranscodeService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using LibVLCSharp.Shared;
using System.Text.RegularExpressions;

namespace Drastic.Whisper.UI.Services
{
    public class VlcTranscodeService : ITranscodeService
    {
        private string basePath;
        private string? generatedFilename;

        private LibVLC libVLC;

        public VlcTranscodeService(string? basePath = default, string? generatedFilename = default)
        {
            this.basePath = basePath ?? Path.GetTempPath();
            this.libVLC = new LibVLC();
            this.generatedFilename = generatedFilename;
        }

        public string BasePath => this.basePath;

        public async Task<string> ProcessFile(string file)
        {
            using var mediaPlayer = new MediaPlayer(this.libVLC);
            var processingCancellationTokenSource = new CancellationTokenSource();
            var outputfile = Path.Combine(this.basePath, $"{this.generatedFilename ?? Path.GetRandomFileName()}.wav");
            mediaPlayer.Stopped += (s, e) => processingCancellationTokenSource.CancelAfter(1);

            Media media;

            if (IsUrl(file))
            {
                media = new Media(this.libVLC, file, FromType.FromLocation);
            }
            else
            {
                media = new Media(this.libVLC, file, FromType.FromPath);
            }

            media.AddOption(":no-video");
            media.AddOption(":sout=#transcode{acodec=s16l,channels=1,samplerate=16000}:file{dst='" + outputfile + "'");
            var testing = media.Parse();
            mediaPlayer.Play(media);

            while (!processingCancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), processingCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }

            return outputfile;
        }

        private static bool IsUrl(string str)
        {
            // URL pattern
            string urlPattern = @"^(https?|ftp)://[^\s/$.?#].[^\s]*$";
            // Check if the string matches the URL pattern
            return Regex.IsMatch(str, urlPattern);
        }
    }
}