using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    /// <summary>
    /// Example usage of the telegraph system.
    /// This class demonstrates how to create and use telegraphs in abilities.
    /// </summary>
    public static class TelegraphExample
    {
        /// <summary>
        /// Example: Create a simple area attack telegraph
        /// </summary>
        /// <param name="attacker">Creature performing the attack</param>
        /// <param name="target">Target creature</param>
        public static void CreateAreaAttackTelegraph(uint attacker, uint target)
        {
            var position = GetPosition(target);
            
            // Create a 3-second telegraph that damages all enemies in a 5-meter radius
            TelegraphHelper.CreateSphereTelegraph(
                attacker,
                position,
                5.0f, // 5 meter radius
                3.0f, // 3 seconds duration
                true, // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    // This action executes when the telegraph completes
                    foreach (var creature in affectedCreatures)
                    {
                        if (GetIsEnemy(creator, creature))
                        {
                            // Deal damage to each affected enemy
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(50, DamageType.Fire), creature);
                            SendMessageToPC(creator, $"Hit {GetName(creature)} for 50 fire damage!");
                        }
                    }
                });
        }

        /// <summary>
        /// Example: Create a cone attack telegraph
        /// </summary>
        /// <param name="attacker">Creature performing the attack</param>
        public static void CreateConeAttackTelegraph(uint attacker)
        {
            var position = GetPosition(attacker);
            var facing = GetFacing(attacker);
            
            // Create a 2-second cone telegraph in front of the attacker
            TelegraphHelper.CreateConeTelegraph(
                attacker,
                position,
                facing,
                8.0f, // 8 meter length
                4.0f, // 4 meter width at the end
                2.0f, // 2 seconds duration
                true, // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    foreach (var creature in affectedCreatures)
                    {
                        if (GetIsEnemy(creator, creature))
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(75, DamageType.Cold), creature);
                            SendMessageToPC(creator, $"Cone attack hit {GetName(creature)} for 75 cold damage!");
                        }
                    }
                });
        }

        /// <summary>
        /// Example: Create a line attack telegraph
        /// </summary>
        /// <param name="attacker">Creature performing the attack</param>
        /// <param name="target">Target creature to aim at</param>
        public static void CreateLineAttackTelegraph(uint attacker, uint target)
        {
            var attackerPos = GetPosition(attacker);
            var targetPos = GetPosition(target);
            
            // Calculate direction from attacker to target
            var dx = targetPos.X - attackerPos.X;
            var dy = targetPos.Y - attackerPos.Y;
            var dz = targetPos.Z - attackerPos.Z;
            var length = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var rotation = (float)System.Math.Atan2(dy, dx);
            
            // Create a 1.5-second line telegraph
            TelegraphHelper.CreateLineTelegraph(
                attacker,
                attackerPos,
                rotation,
                10.0f, // 10 meter length
                2.0f,  // 2 meter width
                1.5f,  // 1.5 seconds duration
                true,  // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    foreach (var creature in affectedCreatures)
                    {
                        if (GetIsEnemy(creator, creature))
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(100, DamageType.Electrical), creature);
                            SendMessageToPC(creator, $"Line attack hit {GetName(creature)} for 100 electrical damage!");
                        }
                    }
                });
        }

        /// <summary>
        /// Example: Create a beneficial telegraph (healing)
        /// </summary>
        /// <param name="caster">Creature casting the spell</param>
        public static void CreateHealingTelegraph(uint caster)
        {
            var position = GetPosition(caster);
            
            // Create a 4-second healing telegraph
            TelegraphHelper.CreateSphereTelegraph(
                caster,
                position,
                6.0f, // 6 meter radius
                4.0f, // 4 seconds duration
                false, // Not hostile (beneficial)
                (creator, affectedCreatures) =>
                {
                    foreach (var creature in affectedCreatures)
                    {
                        if (GetIsReactionTypeFriendly(creator, creature) == 1)
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectHeal(30), creature);
                            SendMessageToPC(creator, $"Healed {GetName(creature)} for 30 HP!");
                        }
                    }
                });
        }

        /// <summary>
        /// Example: Create a telegraph that follows a creature
        /// </summary>
        /// <param name="caster">Creature creating the telegraph</param>
        /// <param name="target">Target to follow</param>
        public static void CreateFollowingTelegraph(uint caster, uint target)
        {
            // This would require a more complex implementation with periodic updates
            // For now, this is just a placeholder showing the concept
            var position = GetPosition(target);
            
            TelegraphHelper.CreateSphereTelegraph(
                caster,
                position,
                3.0f, // 3 meter radius
                5.0f, // 5 seconds duration
                true, // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    foreach (var creature in affectedCreatures)
                    {
                        if (GetIsEnemy(creator, creature))
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(40, DamageType.Acid), creature);
                        }
                    }
                });
        }
    }
}
