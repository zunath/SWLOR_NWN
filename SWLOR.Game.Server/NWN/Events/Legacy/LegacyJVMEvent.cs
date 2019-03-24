using System;
using System.Reflection;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.NWN.Events.Legacy
{
    public static class LegacyJVMEvent
    {
        public static void Run(string variableName)
        {
            NWObject self = (Object.OBJECT_SELF);
            string script = self.GetLocalString(variableName);

            using (new Profiler("LegacyJVMEvent::" + script))
            {
                Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

                if (type == null)
                {
                    Console.WriteLine("Unable to locate type for LegacyJVMEvent: " + script);
                    return;
                }

                IRegisteredEvent @event = Activator.CreateInstance(type) as IRegisteredEvent;
                @event?.Run();
            }
        }
    }
}
