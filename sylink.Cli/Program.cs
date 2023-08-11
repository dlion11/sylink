using System.CommandLine;

namespace sylink.Cli
{
    internal class Program
    {
        // Commands
        static RootCommand _rootCommand = new();
        static Argument<FileInfo?> _fileArg = new("--file");
        static Option<bool> _overwriteOpt = new("--overwrite");

        static Program()
        {
            // Initialize commands, arguments, and options
            InitializeCommands();
            InitializeArguments();
            InitializeOptions();
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
                    Process(file, forceOverwrite);
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

        static void Process(FileInfo? file, bool forceOverwrite)
        {
            // Check if file exists
            if (file is null)
                throw new FileNotFoundException();

            // Get lines
            var lines = File.ReadAllLines(file.FullName).Select(x => x.Split('\t')).ToArray();
            foreach (var line in lines)
            {
                // Check if lines have 2 elements
                if (line.Length != 2)
                    continue;

                // Get source and destination file info
                var source = new FileInfo(line[0]);
                var destination = new FileInfo(line[1]);
                
                // Check if source file exists
                Console.Write($"[{source.Name}] Creating '{destination.FullName}'... ");
                if (source is null || !source.Exists)
                {
                    Console.Error.Write("Source file does not exist. SKIPPED.");
                    continue;
                }

                // Check if destination file already exists and not forcing overwrite
                if (destination.Exists && !forceOverwrite)
                {
                    Console.Error.Write("Destination file already exist. SKIPPED.");
                    continue;
                } else if (destination.Exists && forceOverwrite)
                    destination.Delete();

                // Create symlinks
                File.CreateSymbolicLink(destination.FullName, source.FullName);
                Console.WriteLine("Done.");
            }
        }
    }
}