namespace SWLOR.Game.Server.GameObject.Contracts
{
    public interface INWPlayer
    {
        bool IsBusy { get; set; }
        int EffectiveCastingSpeed { get; }
    }
}