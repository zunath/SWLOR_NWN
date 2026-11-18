using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The stand-ins for the converted neighbours the ability instance takes, which answer from the bench and log what they are asked.</summary>
public sealed partial class AbilityBench
{
    private IStatusEffectApplicationService StatusApplications()
    {
        var service = ScriptedProxy.Create<IStatusEffectApplicationService>(out var proxy);
        proxy.On("ApplyStatusEffect", args =>
            {
                var effect = args[2] as IStatusEffect;
                var type = args[2] as Type ?? effect!.GetType();
                Log.Add($"ApplyStatus:{args[0]}:{args[1]}:{type.Name}:{args[3]}:{args[4]}");
                if (!StatusApplies)
                    return false;
                ActiveEffects[((uint)args[1]!, type)] = effect ?? ((IStatusEffect)Activator.CreateInstance(type)!);
                return true;
            });
        return service;
    }

    private IStatusEffectService StatusEffects()
    {
        var service = ScriptedProxy.Create<IStatusEffectService>(out var proxy);
        proxy
            .On("GetCreatureStatusEffects", args => CreatureEffects.TryGetValue((uint)args[0]!, out var found)
                ? found
                : new CreatureStatusEffect { StatGroup = new StatGroup() })
            .On("GetStatusEffect", args => ActiveEffects.GetValueOrDefault(((uint)args[0]!, (Type)args[1]!)))
            .On("HasStatusEffect", args => ActiveEffects.ContainsKey(((uint)args[0]!, (Type)args[1]!)))
            .On("HasStatusEffectCategory", args => (TypeCategories & (StatusEffectCategory)args[1]!) == (StatusEffectCategory)args[1]! && TypeCategories != StatusEffectCategory.None)
            .On("NotifyAttackAttemptStatusEffects", args =>
            {
                Log.Add($"NotifyAttackAttempt:{args[0]}:{args[1]}");
                return null;
            })
            .On("NotifyDamageStatusEffects", args =>
            {
                Log.Add($"NotifyDamage:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("NotifyPreDamageStatusEffects", args =>
            {
                Log.Add($"NotifyPreDamage:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
                return null;
            })
            .On("RemoveStatusEffect", args =>
            {
                Log.Add($"RemoveStatus:{args[0]}:{((Type)args[1]!).Name}:{args[2]}:{args[3]}");
                ActiveEffects.Remove(((uint)args[0]!, (Type)args[1]!));
                return null;
            });
        return service;
    }

    private IStatusEffectCatalogService StatusCatalog()
    {
        var service = ScriptedProxy.Create<IStatusEffectCatalogService>(out var proxy);
        proxy
            .On("CreateStatusEffect", args => (IStatusEffect)Activator.CreateInstance((Type)args[0]!)!)
            .On("GetStatusEffectName", args => ((Type)args[0]!).Name);
        return service;
    }

    private IEnmityService Enmity()
    {
        var service = ScriptedProxy.Create<IEnmityService>(out var proxy);
        proxy.On("ModifyEnmity", args =>
        {
            Log.Add($"ModifyEnmity:{args[0]}:{args[1]}:{args[2]}");
            return null;
        });
        return service;
    }

    private ICombatPointService CombatPoints()
    {
        var service = ScriptedProxy.Create<ICombatPointService>(out var proxy);
        proxy.On("AddCombatPoint", args =>
        {
            Log.Add($"AddCombatPoint:{args[0]}:{args[1]}:{args[2]}:{args[3]}");
            return null;
        });
        return service;
    }

    private IRecastService Recast()
    {
        var service = ScriptedProxy.Create<IRecastService>(out var proxy);
        proxy.On("IsOnRecastDelay", args =>
        {
            Log.Add($"IsOnRecastDelay:{args[0]}:{args[1]}");
            return RecastAnswer;
        });
        return service;
    }

    private ITelegraphService Telegraph()
    {
        var service = ScriptedProxy.Create<ITelegraphService>(out var proxy);
        proxy.On("CreateSphereTelegraph", args =>
            {
                Log.Add($"SphereTelegraph:{args[0]}:{args[2]}:{args[3]}:{args[4]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[5]!;
                return "telegraph";
            })
            .On("CreateConeTelegraph", args =>
            {
                Log.Add($"ConeTelegraph:{args[0]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[7]!;
                return "telegraph";
            })
            .On("CreateLineTelegraph", args =>
            {
                Log.Add($"LineTelegraph:{args[0]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}");
                PendingTelegraph = (ApplyTelegraphEffect)args[7]!;
                return "telegraph";
            })
            .On("CreateTelegraph", args =>
            {
                Log.Add($"Flash:{args[0]}:{args[2]}:{args[3]}:{args[4]}:{args[5]}:{args[6]}:{args[7] == null}");
                return "flash";
            })
            .On("ResolveImpactGeometry", args =>
            {
                var impact = (TelegraphGeometry)args[0]!;
                return ResolveGeometry?.Invoke(impact) ?? impact;
            })
            .On("ShouldShowImpactFlash", args => ImpactFlashShows);
        return service;
    }

    private IMessagingService Messaging()
    {
        var service = ScriptedProxy.Create<IMessagingService>(out var proxy);
        proxy.On("SendMessageNearbyToPlayers", args =>
        {
            var build = (BuildMessageDelegate)args[1]!;
            Log.Add($"Nearby:{args[0]}:{build(Observer)}:{args[2]}");
            return null;
        });
        return service;
    }

    private IMusicService Music()
    {
        var service = ScriptedProxy.Create<IMusicService>(out var proxy);
        // As an ability impact observer, the music service starts the battle theme of the engaged target.
        proxy.On("OnTargetEngaged", args =>
            {
                Log.Add($"BattleTheme:{args[1]}:{args[0]}");
                return null;
            })
            .On("OnAbilityImpact", args => null);
        return service;
    }

    private INamedAnimationService NamedAnimation()
    {
        var service = ScriptedProxy.Create<INamedAnimationService>(out var proxy);
        proxy.On("Play", args =>
            {
                Log.Add($"NamedPlay:{args[0]}");
                return null;
            })
            .On("ReleaseForNativePlayback", args =>
            {
                Log.Add($"NamedRelease:{args[0]}");
                return null;
            });
        return service;
    }

    private IMimicryService Mimicry()
    {
        var service = ScriptedProxy.Create<IMimicryService>(out var proxy);
        proxy.On("IsTechniqueEquipped", args => TechniqueEquipped);
        return service;
    }

    private IActivityService Activity()
    {
        var service = ScriptedProxy.Create<IActivityService>(out var proxy);
        proxy.On("IsBusy", args => Busy.Contains((uint)args[0]!));
        return service;
    }

    private IPartyRoster Party()
    {
        var service = ScriptedProxy.Create<IPartyRoster>(out var proxy);
        proxy.On("IsInParty", args => PartyPairs.Contains(((uint)args[0]!, (uint)args[1]!)));
        return service;
    }

    private IPlayerFeedbackService PlayerFeedback()
    {
        var service = ScriptedProxy.Create<IPlayerFeedbackService>(out var proxy);
        proxy.On("ShowCooldownFloatingText", args =>
        {
            Log.Add($"CooldownText:{args[0]}:{args[1]}:{args[2]}");
            return true;
        });
        return service;
    }

    private ICombatRecorderService Recorder()
    {
        var service = ScriptedProxy.Create<ICombatRecorderService>(out var proxy);
        proxy.On("get_IsRecording", args => Recording)
            .On("BeginAbilityAttribution", args =>
            {
                Log.Add($"RecorderAbility:{args[0]}:{(args[1] as AbilityDetail)?.Name}");
                return default(CombatRecordAttributionScope);
            })
            .On("BeginAttribution", args => default(CombatRecordAttributionScope))
            .On("BeginStatTriggerAttribution", args =>
            {
                Log.Add($"RecorderStat:{args[0]}:{args[1]}");
                return default(CombatRecordAttributionScope);
            })
            .On("CaptureScope", args => default(CombatRecordAttribution))
            .On("ExpectHitPointCost", args =>
            {
                Log.Add($"RecorderHitPointCost:{args[0]}:{args[1]}");
                return null;
            })
            .On("RecordAttack", args =>
            {
                Log.Add($"RecorderAttack:{args[0]}:{args[1]}:{args[2]}:{args[3]}:{args[4]}");
                return null;
            })
            .On("RecordRecurringAbilityImpact", args =>
            {
                Log.Add($"RecorderRecurring:{args[0]}:{(args[1] as AbilityDetail)?.Name}");
                return null;
            });
        return service;
    }

    private IMeleeTechniqueService MeleeTechnique()
    {
        var service = ScriptedProxy.Create<IMeleeTechniqueService>(out var proxy);
        proxy.On("IsSweepActivation", args => SweepActivation)
            .On("GetCapturedWeaponDamage", args => CapturedWeaponDamage)
            .On("GetCapturedWeaponDamageType", args => CapturedWeaponDamageType)
            .On("ValidateActivation", args => TechniqueError)
            .On("IsImpactValid", args => TechniqueImpactValid)
            .On("GetAbilityAccuracy", args => TechniqueAccuracy)
            .On("CompleteActivation", args =>
            {
                Log.Add($"TechniqueComplete:{args[0]}:{(args[1] as AbilityImpactSummary)?.ImpactedTargetCount}");
                return null;
            });
        return service;
    }

    private ICapstoneProofService CapstoneProof()
    {
        var service = ScriptedProxy.Create<ICapstoneProofService>(out var proxy);
        // As an ability impact observer, the capstone proof service records the impact.
        proxy.On("OnAbilityImpact", args =>
            {
                Log.Add($"CapstoneProof:{args[0]}:{args[1]}:{(args[2] as AbilityDetail)?.Name}");
                return null;
            })
            .On("OnTargetEngaged", args => null);
        return service;
    }

    private IResistanceService Resistance()
    {
        var service = ScriptedProxy.Create<IResistanceService>(out var proxy);
        proxy.On("IsValidResistanceType", args => (ResistanceType)args[0]! != ResistanceType.Invalid)
            .On("ApplyResistanceToDamage", args =>
            {
                Log.Add($"Resistance:{args[0]}:{args[1]}:{args[2]}");
                return args[2];
            });
        return service;
    }

    private IStealthService Stealth()
    {
        var service = ScriptedProxy.Create<IStealthService>(out var proxy);
        proxy.On("IsConcealed", args => Concealed.Contains((uint)args[0]!));
        return service;
    }
}
