using HtmlAgilityPack;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ffnet.Cli
{
    internal class Program
    {
        private bool _isDone = false;
        private readonly string _startInput = "s";
        private readonly string _sourceInput = "o";
        private readonly string _outputInput = "t";
        private readonly string _terminatingInput = "q";
        private readonly string _sourcePath = "sources";
        private readonly string _outputPath = "output";
        private readonly string _templatePath = "template.xhtml";

        private HtmlDocument? _doc;
        private Dictionary<string, string> _chapterTitlesMap;

        static async Task Main()
        {
            var app = new Program();
            await app.RunAsync();
        }
        public Program()
        {
            _chapterTitlesMap = new();
        }

        private async Task RunAsync()
        {
            while (!_isDone)
            {
                Console.WriteLine($"{_startInput}:\tStart");
                Console.WriteLine($"{_sourceInput}:\tOpen source");
                Console.WriteLine($"{_outputInput}:\tOpen output");
                Console.WriteLine($"{_terminatingInput}:\tExit");

                // Get input
                var input = GetInput() ?? "";
                if (string.IsNullOrEmpty(input) || input?.ToLower() == _terminatingInput)
                    _isDone = true;
                else if (input?.ToLower() == _startInput)
                {
                    await ProcessAsync();
                    GenerateEntries();
                }
                else if (input?.ToLower() == _sourceInput)
                {
                    Console.WriteLine("Opening sources...");
                    Process.Start("explorer.exe", _sourcePath);
                }
                else if (input?.ToLower() == _outputInput)
                {
                    Console.WriteLine("Opening output...");
                    Process.Start("explorer.exe", _outputPath);
                }
            }
        }

        private string? GetInput()
        {
            Console.Write("Input:\t");
            return Console.ReadLine();
        }

        private async Task ProcessAsync()
        {
            Console.WriteLine();

            // Check if source directory exists
            if (!Directory.Exists(_sourcePath))
                return;

            // Load source files
            var files = new DirectoryInfo(_sourcePath).EnumerateFiles("*.html");
            foreach (var file in files)
            {
                // Load source
                _doc = new HtmlDocument();
                await Task.Run(() => _doc.LoadHtml(File.ReadAllText(file.FullName)));

                // Get info
                var title = _doc.DocumentNode.SelectSingleNode("//*[@id=\"profile_top\"]/b").InnerText;
                title = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

                // Get body
                var body = "";
                var bodyNode = _doc.DocumentNode.SelectSingleNode("//*[@id=\"storytext\"]")?.ChildNodes.ToList();
                foreach (var node in bodyNode!)
                    body += (node.WriteTo() + "\n");
                body = body.Trim()
                    .Replace("<hr>", "<hr />")
                    .Replace("<hr size=\"1\" noshade=\"\">", "<hr size=\"1\" noshade=\"\" />")
                    .Replace("<br>", "<br />");

                var selected = _doc.DocumentNode.SelectSingleNode("//*[@id=\"chap_select\"]/option[@selected]");
                var chapterNo = selected?.GetAttributeValue("value", 0);
                var chapterTitle = "";

                if (selected is not null)
                {
                    var match = Regex.Match(selected.InnerText, ".+\\. (?<title>.+)");
                    chapterTitle = match?.Groups["title"].Value ?? "";
                }

                // Load template
                var template = File.ReadAllText(_templatePath);
                template = template
                    .Replace("{{CHAPTITLE}}", chapterTitle)
                    .Replace("{{CHAPNO}}", chapterNo.ToString())
                    .Replace("{{BODY}}", body);

                // Write file
                var path = Path.Combine(_outputPath, title);
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, $"{chapterNo}.xhtml"), template);

                // Map chapter title
                _chapterTitlesMap.Add($"{title}-{chapterNo}", chapterTitle);
            }
        }

        private void GenerateEntries()
        {
            var dirs = new DirectoryInfo(_outputPath).EnumerateDirectories();
            foreach (var dir in dirs)
            {
                var body = "";
                var contents = new List<string>();
                var spine = new List<string>();
                var toc = new List<string>();

                var files = dir.EnumerateFiles("*.xhtml");
                foreach (var file in files)
                {
                    var num = int.Parse(file.Name.Replace(file.Extension, ""));
                    var id = $"chap{num}";
                    var chapterTitle = _chapterTitlesMap[$"{dir.Name}-{num}"];

                    contents.Add($"<item id=\"{id}\" href=\"Content/{file.Name}\" media-type=\"application/xhtml+xml\" />");
                    spine.Add($"<itemref idref=\"{id}\" />");
                    toc.Add($"<navPoint id=\"navPoint-{num + 1}\" playOrder=\"{num + 1}\"><navLabel><text>{num}. {chapterTitle}</text></navLabel><content src=\"Content/{file.Name}\"/></navPoint>");
                }

                body += $"{string.Join('\n', contents)}\n";
                body += $"\n{string.Join('\n', spine)}\n";
                body += $"\n{string.Join('\n', toc)}\n";

                File.WriteAllText(Path.Combine(dir.FullName, "meta.txt"), body);
            }
        }
    }
}