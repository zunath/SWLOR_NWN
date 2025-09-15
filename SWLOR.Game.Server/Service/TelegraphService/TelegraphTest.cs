using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    /// <summary>
    /// Test script for the telegraph system.
    /// This can be used to test telegraph functionality in-game.
    /// </summary>
    public static class TelegraphTest
    {
        /// <summary>
        /// Test command to create a sphere telegraph.
        /// Usage: Call this from a conversation or script to test sphere telegraphs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void TestSphereTelegraph()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            var position = GetPosition(player);
            
            TelegraphHelper.CreateSphereTelegraph(
                player,
                position,
                5.0f, // 5 meter radius
                3.0f, // 3 seconds duration
                true, // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, $"Sphere telegraph completed! Affected {affectedCreatures.Count} creatures.");
                    
                    foreach (var creature in affectedCreatures)
                    {
                        SendMessageToPC(creator, $"  - {GetName(creature)}");
                    }
                });
                
            SendMessageToPC(player, "Created sphere telegraph test!");
        }

        /// <summary>
        /// Test command to create a cone telegraph.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void TestConeTelegraph()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            var position = GetPosition(player);
            var facing = GetFacing(player);
            
            TelegraphHelper.CreateConeTelegraph(
                player,
                position,
                facing,
                8.0f, // 8 meter length
                4.0f, // 4 meter width
                2.0f, // 2 seconds duration
                true, // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, $"Cone telegraph completed! Affected {affectedCreatures.Count} creatures.");
                    
                    foreach (var creature in affectedCreatures)
                    {
                        SendMessageToPC(creator, $"  - {GetName(creature)}");
                    }
                });
                
            SendMessageToPC(player, "Created cone telegraph test!");
        }

        /// <summary>
        /// Test command to create a line telegraph.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void TestLineTelegraph()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            var position = GetPosition(player);
            var facing = GetFacing(player);
            
            TelegraphHelper.CreateLineTelegraph(
                player,
                position,
                facing,
                10.0f, // 10 meter length
                2.0f,  // 2 meter width
                1.5f,  // 1.5 seconds duration
                true,  // Hostile telegraph
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, $"Line telegraph completed! Affected {affectedCreatures.Count} creatures.");
                    
                    foreach (var creature in affectedCreatures)
                    {
                        SendMessageToPC(creator, $"  - {GetName(creature)}");
                    }
                });
                
            SendMessageToPC(player, "Created line telegraph test!");
        }

        /// <summary>
        /// Test command to create a beneficial telegraph.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void TestBeneficialTelegraph()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            var position = GetPosition(player);
            
            TelegraphHelper.CreateSphereTelegraph(
                player,
                position,
                6.0f, // 6 meter radius
                4.0f, // 4 seconds duration
                false, // Not hostile (beneficial)
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, $"Beneficial telegraph completed! Affected {affectedCreatures.Count} creatures.");
                    
                    foreach (var creature in affectedCreatures)
                    {
                        SendMessageToPC(creator, $"  - {GetName(creature)}");
                    }
                });
                
            SendMessageToPC(player, "Created beneficial telegraph test!");
        }

        /// <summary>
        /// Test command to clear all telegraphs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void ClearAllTelegraphs()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            Telegraph.ClearAllTelegraphs();
            SendMessageToPC(player, "Cleared all telegraphs!");
        }

        /// <summary>
        /// Test command to create multiple telegraphs at once.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void TestMultipleTelegraphs()
        {
            var player = GetLastSpeaker();
            if (!GetIsPC(player))
                return;

            var position = GetPosition(player);
            var facing = GetFacing(player);
            
            // Create multiple telegraphs with different timings
            TelegraphHelper.CreateSphereTelegraph(
                player,
                position,
                3.0f, // 3 meter radius
                1.0f, // 1 second duration
                true,
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, "First telegraph (sphere) completed!");
                });

            TelegraphHelper.CreateConeTelegraph(
                player,
                position,
                facing,
                6.0f, // 6 meter length
                3.0f, // 3 meter width
                2.0f, // 2 seconds duration
                true,
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, "Second telegraph (cone) completed!");
                });

            TelegraphHelper.CreateLineTelegraph(
                player,
                position,
                facing,
                8.0f, // 8 meter length
                1.5f, // 1.5 meter width
                3.0f, // 3 seconds duration
                true,
                (creator, affectedCreatures) =>
                {
                    SendMessageToPC(creator, "Third telegraph (line) completed!");
                });
                
            SendMessageToPC(player, "Created multiple telegraph test!");
        }
    }
}
