namespace SWLOR.Tools.Editor.Messages
{
    public class CreatedNewEditorObject<T>
    {
        public T Object { get; set; }

        public CreatedNewEditorObject(T obj)
        {
            Object = obj;
        }
    }
}
