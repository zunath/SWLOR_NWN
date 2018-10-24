using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IDataSyncViewModel
    {
        IDatabaseConnectionViewModel DatabaseConnectionVM { get; set; }
        bool ControlsEnabled { get; set; }
    }
}
