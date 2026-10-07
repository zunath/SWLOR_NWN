namespace SWLOR.Toolset.Tests
{
    internal static class ScratchDirectory
    {
        /// <summary>
        /// Deletes a scratch module for tests that start a workspace they do not own the lifetime
        /// of. <c>WorkspaceContext.Open</c> leaves catalog and index scans running in the
        /// background; they read module files with a share mode that blocks deletion, so the first
        /// attempt can race a scan. The scans are bounded, so retry briefly and then surface the
        /// real error if a handle is genuinely leaked.
        /// </summary>
        public static void Delete(string path)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (true)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                    return;
                }
                catch (IOException) when (DateTime.UtcNow < deadline)
                {
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException) when (DateTime.UtcNow < deadline)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
