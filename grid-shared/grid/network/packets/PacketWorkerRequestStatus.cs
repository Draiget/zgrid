using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerRequestStatus : IPacket
    {
        private bool _isRunningTask;

        public PacketWorkerRequestStatus() {
            
        }

        public PacketWorkerRequestStatus(bool isRunningTask) {
            _isRunningTask = isRunningTask;
        }

        public void Read(PacketBuffer buffer) {
            _isRunningTask = buffer.ReadBoolean();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_isRunningTask);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerClient) handler).HandleWorkerRequestStatus(this);
        }

        public bool IsStandby() {
            return !_isRunningTask;
        }
    }
}
