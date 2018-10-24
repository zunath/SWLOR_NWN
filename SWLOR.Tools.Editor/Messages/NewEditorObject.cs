namespace SWLOR.Tools.Editor.Messages
{
    public class NewEditorObject<T>
    {
        public T Object { get; set; }

        public NewEditorObject(T newEditorObject)
        {
            Object = newEditorObject;
        }
    }
}
