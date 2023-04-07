using System.Text.Json;

namespace h20.Cli
{
	internal class Settings
	{
		private static string _settingsPath = "settings.json";
		private static JsonSerializerOptions _options;

		public string? TempPath { get; set; }
		public string? SourcePath { get; set; }
		public string? OutputPath { get; set; }
		public List<string> Xpaths { get; set; }

        public Settings()
        {
			Xpaths = new();
        }

        static Settings()
        {
			_options = new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = true,
			};
        }

        public static Settings LoadSettings()
		{
			Settings settings;

			if (!File.Exists(_settingsPath))
				return new();

			settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath), _options) ?? new();

			return settings;
		}
    }
}
