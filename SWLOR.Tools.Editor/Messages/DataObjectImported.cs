using SWLOR.Tools.Editor.Enumeration;

namespace SWLOR.Tools.Editor.Messages
{
    public class DataObjectImported
    {
        public ResourceType ResourceType { get; set; }
        public object DataObject { get; set; }

        public DataObjectImported(ResourceType resourceType, object dataObject)
        {
            ResourceType = resourceType;
            DataObject = dataObject;
        }
    }
}
