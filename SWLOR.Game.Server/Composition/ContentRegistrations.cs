// Written by tools/Refactor/SWLOR.ContentInjection (Apply-ContentInjection.ps1); do not edit by hand.
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The content the container knows (MaintainabilityRefactorContentInjection.md): every content singleton (one instance per build,
    /// built like a service), one line each, sorted by name, and every <c>Content.Create</c> signature of the game's code, which the
    /// reload validation checks. Made content is never registered: <see cref="ContentFactory"/> builds it.
    /// </summary>
    internal static class ContentRegistrations
    {
        internal static void Register(IServiceCollection services)
        {
        }

        internal static IEnumerable<ContentSignature> Signatures()
        {
            yield break;
        }
    }
}
