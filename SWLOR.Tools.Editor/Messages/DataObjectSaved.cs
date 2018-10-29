namespace SWLOR.Tools.Editor.Messages
{
    public class DataObjectSaved<T>
    {
        public T DataObject { get; set; }

        public DataObjectSaved(T dataObject)
        {
            DataObject = dataObject;
        }
    }
}
