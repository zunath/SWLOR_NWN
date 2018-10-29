using System;

namespace SWLOR.Tools.Editor.Attributes
{
    public class FolderAttribute: Attribute
    {
        public string Folder { get; set; }

        public FolderAttribute(string folder)
        {
            Folder = folder;
        }
    }
}
