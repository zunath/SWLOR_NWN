// Mirrors SWLOR.Game.Server/GlobalUsings.cs so test code reads identically to
// game code (bare NWScript calls, System, status-effect types).
global using System;
global using SWLOR.Game.Server.Feature.StatusEffectDefinition;
global using static SWLOR.NWN.API.NWScript.NWScript;

// These two are NOT in the game project's global usings - it doesn't need them
// because its own code lives inside these namespaces. Test code now lives in
// SWLOR.Game.Server.EngineTests.*, so it would otherwise need a per-file using
// for the services (Ability, Perk, Combat, Stat, ...) and features (UsePerkFeat)
// that virtually every test touches.
global using SWLOR.Game.Server.Service;
global using SWLOR.Game.Server.Feature;
