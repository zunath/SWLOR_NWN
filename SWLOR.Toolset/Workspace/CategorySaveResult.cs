namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Whether a category sidecar write happened, and why not when it did not.
    /// </summary>
    /// <remarks>
    /// A result rather than a silent log line, so a command that reports "renamed" has actually renamed
    /// something on disk. The sidecar can legitimately refuse a write - it is read-only when a newer
    /// Toolset produced it, and it declines to clobber an external edit - and a caller that cannot tell
    /// the difference tells the builder their arrangement is saved when it is not.
    /// </remarks>
    public readonly record struct CategorySaveResult(
        bool Saved,
        string? Problem,
        string? ContentSha256)
    {
        public static CategorySaveResult Ok(string? contentSha256 = null) =>
            new(true, null, contentSha256);

        public static CategorySaveResult Failed(string problem) =>
            new(false, problem, null);
    }
}
