using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.packets;

namespace grid_shared.grid.network
{
    public interface IPacketHandler
    {
        void HandleDisconnect(PacketWorkerDisconnect packet);
    }
}
