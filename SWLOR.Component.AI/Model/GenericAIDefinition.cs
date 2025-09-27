using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Component.AI.Model
{
    public class GenericAIDefinition: AIBase
    {
        public GenericAIDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }
    }
}
