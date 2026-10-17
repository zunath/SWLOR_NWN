using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition;

public sealed class OrdinaryAttackHasteStatusEffect : StatusEffectBase
{
    public int Stacks { get; }
    public int HastePerStack { get; }
    public override string Name => $"Attack Rhythm ({Stacks})";
    public override EffectIconType Icon => Stacks switch
    {
        2 => EffectIconType.OrdinaryAttackHasteStatusEffect2,
        3 => EffectIconType.OrdinaryAttackHasteStatusEffect3,
        4 => EffectIconType.OrdinaryAttackHasteStatusEffect4,
        >= 5 => EffectIconType.OrdinaryAttackHasteStatusEffect5,
        _ => EffectIconType.OrdinaryAttackHasteStatusEffect1
    };
    public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
    public override bool PersistsOnLogout => false;
    public override bool SendsApplicationMessage => false;
    public override bool SendsWornOffMessage => false;
    public OrdinaryAttackHasteStatusEffect() : this(1, 2) { }
    public OrdinaryAttackHasteStatusEffect(int stacks, int hastePerStack)
    {
        Stacks = Math.Max(0, stacks);
        HastePerStack = Math.Max(0, hastePerStack);
        StatGroup.Stats[StatType.AttackDelayReductionPercent] = Stacks * HastePerStack;
    }
    public override IStatusEffect Clone() => new OrdinaryAttackHasteStatusEffect(Stacks, HastePerStack);
}
