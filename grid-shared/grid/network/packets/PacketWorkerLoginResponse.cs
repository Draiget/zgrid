using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerLoginResponse : IPacket
    {
        public static byte[] ResponseHandshake = { 0xFA, 0xFC, 0xAF, 0xFA, 0xAB, 0xEB, 0x54, 0x54, 0xCC };

        private byte[] _handshake;
        private bool _isAccepted;
        private string _denyReason;

        public PacketWorkerLoginResponse(bool isAccepted, string denyReason = null) {
            _handshake = ResponseHandshake;
            _isAccepted = isAccepted;
            _denyReason = denyReason ?? "";
        }

        public void Read(PacketBuffer buffer) {
            _handshake = buffer.ReadBytes(ResponseHandshake.Length);
            _isAccepted = buffer.ReadBoolean();
            _denyReason = buffer.ReadString();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_handshake);
            buffer.Write(_isAccepted);
            buffer.Write(_denyReason);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerClient)handler).HandleLoginResponse(this);
        }

        public bool IsValidHandshake() {
            return PacketWorkerLoginRequest.Handshake.SequenceEqual(_handshake);
        }

        public bool IsAccepted() {
            return _isAccepted;
        }

        public string GetDenyReason() {
            return _denyReason;
        }
    }
}
