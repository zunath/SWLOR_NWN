using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ImplantInstallationStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new StatusEffectBuilder();
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            ImplantInstallation1();

            return _builder.Build();
        }

        private void ImplantInstallation1()
        {
            _builder.Create(StatusEffectType.ImplantInstallation1)
                .Name("Implant Installation I")
                .EffectIcon(0) // todo find an icon
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 1 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                });
        }
    }
}
