using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The type names <see cref="ReloadStateCarrier"/> writes where a field holds a subclass or an implementation of its declared
    /// type (a status effect in a set of <c>IStatusEffect</c>): the full name of a type of the game assembly, which the new build
    /// resolves in its own game assembly. Any other type is refused both ways, so the carry never names or creates a type outside
    /// the build.
    /// </summary>
    public sealed class ReloadCarryBinder : ISerializationBinder
    {
        private readonly Assembly _gameAssembly;

        public ReloadCarryBinder(Assembly gameAssembly)
        {
            _gameAssembly = gameAssembly ?? throw new ArgumentNullException(nameof(gameAssembly));
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (serializedType == null || serializedType.Assembly != _gameAssembly || serializedType.IsGenericType)
                throw new JsonSerializationException($"Only types of the game build can be named in a reload carry, not {serializedType?.FullName}.");

            assemblyName = null;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return _gameAssembly.GetType(typeName, throwOnError: false)
                   ?? throw new JsonSerializationException($"The reloaded build has no type {typeName} to carry state into.");
        }
    }
}
