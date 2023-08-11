using System.CommandLine;

namespace sylink.Cli
{
    internal class Program
    {
        // Commands
        static RootCommand _rootCommand = new();
        static Argument<FileInfo?> _fileArg = new("--file");

        static Program()
        {
            // Initialize commands, arguments, and options
            InitializeCommands();
            InitializeArguments();
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

        static async Task<int> Main(string[] args)
        {
            if (_rootCommand is null)
                return await Task.FromResult(1);

            // Handle root command
            _rootCommand.SetHandler((file) =>
            {
                try
                {
                    Process(file);
                }
                catch (FileNotFoundException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }, _fileArg);

#if DEBUG
            //args = new[] { "--version" };
#endif
            return await _rootCommand.InvokeAsync(args);
        }

        static void Process(FileInfo? file)
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

                // Check if destination file already exists
                if (destination.Exists)
                {
                    Console.Error.Write("Destination file already exist. SKIPPED.");
                    continue;
                }

                // Create symlinks
                File.CreateSymbolicLink(destination.FullName, source.FullName);
                Console.WriteLine("Done.");
            }
        }
    }
}