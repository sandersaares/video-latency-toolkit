using Koek;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Vltk.Generator.Gui
{
    /// <summary>
    /// We remember some user choices so we can easily restore the values on next execution.
    /// </summary>
    public sealed class GeneratorSettings
    {
        public static GeneratorSettings Current { get; }

        /// <summary>
        /// URL of the timeserver that provides the clock synchronization signal.
        /// </summary>
        public string? TimeserverUrl { get; set; } = "ntp://time.windows.com";

        public void Save()
        {
            try
            {
                var folder = Path.GetDirectoryName(SettingsFilePath)!;
                Directory.CreateDirectory(folder);

                File.WriteAllBytes(SettingsFilePath, JsonSerializer.SerializeToUtf8Bytes(this));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to asve settings file: {ex.Message}");
            }
        }

        private static GeneratorSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new GeneratorSettings();

                var folder = Path.GetDirectoryName(SettingsFilePath)!;
                Directory.CreateDirectory(folder);

                return JsonSerializer.Deserialize<GeneratorSettings>(File.ReadAllText(SettingsFilePath)) ?? new GeneratorSettings();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to read saved settings file: {ex.Message}");

                return new GeneratorSettings();
            }
        }

        static GeneratorSettings()
        {
            Current = Load();
        }

        private static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "video-latency-toolkit", "settings.json");
    }
}
