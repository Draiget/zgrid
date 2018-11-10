using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.packets;

namespace grid_shared.grid.network.handlers
{
    public interface INetHandlerWorkerServer : IPacketHandler
    {
        void HandleLoginRequest(PacketWorkerLoginRequest packet);

        void HandleTaskResponse(PacketWorkerTaskResponse packet);

        void HandleWorkerStatusResponse(PacketWorkerResponseStatus packet);

        void HandleFileRequest(PacketWorkerFileRequest packet);

        void HandleTaskFinish(PacketWorkerTaskFinish packet);

        void HandleTaskCancel(PacketWorkerTaskCancel packet);
    }
}
