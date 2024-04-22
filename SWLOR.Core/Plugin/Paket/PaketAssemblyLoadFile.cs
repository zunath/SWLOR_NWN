namespace SWLOR.Core.Plugin.Paket
{
    internal sealed class PaketAssemblyLoadFile
    {
        private const string LoadPrefix = "#r ";

        private readonly string rootPath;

        public PaketAssemblyLoadFile(string path)
        {
            rootPath = Path.GetFullPath(Path.GetDirectoryName(path)!);

            using StreamReader streamReader = File.OpenText(path);
            while (!streamReader.EndOfStream)
            {
                string? line = streamReader.ReadLine();
                if (line?.StartsWith("#r") == true)
                {
                    ProcessLine(line);
                }
            }
        }

        internal Dictionary<string, string> AssemblyPaths { get; } = new Dictionary<string, string>();

        private void ProcessLine(string line)
        {
            string relativeAssemblyPath = line[LoadPrefix.Length..].Trim().Trim('"');
            string fullAssemblyPath = Path.GetFullPath(relativeAssemblyPath, rootPath);

            AssemblyPaths[Path.GetFileNameWithoutExtension(fullAssemblyPath)] = fullAssemblyPath;
        }
    }
}