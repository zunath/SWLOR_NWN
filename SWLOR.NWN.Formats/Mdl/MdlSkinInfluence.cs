// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Mdl;

/// <summary>
/// A named bone influence declared by an ASCII skinmesh vertex.
/// </summary>
public readonly record struct MdlSkinInfluence(string BoneName, float Weight);
