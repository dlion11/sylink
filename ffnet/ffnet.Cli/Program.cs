using HtmlAgilityPack;
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

        static async Task Main()
        {
            var app = new Program();
            await app.RunAsync();
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
                if (input?.ToLower() == _terminatingInput)
                    _isDone = true;
                else if (input?.ToLower() == _startInput)
                    await ProcessAsync();
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
                var body = _doc.DocumentNode.SelectSingleNode("//*[@id=\"storytext\"]")?.InnerHtml
                    .Replace("<hr>", "<hr />")
                    .Replace("<br>", "<br />");

                var selected = _doc.DocumentNode.SelectSingleNode("//*[@id=\"chap_select\"]/option[@selected]");
                var chapterNo = selected?.GetAttributeValue("value", 0);
                var chapterTitle = "";

                if (selected is not null)
                {
                    var match = Regex.Match(selected.InnerText, ".+\\. (?<title>.+)");
                    chapterTitle = match?.Groups["title"].Value;
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
            }

        }
    }
}