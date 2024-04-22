namespace SWLOR.Core.Plugin
{
    /// <summary>
    /// Additional Plugin Metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class PluginInfoAttribute : Attribute
    {
        /// <summary>
        /// A list of optional plugin dependencies.
        /// </summary>
        /// <remarks>
        /// By default, an exception will be thrown at startup if a plugin references a type from another plugin, and the plugin is not loaded.<br/>
        /// Adding a plugin name to this list will instead cause these types to be skipped if the plugin is not loaded.<br/>
        /// </remarks>
        public string[] OptionalDependencies { get; init; } = Array.Empty<string>();
    }
}
