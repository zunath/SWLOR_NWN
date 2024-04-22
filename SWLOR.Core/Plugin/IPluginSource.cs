namespace SWLOR.Core.Plugin
{
    internal interface IPluginSource
    {
        IEnumerable<Plugin> Bootstrap();
    }
}
