using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grid_shared.grid.network
{
    public interface IPacket
    {
        void Read(PacketBuffer buffer);

        void Write(PacketBuffer buffer);

        void Handle(IPacketHandler handler);
    }
}
