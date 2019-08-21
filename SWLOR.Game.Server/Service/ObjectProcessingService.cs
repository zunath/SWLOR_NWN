using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class ObjectProcessingService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        static ObjectProcessingService()
        {
        }

        private static void OnModuleLoad()
        {
            RunProcessor();
        }
        
        public static float ProcessingTickInterval => 1f;

        private static void RunProcessor()
        {
            MessageHub.Instance.Publish(new OnObjectProcessorRan());

            _.DelayCommand(ProcessingTickInterval, () => RunProcessor());
        }
    }
}
