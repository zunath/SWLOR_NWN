using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class ObjectFactoryProcessor: IEventProcessor
    {
        public void Run(object[] args)
        {
            NWObjectFactory.Clean();
        }
    }
}
