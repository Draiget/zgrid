using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerDisconnect : IPacket
    {
        public string Reason;

        public PacketWorkerDisconnect() {
            
        }

        public PacketWorkerDisconnect(string reason) {
            Reason = reason;
        }

        public void Read(PacketBuffer buffer) {
            Reason = buffer.ReadString();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(Reason);
        }

        public void Handle(IPacketHandler handler) {
            handler.HandleDisconnect(this);
        }
    }
}
