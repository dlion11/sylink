using System.CommandLine;

namespace sylink.Cli
{
    internal class Program
    {
        // Commands
        static RootCommand? _rootCommand;

        static Program()
        {
            // Initialize commands, arguments, and options
            InitializeCommands();
        }

        static async Task<int> Main(string[] args)
        {
            if (_rootCommand is null)
                return await Task.FromResult(1);

            // Handle root command
            _rootCommand.SetHandler(() =>
            {

            });

#if DEBUG
            //args = new[] { "--version" };
#endif
            return await _rootCommand.InvokeAsync(args);
        }

        static void InitializeCommands()
        {
            _rootCommand = new RootCommand("Utility to batch create symbolic links listed from a file.");
        }
    }
}