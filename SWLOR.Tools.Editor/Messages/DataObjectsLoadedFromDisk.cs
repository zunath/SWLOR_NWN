namespace SWLOR.Tools.Editor.Messages
{
    public class DataObjectsLoadedFromDisk
    {
        public string Folder { get; set; }

        public DataObjectsLoadedFromDisk(string folder)
        {
            Folder = folder;
        }
    }
}
