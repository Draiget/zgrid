using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network;
using grid_shared.grid.network.handlers;
using log4net;

namespace grid_server.server.network
{
    public class GridNetClientManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        private readonly TcpClient _tcpClient;
        private IPacketHandler _packetHandler;
        private readonly NetworkStream _netStream;
        private long _lastPacketTime;
        private bool _isFullyDisconnected;

        public GridNetClientManager(TcpClient client) {
            _tcpClient = client;
            _isFullyDisconnected = false;
            _netStream = _tcpClient.GetStream();
        }

        public void SetNetHandler(IPacketHandler handler) {
            _packetHandler = handler;
        }

        public TcpClient GetTcpClient() {
            return _tcpClient;
        }

        public bool IsChannelOpen() {
            return _tcpClient != null && !_isFullyDisconnected && _tcpClient.Connected && _tcpClient.Client.Connected;
        }

        public bool CheckIfTimeout() {
            return _tcpClient != null && _tcpClient.Client.Poll(0, SelectMode.SelectRead);
        }

        public void ChannelRead() {
            if (_isFullyDisconnected) {
                return;
            }

            var timeout = DateTime.Now.AddMilliseconds(10);
            while (_tcpClient != null && _tcpClient.Connected && _tcpClient.Available != 0 && DateTime.Now < timeout) {
                try {
                    var packet = ReadPacket();
                    ProcessPacket(packet);
                } catch (SocketException se) {
                    Logger.Warn("Socket Exception during worker packet read", se);
                    return;
                }
            }
        }

        public IPacket ReadPacket() {
            var netReader = new BinaryReader(_netStream);
            var packetId = netReader.ReadInt32();
            var packetLen = netReader.ReadInt32();

            var packetBuffer = netReader.ReadBytes(packetLen);
            var tempBuffer = new MemoryStream(packetBuffer);
            using (var reader = new PacketBuffer(tempBuffer)) {
                var initPos = tempBuffer.Position;
                if (packetId == -1) {
                    Logger.Debug($"Invalid packet id received {packetId}");
                    return null;
                }

                var packetType = PacketsRegistry.GetPacketById(packetId);
                if (packetType == null) {
                    Logger.Debug($"Unknown packet id received {packetId}");
                    return null;
                }

                var packet = (IPacket) Activator.CreateInstance(packetType);
                packet.Read(reader);

                if (tempBuffer.Position < initPos + packetLen) {
                    Logger.Error($"Packet (id: {packetId}, name: '{packet.GetType().FullName}') not fully readed ({tempBuffer.Position} < {initPos + packetLen})");
                    return null;
                }

                return packet;
            }
        }

        public void ProcessPacket(IPacket packet) {
            if (!IsChannelOpen() || packet == null) {
                return;
            }

            var handler = _packetHandler;
            if (handler == null) {
                Logger.Debug($"Received packet '{packet.GetType().FullName}' has no any handlers");
                return;
            }

            try {
                packet.Handle(handler);
            } catch (Exception e) {
                Logger.Error($"Unable to handle packet '{packet.GetType().FullName}', unexpected error", e);
            }
        }

        public NetworkStream GetNetworkStream() {
            return _netStream;
        }

        public void SetFullyDisconnected(bool status) {
            _isFullyDisconnected = status;
        }
    }
}
