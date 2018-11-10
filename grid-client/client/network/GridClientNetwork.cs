using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using log4net;

namespace grid_client.client.network
{
    public class GridClientNetwork
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridClient));

        private readonly NetworkStream _netStream;
        private readonly GridClientNetworkSystem _networkSystem;
        private readonly TcpClient _tcpClient;
        private IPacketHandler _currentHandler;
        private readonly GridClient _gridClient;
        private long _checkDisconnectTime;

        public GridClientNetwork(GridClientNetworkSystem system, TcpClient client, GridClient gridClient) {
            _networkSystem = system;
            _gridClient = gridClient;
            _tcpClient = client;
            _netStream = client.GetStream();
            _checkDisconnectTime = DateTime.Now.Ticks;
            _currentHandler = new GridClientNetworkHandler(this, system);
        }

        public void SetPacketHandler(IPacketHandler handler) {
            _currentHandler = handler;
        }

        public void SendPacket(IPacket packet) {
            var writer = new BinaryWriter(_netStream);
            using (var ms = new PacketBuffer(new MemoryStream())) {
                var packetId = PacketsRegistry.GetPacketIdByType(packet.GetType());
                packet.Write(ms);

                var buffer = ((MemoryStream)ms.BaseStream).ToArray();
                writer.Write(packetId);
                writer.Write(buffer.Length);
                writer.Write(buffer);
                _netStream.Flush();
            }
        }

        public void Disconnect(string reason = null) {
            if (_networkSystem.IsConnected()) {
                SendPacket(new PacketWorkerDisconnect(reason));
            }
        }

        public void OneSideDisconnect(string reason = null) {
            if (string.IsNullOrEmpty(reason)) {
                Logger.Info($"Disconnected from server: {reason}");
            }

            if (_networkSystem.IsConnected()) {
                _networkSystem.ForceDisconnect();
            }
        }

        public GridClient GetClient() {
            return _gridClient;
        }

    }
}
