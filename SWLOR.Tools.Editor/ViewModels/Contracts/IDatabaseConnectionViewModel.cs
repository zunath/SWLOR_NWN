using System.Security;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IDatabaseConnectionViewModel
    {
        string IPAddress { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Database { get; set; }
        void Connect();
    }
}
