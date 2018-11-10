using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using log4net;

namespace grid_shared.grid.network
{
    public abstract class GridNetworkSystemBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GridNetworkSystemBase));

        public void Init() {
            PacketsRegistry.Initialize();
            PostInit();
        }

        public virtual void PostInit() {

        }

        public virtual void StartListen() {

        }
    }
}
