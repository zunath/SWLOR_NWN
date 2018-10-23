namespace SWLOR.Tools.Editor.Messages
{
    public class RenamedEditorObjectMessage<T>
    {
        public T RenamedEditorObject { get; set; }

        public RenamedEditorObjectMessage(T renamedEditorObject)
        {
            RenamedEditorObject = renamedEditorObject;
        }
    }
}
