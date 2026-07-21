using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>Where a decoded minimap texture was found (or that it wasn't).</summary>
    internal enum MinimapImageSource
    {
        Loose,
        BaseGameArchive,
        Missing
    }
}
