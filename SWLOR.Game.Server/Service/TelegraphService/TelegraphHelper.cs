using System;
using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    public static class TelegraphHelper
    {
        /// <summary>
        /// Creates a simple sphere telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Center position</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateSphereTelegraph(
            uint creator, 
            Vector3 position, 
            float radius, 
            float duration, 
            bool isHostile, 
            ApplyTelegraphEffect action)
        {
            return Telegraph.CreateTelegraph(
                creator, 
                position, 
                0f, 
                new Vector2(radius, radius), 
                duration, 
                isHostile, 
                TelegraphType.Sphere, 
                action);
        }

        /// <summary>
        /// Creates a cone telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Base position of the cone</param>
        /// <param name="rotation">Direction the cone faces (in radians)</param>
        /// <param name="length">Length of the cone</param>
        /// <param name="width">Width of the cone at the end</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateConeTelegraph(
            uint creator, 
            Vector3 position, 
            float rotation, 
            float length, 
            float width, 
            float duration, 
            bool isHostile, 
            ApplyTelegraphEffect action)
        {
            return Telegraph.CreateTelegraph(
                creator, 
                position, 
                rotation, 
                new Vector2(length, width), 
                duration, 
                isHostile, 
                TelegraphType.Cone, 
                action);
        }

        /// <summary>
        /// Creates a line telegraph.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="position">Start position of the line</param>
        /// <param name="rotation">Direction the line faces (in radians)</param>
        /// <param name="length">Length of the line</param>
        /// <param name="width">Width of the line</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateLineTelegraph(
            uint creator, 
            Vector3 position, 
            float rotation, 
            float length, 
            float width, 
            float duration, 
            bool isHostile, 
            ApplyTelegraphEffect action)
        {
            return Telegraph.CreateTelegraph(
                creator, 
                position, 
                rotation, 
                new Vector2(length, width), 
                duration, 
                isHostile, 
                TelegraphType.Line, 
                action);
        }

        /// <summary>
        /// Creates a telegraph at a creature's position.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to center the telegraph on</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateTelegraphAtCreature(
            uint creator, 
            uint target, 
            TelegraphType type, 
            Vector2 size, 
            float duration, 
            bool isHostile, 
            ApplyTelegraphEffect action)
        {
            var position = GetPosition(target);
            var rotation = GetFacing(target);
            
            return Telegraph.CreateTelegraph(
                creator, 
                position, 
                rotation, 
                size, 
                duration, 
                isHostile, 
                type, 
                action);
        }

        /// <summary>
        /// Creates a telegraph in front of a creature.
        /// </summary>
        /// <param name="creator">Creature creating the telegraph</param>
        /// <param name="target">Target creature to position the telegraph in front of</param>
        /// <param name="distance">Distance in front of the target</param>
        /// <param name="type">Type of telegraph</param>
        /// <param name="size">Size of the telegraph</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="isHostile">Whether this telegraph is hostile</param>
        /// <param name="action">Action to execute when telegraph completes</param>
        /// <returns>Telegraph ID</returns>
        public static string CreateTelegraphInFrontOfCreature(
            uint creator, 
            uint target, 
            float distance, 
            TelegraphType type, 
            Vector2 size, 
            float duration, 
            bool isHostile, 
            ApplyTelegraphEffect action)
        {
            var position = GetPosition(target);
            var rotation = GetFacing(target);
            
            // Calculate position in front of the creature
            var frontPosition = new Vector3(
                position.X + (float)Math.Cos(rotation) * distance,
                position.Y + (float)Math.Sin(rotation) * distance,
                position.Z
            );
            
            return Telegraph.CreateTelegraph(
                creator, 
                frontPosition, 
                rotation, 
                size, 
                duration, 
                isHostile, 
                type, 
                action);
        }
    }
}
