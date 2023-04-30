// <copyright file="LogErrorHandlerService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System;
using System.IO;

namespace Drastic.Whisper.Services
{
    public class LogErrorHandlerService : Drastic.Services.IErrorHandlerService
    {
        private bool disposedValue;
        private StreamWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogErrorHandlerService"/> class.
        /// </summary>
        /// <param name="logLocation">Location of logs.</param>
        public LogErrorHandlerService(string logLocation)
        {
            this.writer = new StreamWriter(logLocation);
            this.writer.AutoFlush = true;
        }

        /// <inheritdoc/>
        public void HandleError(Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
            this.writer.WriteLine($"({DateTime.UtcNow}): {ex}");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Is Disposing.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.writer.Close();
                    this.writer.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}