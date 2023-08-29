using System.CommandLine;

namespace sylink.Cli
{
    internal class Program
    {
        // Commands
        static RootCommand _rootCommand = new();
        static Argument<FileInfo?> _fileArg = new("--file");
        static Option<bool> _overwriteOpt = new("--overwrite");

        static List<SymLink> FilesToProcess;
        static List<SymLink> FilesToOverwrite;
        static List<SymLink> FilesToSkip;

        static Program()
        {
            // Initialize commands, arguments, and options
            InitializeCommands();
            InitializeArguments();
            InitializeOptions();

            FilesToProcess = new();
            FilesToOverwrite = new();
            FilesToSkip = new();
        }

        static void InitializeCommands()
        {
            _rootCommand.Description = "Utility to batch create symbolic links listed from a file.";
        }

        static void InitializeArguments()
        {
            _fileArg.Description = "The file to read the list from.";
            _fileArg.SetDefaultValue(new FileInfo("list.txt"));

            _rootCommand?.AddArgument(_fileArg);
        }

        static void InitializeOptions()
        {
            _overwriteOpt.Description = "Flag to overwrite destination file if exists";
            _overwriteOpt.AddAlias("-o");
            _overwriteOpt.SetDefaultValue(false);

            _rootCommand.AddOption(_overwriteOpt);
        }

        static async Task<int> Main(string[] args)
        {
            if (_rootCommand is null)
                return await Task.FromResult(1);

            // Handle root command
            _rootCommand.SetHandler((file, forceOverwrite) =>
            {
                try
                {
                    Analyze(file, forceOverwrite);
                    ShowFiles();

                    if (FilesToProcess.Any() || FilesToOverwrite.Any())
                    {
                        Console.Write("\nDo you want to continue? (y/N): ");
                        var input = Console.ReadLine();

                        if (input?.ToLower() == "y")
                            Process();
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }, _fileArg, _overwriteOpt);

#if DEBUG
            //args = new[] { "--version" };
#endif
            return await _rootCommand.InvokeAsync(args);
        }

        static void Analyze(FileInfo? file, bool forceOverwrite)
        {
            // Check if file exists
            if (file is null)
                throw new FileNotFoundException();

            // Get lines
            var lines = File.ReadAllLines(file.FullName).Select(x => x.Split('\t')).ToArray();

            Console.Write("Analyzing... ");
            foreach (var line in lines)
            {
                // Check if lines have 2 elements
                if (line.Length != 2)
                    continue;

                // Get source and destination file info
                var source = new FileInfo(line[0]);
                var destination = new FileInfo(line[1]);

                var symlink = new SymLink(source, destination);
                if (!symlink.HasSource || (symlink.Destination.Exists && !forceOverwrite))
                    FilesToSkip.Add(symlink);
                else if (symlink.Destination.Exists && forceOverwrite)
                {
                    // Check if existing file is a real file or link
                    if (symlink.Destination.LinkTarget is null)
                        FilesToSkip.Add(symlink);
                    else
                        FilesToOverwrite.Add(symlink);
                }
                else
                    FilesToProcess.Add(symlink);
            }
            Console.WriteLine("Done.");
        }

        static void ShowFiles()
        {
            ShowFiles("\n[Files to process]", FilesToProcess);
            ShowFiles("\n[Files to overwrite]", FilesToOverwrite);
            ShowFiles("\n[Files to skip]", FilesToSkip);
        }

        static void ShowFiles(string message, IEnumerable<SymLink> collection)
        {
            if (collection.Any())
            {
                Console.WriteLine(message);
                Console.WriteLine(string.Join("\n\n", collection.Select(x => x.ToString())));
            }
        }

        static void Process()
        {
            // Delete existing destination files
            foreach (var file in FilesToOverwrite)
                file.Destination.Delete();

            // Process files
            Console.WriteLine();
            Process(FilesToProcess);
            Process(FilesToOverwrite);
        }

        static void Process(IEnumerable<SymLink> collection)
        {
            foreach (var file in collection)
            {
                Console.WriteLine($"{file}");
                File.CreateSymbolicLink(file.Destination.FullName, file.Source!.FullName);
                Console.WriteLine("Done.");
            }
        }
    }
}