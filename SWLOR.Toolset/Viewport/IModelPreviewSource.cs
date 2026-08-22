using System.ComponentModel;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>The small surface the reusable one-model viewport observes.</summary>
    public interface IModelPreviewSource : INotifyPropertyChanged
    {
        AreaScene? PreviewScene { get; }

        ResourceIndex? ResourceIndex { get; }

        string? PreviewAnimationName { get; }

        bool IsAnimationPlaying { get; }

        /// <summary>Rebuilds the preview from the newly active module HAK stack.</summary>
        void ReloadGameResources();
    }
}
