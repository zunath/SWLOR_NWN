using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Autofac;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.Startup
{
    public class CreateDataDirectories: IStartable
    {
        public void Start()
        {
            var types =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(FolderAttribute), true)
                where attributes != null && attributes.Length > 0
                select new { Type = t, Attributes = attributes.Cast<FolderAttribute>() };
            
            foreach (var type in types)
            {
                var attribute = type.Attributes.First();
                CreateDirectory(attribute.Folder);
            }
        }

        private void CreateDirectory(string folder)
        {
            if (!Directory.Exists("./Data/" + folder))
            {
                Directory.CreateDirectory("./Data/" + folder);
            }
        }

    }
}
