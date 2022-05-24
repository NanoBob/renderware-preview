using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RenderWarePreviewer.Helpers
{
    public static class GtaDirectoryHelper
    {
        private static readonly string configDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RenderWarePreviewer");
        private static readonly string gtaPathFile = Path.Join(configDirectory, "gtapath.txt");

        public static string? GetGtaDirectory()
        {
            string? gtaDirectory = GetGtaDirectoryFromFiles();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && gtaDirectory == null)
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Multi Theft Auto: San Andreas All\Common");
                gtaDirectory = key?.GetValue("GTA:SA Path")?.ToString();
            }

            return gtaDirectory;
        }

        private static string? GetGtaDirectoryFromFiles()
        {
            if (File.Exists(gtaPathFile))
                return File.ReadAllText(gtaPathFile);

            return null;
        }

        public static void StoreGtaDirectory(string path)
        {
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            File.WriteAllText(gtaPathFile, path);
        }
    }
}
