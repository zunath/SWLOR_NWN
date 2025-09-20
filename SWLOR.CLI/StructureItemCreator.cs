using System;
using System.IO;
using System.Linq;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.CLI
{
    class StructureItemCreator
    {
        public void Process()
        {
            const string Output = "./structure_output/";
            const string Template = "structure_0000.uti.json";

            if (Directory.Exists(Output))
            {
                Directory.Delete(Output, true);
            }

            Directory.CreateDirectory(Output);

            var templateContents = File.ReadAllText($"./Templates/{Template}");


            var structureTypes = Enum.GetValues(typeof(StructureType)).Cast<StructureType>();
            foreach (var structure in structureTypes)
            {
                var structureDetail = structure.GetAttribute<StructureType, StructureAttribute>();

                var id = ((int)structure).ToString().PadLeft(4, '0');
                var fileName = Template.Replace("0000", id);

                var contents = templateContents
                    .Replace("%%NAME%%", structureDetail.Name)
                    .Replace("%%TAG%%", $"structure_{id}")
                    .Replace("%%RESREF%%", $"structure_{id}");

                File.WriteAllText($"{Output}{fileName}", contents);
            }
        }
    }
}
