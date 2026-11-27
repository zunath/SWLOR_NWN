namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The steps in which a creature's combat state is forgotten, in the order the cleanup runs them. The forgets have side
    /// effects that later forgets observe (a suppression forget removes the overwatch status, a damage modifier forget clears the
    /// repeated-target tracker and its status), so the order is part of the behavior: <see cref="ICombatService"/> walks the
    /// steps from the first to the last over the registered <see cref="ICombatCreatureStateOwner"/> list, and the owners that
    /// share a step run in registration order. Two steps belong to a service that appears twice in the order, so a service can
    /// own more than one step.
    /// </summary>
    public enum CombatForgetPhase
    {
        /// <summary>The passive and defeated-enemy healing budgets and the stat trigger cooldowns.</summary>
        ResourceTriggers = 1,

        /// <summary>The recent damage targets and the last combat activity.</summary>
        Activity = 2,

        /// <summary>What the creature guarded or deflected.</summary>
        DefenseMemory = 3,

        /// <summary>The Suppression stacks and the overwatch status.</summary>
        Suppression = 4,

        /// <summary>The hostile ability sequence the creature was in.</summary>
        Activation = 5,

        /// <summary>The auto-attack cycle counts and the critical hit sequence.</summary>
        HitEffects = 6,

        /// <summary>The same-target hit state and the area ability hit sequences.</summary>
        AbilityImpact = 7,

        /// <summary>The first hostile ability hit counts and the stealth opening windows.</summary>
        OpeningActivity = 8,

        /// <summary>The stamina costs recorded for the creature's abilities.</summary>
        AbilityStaminaCost = 9,

        /// <summary>The attack swing debts.</summary>
        AttackTiming = 10,

        /// <summary>The auto-attack cycle critical rate count, the repeated-target trackers and the same-target pressure.</summary>
        DamageModifiers = 11
    }
}
