namespace SWLOR.CombatSim.Cli
{
    /// <summary>
    /// Writes an output through a temporary file moved into place, so a path that is a symbolic or hard link to another
    /// file (such as the scenario) becomes a file of its own instead of overwriting the one it pointed at.
    /// </summary>
    public static class OutputFile
    {
        public static void WriteAllText(string path, string contents)
        {
            var fullPath = Path.GetFullPath(path);
            var temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporary, contents);
            try
            {
                File.Move(temporary, fullPath, overwrite: true);
            }
            catch
            {
                File.Delete(temporary);
                throw;
            }
        }
    }
}
