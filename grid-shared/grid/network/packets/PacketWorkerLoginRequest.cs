using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerLoginRequest : IPacket
    {
        public static byte[] Handshake = { 0xAA, 0xCA, 0xFA, 0xAA, 0xBA, 0xBE, 0x54, 0x54, 0xCC };

        private byte[] _handshake;
        private string _workerName;

        public PacketWorkerLoginRequest() {
            
        }

        public PacketWorkerLoginRequest(string workerName) {
            _workerName = workerName;
        }

        public void Read(PacketBuffer buffer) {
            _handshake = buffer.ReadBytes(Handshake.Length);
            _workerName = buffer.ReadString();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(Handshake);
            buffer.Write(_workerName);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleLoginRequest(this);
        }

        public bool IsValidHandshake() {
            return Handshake.SequenceEqual(_handshake);
        }

        public string GetWorkerName() {
            return _workerName;
        }
    }
}
