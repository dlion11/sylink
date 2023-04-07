using HtmlAgilityPack;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace h20.Cli
{
	[SupportedOSPlatform("windows")]
	internal class Program
	{
		private static Settings? _settings;

		static async Task Main(string[] args)
		{
			// Load settings
			_settings = Settings.LoadSettings();

			if (!ConfirmContinue())
			{
				Console.WriteLine("Done.");
				Console.Write("Press any key to continue. ");
				Console.ReadKey();
				return;
			}

			await Process();
		}

		static async Task Process()
		{
			// Check if settings valid
			if (_settings is null
				|| _settings?.TempPath is null
				|| _settings?.SourcePath is null)
			{
				Console.Error.WriteLine("error: Invalid settings.");
				return;
			}

			// Create temp diretory if it does not exist
			Directory.CreateDirectory(_settings!.TempPath);

			// Check if source path exists
			var sourcePath = Path.Combine(_settings!.TempPath, _settings!.SourcePath);
			if (!Directory.Exists(sourcePath))
			{
				Directory.CreateDirectory(sourcePath);
				return;
			}

			// Process each file
			var files = new DirectoryInfo(sourcePath).EnumerateFiles();
			foreach (var file in files)
			{
				Console.Write($"Processing '{file.Name}'... ");

				// Get information
				var match = Regex.Match(file.Name, "(?<title>.+) - [cC]hapter (?<chapter>\\d+\\.?-?\\d+)");
				if (!match.Success)
					continue;
				var title = match.Groups["title"].Value.Trim();
				var chapter = match.Groups["chapter"].Value.Trim();

				// Read html file
				var html = File.ReadAllText(file.FullName);
				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				// Get image urls
				var imageUrls = new List<string>();
				foreach (var xpath in _settings.Xpaths)
				{
					imageUrls = doc.DocumentNode.SelectNodes(xpath)?
					.Select(x => x.GetAttributeValue<string>("src", "").Trim())
					.ToList();

					if (imageUrls is not null)
						break;
					continue;
				}
				Console.WriteLine($"Found {imageUrls.Count} images.");

				var outputPath = Path.Combine(_settings!.TempPath, _settings!.OutputPath ?? "output", title);
				var outputFile = Path.Combine(outputPath, $"[{chapter.PadLeft(4, '0')}] Chapter {chapter.PadLeft(4, '0')}.zip");
				Directory.CreateDirectory(outputPath);

				using (var zipStream = new MemoryStream())
				{
					// Create archive stream
					using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
					{
						foreach (var url in imageUrls)
						{
							Console.WriteLine($"Downloading image {imageUrls.IndexOf(url) + 1}..");

							// Create image entry
							var entry = archive.CreateEntry($"{(imageUrls.IndexOf(url) + 1).ToString().PadLeft(3, '0')}.jpg");

							// Download image
							using (var entryStream = entry.Open())
							using (var client = new HttpClient())
							{
								var res = await client.GetAsync(url);
								if (res is null)
									continue;

								var raw = await res.Content.ReadAsStreamAsync();
								var img = Image.FromStream(raw);

								// Write image to entry
								img.Save(entryStream, ImageFormat.Jpeg);
							}
						}
					}

					// Write zip to file
					using (var fileStream = new FileStream(outputFile, FileMode.Create))
					{
						zipStream.Seek(0, SeekOrigin.Begin);
						zipStream.CopyTo(fileStream);
					}

					zipStream?.Dispose();
				}

				Console.WriteLine("Done.");
			}
		}

		static bool ConfirmContinue()
		{
			Console.Write("Do you want to contnue? (y/n)\t");
			var ans = Console.ReadLine();

			if (ans == "y")
				return true;
			return false;
		}
	}
}