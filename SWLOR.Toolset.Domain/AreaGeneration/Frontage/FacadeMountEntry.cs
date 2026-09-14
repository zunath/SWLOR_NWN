#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Frontage
{
    /// <summary>
    /// One weighted wall-mounted sign/holo/banner placeable the facade-mount pass
    /// (BuildingFrontagePlanner.PlanFacadeMounts) may hang on a building face -- stamped structure
    /// tile faces and <see cref="BuildingFrontageEntry"/> placeable faces -- at an evidence-derived
    /// height band. Mined from the dense hand-built fcx01 city areas' elevated (Z &gt; 0.5m)
    /// decoratives: sign-family items sit ON building faces (median face distance ~0) at per-resref
    /// Z bands between 1.1 and 7.0m in the frontage audit.
    /// </summary>
    public class FacadeMountEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        /// <summary>Bottom of the mined mounting-height band (meters above ground).</summary>
        public float MinHeight { get; set; } = 2f;
        /// <summary>Top of the mined mounting-height band (meters above ground).</summary>
        public float MaxHeight { get; set; } = 6f;
    }
}
