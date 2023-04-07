namespace ffnet.Cli
{
    internal class Program
    {
        private bool _isDone = false;
        private static readonly string _terminatingInput = "q";

        static async Task Main(string[] args)
        {
            var app = new Program();
            await app.RunAsync();
        }

        public Program()
        {

        }

        private async Task RunAsync()
        {
            while (!_isDone)
            {
                // Get input
                var input = GetInput() ?? "";
                if (input?.ToLower() == _terminatingInput)
                    _isDone = true;
            }
        }

        private string? GetInput()
        {
            Console.Write("Input:\t");
            return Console.ReadLine();
        }
    }
}