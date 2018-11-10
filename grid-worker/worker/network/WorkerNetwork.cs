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
using grid_shared.grid.tasks;
using log4net;

namespace grid_worker.worker.network
{
    public class WorkerNetwork
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridWorker));

        private readonly NetworkStream _netStream;
        private readonly WorkerNetworkSystem _networkSystem;
        private readonly TcpClient _tcpClient;
        private IPacketHandler _currentHandler;
        private readonly GridWorker _gridWorker;
        private long _checkDisconnectTime;

        public WorkerNetwork(WorkerNetworkSystem system, TcpClient client, GridWorker worker) {
            _networkSystem = system;
            _gridWorker = worker;
            _tcpClient = client;
            _netStream = client.GetStream();
            _checkDisconnectTime = DateTime.Now.Ticks;
            _currentHandler = new WorkerNetworkHandler(this, system);
        }

        public bool IsLoggedOnServer() {
            return ((WorkerNetworkHandler) _currentHandler).IsLogged();
        }

        public void CheckTimedOut() {
            /*if (DateTime.Now.Ticks - _checkDisconnectTime < TimeSpan.TicksPerMillisecond * 500) {
                return;
            }

            _checkDisconnectTime = DateTime.Now.Ticks;

            if (_tcpClient == null || !_tcpClient.Client.Poll(0, SelectMode.SelectWrite)) {
                OneSideDisconnect("Server timeout");
            }*/
        }

        public void ChannelRead() {
            var timeout = DateTime.Now.AddMilliseconds(10);
            while (IsChannelOpen() && _tcpClient.Available != 0 && DateTime.Now < timeout) {
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

                var packet = (IPacket)Activator.CreateInstance(packetType);
                packet.Read(reader);

                if (tempBuffer.Position < initPos + packetLen) {
                    Logger.Error($"Packet (id: {packetId}) not fully readed ({tempBuffer.Position} < {initPos + packetLen})");
                    return null;
                }

                return packet;
            }
        }

        public bool IsChannelOpen() {
            return _tcpClient != null && _tcpClient.Connected && _tcpClient.Client.Connected;
        }

        public void ProcessPacket(IPacket packet) {
            if (!IsChannelOpen()) {
                return;
            }

            if (packet == null) {
                return;
            }

            var handler = _currentHandler;
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

        public void SetPacketHandler(IPacketHandler handler) {
            _currentHandler = handler;
        }

        public void SendPacket(IPacket packet) {
            var writer = new BinaryWriter(_netStream);
            using (var ms = new PacketBuffer(new MemoryStream())) {
                var packetId = PacketsRegistry.GetPacketIdByType(packet.GetType());
                packet.Write(ms);

                var buffer = ((MemoryStream) ms.BaseStream).ToArray();
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

        public GridWorker GetWorker() {
            return _gridWorker;
        }

        public GridJobTask GetActiveTask() {
            return _gridWorker.GetActiveTask();
        }
    }
}
