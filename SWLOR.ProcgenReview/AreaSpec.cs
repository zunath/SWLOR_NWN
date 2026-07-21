using SWLOR.Game.Server.Service.AreaGenerationService;

/// <summary>
/// OverrideParameters is non-null only for --areas-file entries: the full effective
/// MacroLayoutParameters snapshot, used verbatim instead of Composition.BuildLayoutParameters() +
/// Entrances/Exits/DoorTransitions (see the main generation loop). Entrances/Exits/DoorTransitions
/// stay meaningful even for those entries (mirrored from the snapshot) for logging/display symmetry
/// with the string-spec kinds.
/// </summary>
record AreaSpec(string Resref, string DisplayName, DungeonComposition Composition, int Seed, int Size, int Entrances = 1, int Exits = 1, bool DoorTransitions = true, MacroLayoutParameters OverrideParameters = null, bool EnableDecorations = true, int DecorationDensityPercent = 100, string DecorationProfile = "");
