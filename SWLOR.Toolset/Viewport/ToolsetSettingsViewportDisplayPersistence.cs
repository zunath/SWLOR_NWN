using Nwn.Toolset.Avalonia.Areas;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// Stores the shared area viewport display switches in <see cref="ToolsetSettings"/>, so the
    /// builder's choice survives a restart.
    /// </summary>
    public sealed class ToolsetSettingsViewportDisplayPersistence : IAreaViewportDisplayPersistence
    {
        private readonly ToolsetSettings _settings;

        public ToolsetSettingsViewportDisplayPersistence(ToolsetSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public AreaViewportDisplaySettings Load() => new(
            _settings.ShowAreaLighting,
            _settings.ShowFog,
            _settings.ShowCeilings,
            _settings.ShowMaterialMaps);

        public void Save(AreaViewportDisplaySettings settings)
        {
            _settings.ShowAreaLighting = settings.ShowAreaLighting;
            _settings.ShowFog = settings.ShowFog;
            _settings.ShowCeilings = settings.ShowCeilings;
            _settings.ShowMaterialMaps = settings.ShowMaterialMaps;
        }
    }
}
