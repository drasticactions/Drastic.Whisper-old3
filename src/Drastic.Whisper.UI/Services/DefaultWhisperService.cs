﻿// <copyright file="DefaultWhisperService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Whisper.net;
using Drastic.Whisper.Models;

namespace Drastic.Whisper.Services
{
    public class DefaultWhisperService : IWhisperService, IDisposable
    {
        private bool disposedValue;
        private WhisperFactory? factory;
        private WhisperProcessor? processor;
        const int audioSampleLengthS = 1;
        const int audioSampleLengthMs = audioSampleLengthS * 1000;
        const int totalBufferLength = 30 / audioSampleLengthS;
        List<float[]> slidingBuffer = new(totalBufferLength + 1);

        public event EventHandler<OnNewSegmentEventArgs>? OnNewWhisperSegment;

        public bool IsInitialized => this.processor is not null;

        public bool IsIndeterminate => true;

        public Action<double>? OnProgress { get; set; }

        void IDisposable.Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void InitModel(string path, WhisperLanguage language)
        {
            this.factory = WhisperFactory.FromPath(path);
            this.processor = this.SetupProcessor(this.factory, language);
        }

        public void InitModel(byte[] buffer, WhisperLanguage language)
        {
            this.factory = WhisperFactory.FromBuffer(buffer);
            this.processor = this.SetupProcessor(this.factory, language);
        }

        private WhisperProcessor SetupProcessor(WhisperFactory factory, WhisperLanguage language)
        {
            int max_threads = Math.Min(8, Environment.ProcessorCount);
            var builder = factory.CreateBuilder();
            if (language.IsAutomatic)
            {
                builder.WithLanguage("auto");
            }
            else
            {
                builder.WithLanguage(language.LanguageCode);
            }
            return builder.WithSegmentEventHandler(this.OnNewSegment).
                WithThreads(max_threads)
                .Build();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.factory?.Dispose();
                    this.processor?.Dispose();
                }

                this.disposedValue = true;
            }
        }

        private void OnNewSegment(SegmentData e)
        {
            this.OnNewWhisperSegment?.Invoke(this, new OnNewSegmentEventArgs(new Models.WhisperSegmentData(e.Text, e.Start, e.End, e.MinProbability, e.MaxProbability, e.Probability, e.Language)));
        }

        public Task ProcessAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            ArgumentNullException.ThrowIfNull(this.processor);

            return Task.Run(
                () =>
                {
                    using var fileStream = File.OpenRead(filePath);
                    this.processor.Process(fileStream);
                },
                cancellationToken ?? CancellationToken.None);
        }

        public Task ProcessAsync(byte[] buffer, CancellationToken? cancellationToken = null)
        {
            return this.ProcessAsync(new MemoryStream(buffer), cancellationToken);
        }

        public Task ProcessAsync(Stream stream, CancellationToken? cancellationToken = null)
        {
            ArgumentNullException.ThrowIfNull(this.processor);

            return Task.Run(
                () =>
                {
                    this.processor.Process(stream);
                },
                cancellationToken ?? CancellationToken.None);
        }

        public Task ProcessBytes(byte[] e, CancellationToken? cancellationToken = null)
        {
            ArgumentNullException.ThrowIfNull(this.processor);

            var values = new short[e.Length / 2];
            Buffer.BlockCopy(e, 0, values, 0, e.Length);
            var samples = values.Select(x => x / (short.MaxValue + 1f)).ToArray();

            var silenceCount = samples.Count(x => IsSilence(x, -40));

            if (silenceCount < values.Length - values.Length / 12)
            {
                slidingBuffer.Add(samples);

                if (slidingBuffer.Count > totalBufferLength)
                {
                    slidingBuffer.RemoveAt(0);
                }

                processor.Process(slidingBuffer.SelectMany(x => x).ToArray());
            }

            return Task.CompletedTask;
        }

        static bool IsSilence(float amplitude, sbyte threshold)
            => GetDecibelsFromAmplitude(amplitude) < threshold;

        static double GetDecibelsFromAmplitude(float amplitude) => 20 * Math.Log10(Math.Abs(amplitude));
    }
}
