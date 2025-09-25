using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Shared.Domain.Dialog.Contracts
{
    public interface IConversation
    {
        PlayerDialog SetUp(uint player);
    }
}
