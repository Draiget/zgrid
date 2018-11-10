using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using log4net;

namespace grid_client.client.network
{
    public class GridClientNetworkHandler : INetHandlerFiles
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridClient));

        private readonly GridClientNetwork _network;
        private readonly GridClientNetworkSystem _netSystem;
        private bool _isLogged;

        public GridClientNetworkHandler(GridClientNetwork network, GridClientNetworkSystem netSystem) {
            _network = network;
            _netSystem = netSystem;
            _isLogged = false;
        }

        public void HandleDisconnect(PacketWorkerDisconnect packet) {
            throw new NotImplementedException();
        }

        public void HandleFileData(PacketWorkerFileData packet) {
            throw new NotImplementedException();
        }
    }
}
