using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.ContentBuilder.Services;

namespace SWLOR.ContentBuilder.Rendering
{
    /// <summary>Tally of where each tile's minimap art came from, for the status bar.</summary>
    internal sealed class MapRenderStats
    {
        public int LooseHits { get; set; }
        public int ArchiveHits { get; set; }
        public int Misses { get; set; }
        public string BaseGameArchiveStatus { get; set; }
    }
}
