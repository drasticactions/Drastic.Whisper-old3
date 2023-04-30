using System;
using System.Reflection;

namespace Drastic.WhisperSample
{
    public static class Samples
    {
        public static byte[] LoadJFK()
        {
            var resource = GetResourceFileContent("Samples.jfk.wav")!;
            using MemoryStream ms = new MemoryStream();
            resource.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// Get Resource File Content via FileName.
        /// </summary>
        /// <param name="fileName">Filename.</param>
        /// <returns>Stream.</returns>
        public static Stream? GetResourceFileContent(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Drastic.WhisperSample." + fileName;
            if (assembly is null)
            {
                return null;
            }

            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}

