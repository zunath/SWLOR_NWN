namespace SWLOR.Tools.Editor.Messages
{
    public class RenamedEditorObject<T>
    {
        public T Object { get; set; }

        public RenamedEditorObject(T renamedEditorObject)
        {
            Object = renamedEditorObject;
        }
    }
}
