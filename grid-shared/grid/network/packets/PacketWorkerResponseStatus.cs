using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerResponseStatus : PacketWorkerRequestStatus
    {
        public PacketWorkerResponseStatus(bool isRunningTasks)
            : base(isRunningTasks) {
        }

        public new void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleWorkerStatusResponse(this);
        }
    }
}
