namespace SWLOR.Game.Server.Tests.Core.ContentCarry;

/// <summary>The data base of a carried sample effect, as <c>StatusEffectBase</c> is of the status effects: no services.</summary>
public abstract class CarriedSampleEffectBase
{
    protected CarriedSampleEffectBase()
    {
        Id = Guid.NewGuid();
        Stacks = new List<int>();
    }

    public Guid Id { get; private set; }

    public int DurationTicks { get; set; }

    public List<int> Stacks { get; private set; }
}
