using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
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
            ImplantInstallation2();
            ImplantInstallation3();
            ImplantInstallation4();
            ImplantInstallation5();

            return _builder.Build();
        }

        private void ImplantInstallation1()
        {
            _builder.Create(StatusEffectType.ImplantInstallation1)
                .Name("Implant Installation I")
                .EffectIcon(63) 
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 1 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                })
                .TickAction((source, target) =>
                {
                    if (!GetIsObjectValid(source) || GetArea(source) != GetArea(target) || GetDistanceBetween(source, target) > 5f)
                    {
                        StatusEffect.Remove(target, StatusEffectType.ImplantInstallation1);
                    }
                });
        }
        private void ImplantInstallation2()
        {
            _builder.Create(StatusEffectType.ImplantInstallation2)
                .Name("Implant Installation II")
                .EffectIcon(63)
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 2 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                })
                .TickAction((source, target) =>
                {
                    if (!GetIsObjectValid(source) || GetArea(source) != GetArea(target) || GetDistanceBetween(source, target) > 5f)
                    {
                        StatusEffect.Remove(target, StatusEffectType.ImplantInstallation2);
                    }
                });
        }
        private void ImplantInstallation3()
        {
            _builder.Create(StatusEffectType.ImplantInstallation3)
                .Name("Implant Installation III")
                .EffectIcon(63)
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 3 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                })
                .TickAction((source, target) =>
                {
                    if (!GetIsObjectValid(source) || GetArea(source) != GetArea(target) || GetDistanceBetween(source, target) > 5f)
                    {
                        StatusEffect.Remove(target, StatusEffectType.ImplantInstallation3);
                    }
                });
        }
        private void ImplantInstallation4()
        {
            _builder.Create(StatusEffectType.ImplantInstallation4)
                .Name("Implant Installation IV")
                .EffectIcon(63)
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 4 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                })
                .TickAction((source, target) =>
                {
                    if (!GetIsObjectValid(source) || GetArea(source) != GetArea(target) || GetDistanceBetween(source, target) > 5f)
                    {
                        StatusEffect.Remove(target, StatusEffectType.ImplantInstallation4);
                    }
                });
        }
        private void ImplantInstallation5()
        {
            _builder.Create(StatusEffectType.ImplantInstallation5)
                .Name("Implant Installation V")
                .EffectIcon(63)
                .GrantAction((source, target, length) =>
                {
                    SendMessageToPC(target, "You may install tier 5 implants until you leave this area.");
                })
                .RemoveAction(target =>
                {
                    SendMessageToPC(target, "You may no longer install implants.");
                })
                .TickAction((source, target) =>
                {
                    if (!GetIsObjectValid(source) || GetArea(source) != GetArea(target) || GetDistanceBetween(source, target) > 5f)
                    {
                        StatusEffect.Remove(target, StatusEffectType.ImplantInstallation5);
                    }
                });
        }
    }
}
