using System;

namespace SWLOR.Game.Server.ValueObject
{
    public class ProcessingEvent
    {
        public Type ProcessorType { get; set; }
        public object[] Args { get; set; }

        public ProcessingEvent(Type processorType, object[] args)
        {
            ProcessorType = processorType;
            Args = args;
        }
    }
}
