namespace sylink.Cli
{
    public class SymLink
    {
        public FileInfo? Source { get; set; }
        public FileInfo Destination { get; set; }

        public bool HasSource { get => Source is not null && Source.Exists; }

        public SymLink(FileInfo? source, FileInfo destination)
        {
            Source = source;
            Destination = destination;
        }

        public override string ToString()
        {
            return $"[{Source?.Name}]\nsrc:\t{Source?.FullName}\ndest:\t{Destination.FullName}";
        }
    }
}
