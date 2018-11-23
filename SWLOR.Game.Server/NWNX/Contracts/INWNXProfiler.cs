using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXProfiler
    {
        void PushPerfScope(string name);
        void PopPerfScope();
    }
}