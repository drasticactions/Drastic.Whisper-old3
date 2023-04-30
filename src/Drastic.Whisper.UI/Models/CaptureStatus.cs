// <copyright file="CaptureStatus.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Whisper.UI;

namespace Drastic.Whisper.Models
{
    public enum CaptureStatus
    {
        Listening = 1,
        Voice = 2,
        Transcribing = 4,
        Stalled = 0x80,
    }

    public static class CaptureStatusExtensions
    {
        public static string ToTranslatedString(this CaptureStatus status)
        {
            switch (status)
            {
                case CaptureStatus.Listening:
                    return Drastic.Whisper.UI.Translations.Common.Listening;
                case CaptureStatus.Voice:
                    return string.Empty;
                case CaptureStatus.Transcribing:
                    return Drastic.Whisper.UI.Translations.Common.Transcribing;
                case CaptureStatus.Stalled:
                    return Drastic.Whisper.UI.Translations.Common.Stalled;
                default:
                    return string.Empty;
            }
        }
    }
}
