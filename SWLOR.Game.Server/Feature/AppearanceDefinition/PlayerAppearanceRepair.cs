using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition;

public static class PlayerAppearanceRepair
{
    public static (PhenoType Phenotype, TailType Tail) Normalize(PhenoType phenotype, TailType tail) =>
        (phenotype == PhenoType.SpeederBike ? PhenoType.Normal : phenotype,
            tail is TailType.SwoopBike or TailType.SpeederBike or TailType.LandSpeeder ? TailType.None : tail);

    public static bool Restore(uint player)
    {
        var phenotype = GetPhenoType(player);
        var tail = GetCreatureTailType(player);
        var normalized = Normalize(phenotype, tail);

        // Saved riders can still carry retired IDs after their models and item action are removed.
        if (phenotype != normalized.Phenotype)
            SetPhenoType(normalized.Phenotype, player);
        if (tail != normalized.Tail)
            SetCreatureTailType(normalized.Tail, player);

        return phenotype != normalized.Phenotype || tail != normalized.Tail;
    }
}
