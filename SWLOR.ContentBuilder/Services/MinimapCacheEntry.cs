using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    internal sealed class MinimapCacheEntry
    {
        public BitmapSource Image { get; init; }
        public MinimapImageSource Source { get; init; }
    }
}
