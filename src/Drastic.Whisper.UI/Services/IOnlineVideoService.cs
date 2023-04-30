using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drastic.Whisper.UI.Services
{
    public interface IOnlineVideoService
    {
        bool IsValidUrl (string url);

        Task<string> GetAudioUrlAsync(string url);
    }
}
