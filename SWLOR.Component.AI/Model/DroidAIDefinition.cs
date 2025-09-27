using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Component.AI.Model
{
    public class DroidAIDefinition: AIBase
    {
        public DroidAIDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }
    }
}
