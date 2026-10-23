using System.Collections.Immutable;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// Everything Prospecting knows about a session, as one immutable value. The shared meters hold the node and the budget: Progress is
/// the yield taken from the pool (its maximum is the pool the session began with), Integrity is the node's Stability, and CP is the
/// harvester's charges. Quality is not used. Seams are never shown by a meter, so a preview cannot give away how many are hidden.
/// </summary>
/// <param name="Meters">Yield taken, Stability, charges.</param>
/// <param name="NextOpportunity">Always null: the only surprise Prospecting has is which seams are hidden.</param>
/// <param name="Seams">The seams still in the node, hidden or revealed, in the order they are found. Each seals one unit of the pool.</param>
/// <param name="Units">Units taken from the pool that are plain ore: struck, or the unit of a rich seam. Each is one roll of the node's loot table.</param>
/// <param name="RichRolls">Extra ore rolls from the rich seams claimed.</param>
/// <param name="Gems">Gems from the gem pockets claimed, each in place of the unit it sealed.</param>
/// <param name="Shattered">Gem pockets a Heavy Strike shattered.</param>
/// <param name="Ending">How the session stands: going, or how it ended.</param>
/// <param name="MotherLodeSpent">Whether the first Heavy Strike of the node, the one Mother Lode doubles, has been made.</param>
public sealed record ProspectingState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    ImmutableArray<ProspectingSeam> Seams,
    int Units,
    int RichRolls,
    int Gems,
    int Shattered,
    ProspectingEnding Ending,
    bool MotherLodeSpent = false) : IMinigameState<ProspectingState>
{
    public ProspectingState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>The units of yield left in the pool, the sealed ones included.</summary>
    public int Pool => Meters.Progress.Max - Meters.Progress.Value;

    /// <summary>The units of the pool that are not sealed in a seam.</summary>
    public int LooseUnits => Pool - Seams.Length;

    /// <summary>The node's Stability: 0 collapses it.</summary>
    public int Stability => Meters.Integrity.Value;

    /// <summary>The harvester's charges left.</summary>
    public int Charges => Meters.Cp.Value;

    /// <summary>Everything gathered so far, each item one unit of XP: plain units, extra ore rolls and gems.</summary>
    public int Items => Units + RichRolls + Gems;

    /// <summary>The seams that have been found and not yet claimed.</summary>
    public int RevealedSeams => Seams.Count(seam => seam.Revealed);

    /// <summary>The seams still hidden.</summary>
    public int HiddenSeams => Seams.Count(seam => !seam.Revealed);

    /// <summary>True while an unsurveyed gem pocket could still be shattered.</summary>
    public bool HasHiddenGemPocket => Seams.Any(seam => !seam.Revealed && seam.Kind == ProspectingSeamKind.GemPocket);

    /// <summary>The seam Extract would claim, or null when none is revealed. A revealed gem pocket goes first, then the rich seams.</summary>
    public int? NextExtractIndex
    {
        get
        {
            var rich = -1;
            for (var index = 0; index < Seams.Length; index++)
            {
                var seam = Seams[index];
                if (!seam.Revealed)
                    continue;

                if (seam.Kind == ProspectingSeamKind.GemPocket)
                    return index;

                if (rich < 0)
                    rich = index;
            }

            return rich >= 0 ? rich : null;
        }
    }
}
