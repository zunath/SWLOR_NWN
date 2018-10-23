namespace SWLOR.Tools.Editor.Messages
{
    public class NewEditorObjectMessage<T>
    {
        public T NewEditorObject { get; set; }

        public NewEditorObjectMessage(T newEditorObject)
        {
            NewEditorObject = newEditorObject;
        }
    }
}
