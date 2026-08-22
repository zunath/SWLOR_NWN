#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// One-time post-processing step for a fully-discovered tileset-profile dictionary: a palette
    /// variant (<see cref="DungeonTilesetProfile.IsPaletteVariant"/>) that declared no
    /// Decorations/Vignettes of its own inherits them in place from the first non-variant profile
    /// registered under the same <see cref="DungeonTilesetProfile.TilesetResref"/> — see the
    /// Decorations doc comment. <see cref="Authoring.DefinitionCatalog"/> calls this once immediately
    /// after profile discovery, so an ordinary `tileset.Decorations`/
    /// `tileset.Vignettes` read anywhere else in the codebase already reflects the effective palette —
    /// no call site needs its own inheritance-lookup logic.
    /// </summary>
    public static class DungeonTilesetPaletteInheritance
    {
        public static void Apply(Dictionary<string, DungeonTilesetProfile> profiles)
        {
            foreach (var profile in profiles.Values)
            {
                if (!profile.IsPaletteVariant)
                    continue;

                if (profile.Decorations.Count == 0 && profile.Vignettes.Count == 0)
                {
                    var basis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        (p.Decorations.Count > 0 || p.Vignettes.Count > 0));

                    if (basis != null)
                    {
                        profile.Decorations = basis.Decorations;
                        profile.Vignettes = basis.Vignettes;
                        // Named alternate palettes travel with the standard one: a variant that
                        // declared no dressing of its own offers the same selectable profiles
                        // (e.g. fcx01's "ruined") as its family basis. Shared reference is fine --
                        // palettes are never mutated after build, only read.
                        if (profile.DecorationProfiles.Count == 0)
                            profile.DecorationProfiles = basis.DecorationProfiles;
                    }
                }

                // Structural frontage and facade mounts are family properties like the palette
                // above: a variant that declared none of its own walls/dresses like its family
                // basis (fcx01's Cobble2 district shares the Cobble district's swd_build canyon
                // vocabulary -- the hand-built evidence spans both districts' areas).
                if (profile.FrontageBuildings.Count == 0)
                {
                    var frontageBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.FrontageBuildings.Count > 0);
                    if (frontageBasis != null)
                        profile.FrontageBuildings = frontageBasis.FrontageBuildings;
                }

                if (profile.FacadeMounts.Count == 0)
                {
                    var mountBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.FacadeMounts.Count > 0);
                    if (mountBasis != null)
                        profile.FacadeMounts = mountBasis.FacadeMounts;
                }

                // Street dressing is a family property like frontage/mounts above: a variant that
                // declared no street pool of its own dresses its lanes like its family basis
                // (fcx01's Cobble2 district shares the Cobble district's road-plate/street-accent
                // evidence -- the hand-built exemplars span both districts' areas).
                if (profile.StreetDressings.Count == 0)
                {
                    var streetBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.StreetDressings.Count > 0);
                    if (streetBasis != null)
                        profile.StreetDressings = streetBasis.StreetDressings;
                }

                // The signature composition inherits like the palette: a variant district of an
                // urban family showcases at the same layout pairing/scale unless it declared its
                // own.
                if (string.IsNullOrEmpty(profile.SignatureLayoutProfileKey))
                {
                    var signatureBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        !string.IsNullOrEmpty(p.SignatureLayoutProfileKey));
                    if (signatureBasis != null)
                    {
                        profile.SignatureLayoutProfileKey = signatureBasis.SignatureLayoutProfileKey;
                        if (profile.SignatureSize == 0)
                            profile.SignatureSize = signatureBasis.SignatureSize;
                    }
                }

                // The urban placement grammar is a family property like density below: a variant of
                // an urban family dresses under the same grammar unless it declared its own palette
                // AND its evidence genuinely differs (in which case declare UrbanDressing on the
                // variant directly).
                if (!profile.UrbanDressing)
                {
                    profile.UrbanDressing = profiles.Values.Any(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref && p.UrbanDressing);
                }

                // Frontage scale jitter travels with the frontage vocabulary it modifies: a variant
                // that inherits its family's frontage walls jitters them the same way.
                if (!profile.FrontageScaleJitter)
                {
                    profile.FrontageScaleJitter = profiles.Values.Any(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref && p.FrontageScaleJitter);
                }

                // Chasm semantics are a physical property of the shared tileset art, not of the
                // palette: a variant district of a chasm-bearing family (fcx01's Cobble2 plaza)
                // renders the same bottomless "holes" drop, so its frontage walls obey the same
                // footprint-support rule as the family basis unless it declared its own list.
                if (profile.ChasmTerrains.Count == 0)
                {
                    var chasmBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.ChasmTerrains.Count > 0);
                    if (chasmBasis != null)
                        profile.ChasmTerrains = chasmBasis.ChasmTerrains;
                }

                // The family AREA atmosphere inherits like the palette above: a variant that
                // declared no atmosphere of its own carries its family basis's mined values
                // (fcx01's Cobble2/Plaza district shares the Cobble district's night-city .are
                // evidence -- the hand-built exemplars span both districts' areas). Shared
                // reference is fine -- atmospheres are never mutated after build, only read.
                if (profile.Atmosphere == null)
                {
                    var atmosphereBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.Atmosphere != null);
                    if (atmosphereBasis != null)
                    {
                        profile.Atmosphere = atmosphereBasis.Atmosphere;
                        if (profile.AtmosphereProfiles.Count == 0)
                            profile.AtmosphereProfiles = atmosphereBasis.AtmosphereProfiles;
                    }
                }

                // Family density (see DungeonTilesetProfile.DecorationDensityPerTile) inherits
                // independently of the palette lists: a variant that curated its own small palette
                // still dresses at its family's own measured intensity unless it declared one.
                if (profile.DecorationDensityPerTile <= 0)
                {
                    var densityBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.DecorationDensityPerTile > 0);

                    if (densityBasis != null)
                        profile.DecorationDensityPerTile = densityBasis.DecorationDensityPerTile;
                }
            }
        }
    }
}
