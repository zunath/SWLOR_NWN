using System;
using System.Reflection;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class ScriptEvent
    {
        public static void Run(string variableName)
        {
            NWObject self = (Object.OBJECT_SELF);
            string script = self.GetLocalString(variableName);

            using (new Profiler("ScriptEvent." + script))
            {
                Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

                if (type == null)
                {
                    Console.WriteLine("Unable to locate type for ScriptEvent: " + script);
                    return;
                }

                IRegisteredEvent @event = Activator.CreateInstance(type) as IRegisteredEvent;
                @event?.Run();
            }
        }
    }
}
