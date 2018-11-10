using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.packets;

namespace grid_shared.grid.network.handlers
{
    public interface INetHandlerWorkerClient : IPacketHandler
    {
        void HandleLoginResponse(PacketWorkerLoginResponse packet);

        void HandleTaskRequest(PacketWorkerTaskRequest packet);

        void HandleWorkerRequestStatus(PacketWorkerRequestStatus packet);
    }
}
