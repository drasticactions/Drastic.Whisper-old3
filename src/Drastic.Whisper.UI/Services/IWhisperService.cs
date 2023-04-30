﻿// <copyright file="IWhisperService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System;
using Drastic.Whisper.Models;

namespace Drastic.Whisper.Services
{
    public interface IWhisperService
    {
        event EventHandler<OnNewSegmentEventArgs>? OnNewWhisperSegment;

        bool IsInitialized { get; }

        bool IsIndeterminate { get; }

        Task ProcessAsync(string filePath, CancellationToken? cancellationToken = default);

        Task ProcessAsync(byte[] buffer, CancellationToken? cancellationToken = default);

        Task ProcessAsync(Stream stream, CancellationToken? cancellationToken = default);

        void InitModel(string path, WhisperLanguage lang);

        void InitModel(byte[] buffer, WhisperLanguage lang);

        public Action<double>? OnProgress { get; set; }
    }

    public class OnNewSegmentEventArgs : EventArgs
    {
        public OnNewSegmentEventArgs(WhisperSegmentData segmentData)
        {
            this.Segment = segmentData;
        }

        public WhisperSegmentData Segment { get; }
    }

    public class OnCaptureStatusChangedEventArgs : EventArgs
    {
        public OnCaptureStatusChangedEventArgs(CaptureStatus status)
        {
            this.CaptureStatus = status;
        }

        public CaptureStatus CaptureStatus { get; }
    }
}
