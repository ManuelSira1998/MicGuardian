using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MicGuardian.Configuration
{
    public class GuardianConfig
    {
        public int TargetVolumePercent { get; set; }
        public int MinVolumeThresholdPercent { get; set; }
        public int PollIntervalMilliseconds { get; set; }
        public bool EnableAutoUnmute { get; set; }
        public bool EnableVolumeEnforcement { get; set; }
        public bool EnableLogging { get; set; }

        public GuardianConfig()
        {
            // Valores predeterminados óptimos
            TargetVolumePercent = 90;
            MinVolumeThresholdPercent = 60;
            PollIntervalMilliseconds = 1000;
            EnableAutoUnmute = true;
            EnableVolumeEnforcement = true;
            EnableLogging = true;
        }

        public float TargetVolumeScalar
        {
            get { return Math.Max(0.0f, Math.Min(1.0f, TargetVolumePercent / 100.0f)); }
        }

        public float MinVolumeThresholdScalar
        {
            get { return Math.Max(0.0f, Math.Min(1.0f, MinVolumeThresholdPercent / 100.0f)); }
        }
    }

    public static class ConfigManager
    {
        private static string GetConfigFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "MicGuardian", "config.json");
        }

        public static GuardianConfig Load()
        {
            var config = new GuardianConfig();
            string path = GetConfigFilePath();

            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    config.TargetVolumePercent = ParseInt(json, "TargetVolumePercent", 90);
                    config.MinVolumeThresholdPercent = ParseInt(json, "MinVolumeThresholdPercent", 60);
                    config.PollIntervalMilliseconds = ParseInt(json, "PollIntervalMilliseconds", 1000);
                    config.EnableAutoUnmute = ParseBool(json, "EnableAutoUnmute", true);
                    config.EnableVolumeEnforcement = ParseBool(json, "EnableVolumeEnforcement", true);
                    config.EnableLogging = ParseBool(json, "EnableLogging", true);
                    return config;
                }
            }
            catch { }

            // Si no existe, guardar configuración predeterminada para que el usuario pueda personalizarla
            Save(config);
            return config;
        }

        public static void Save(GuardianConfig config)
        {
            try
            {
                string path = GetConfigFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = string.Format(
                    "{{\r\n" +
                    "  \"TargetVolumePercent\": {0},\r\n" +
                    "  \"MinVolumeThresholdPercent\": {1},\r\n" +
                    "  \"PollIntervalMilliseconds\": {2},\r\n" +
                    "  \"EnableAutoUnmute\": {3},\r\n" +
                    "  \"EnableVolumeEnforcement\": {4},\r\n" +
                    "  \"EnableLogging\": {5}\r\n" +
                    "}}",
                    config.TargetVolumePercent,
                    config.MinVolumeThresholdPercent,
                    config.PollIntervalMilliseconds,
                    config.EnableAutoUnmute.ToString().ToLower(),
                    config.EnableVolumeEnforcement.ToString().ToLower(),
                    config.EnableLogging.ToString().ToLower()
                );

                File.WriteAllText(path, json);
            }
            catch { }
        }

        private static int ParseInt(string json, string key, int defaultValue)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int val;
                if (int.TryParse(match.Groups[1].Value, out val)) return val;
            }
            return defaultValue;
        }

        private static bool ParseBool(string json, string key, bool defaultValue)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
            }
            return defaultValue;
        }
    }
}
