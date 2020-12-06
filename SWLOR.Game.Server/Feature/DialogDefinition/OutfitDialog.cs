using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.DialogService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class OutfitDialog: DialogBase
    {
        public override PlayerDialog SetUp(uint player)
        {
            var dialog = DialogBuilder.Create();


            return dialog.Build();
        }
    }
}
