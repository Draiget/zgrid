using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_server.server.network.handlers;
using grid_shared.grid.network;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using log4net;

namespace grid_server.server.network
{
    public class GridServerNetworkSystem
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        private bool _isInitialized;
        private bool _isRequestShutdown;

        private readonly GridServer _gridServer;

        private TcpListener _tcpListener;
        public List<GridNetClient> NetClients;

        public GridServerNetworkSystem(GridServer server) {
            _isRequestShutdown = false;
            NetClients = new List<GridNetClient>();
            _gridServer = server;
        }

        public void Init() {
            PacketsRegistry.Initialize();

            _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Parse(_gridServer.Settings.BindAddress), (int)_gridServer.Settings.BindPort));
            _tcpListener.AllowNatTraversal(true);

            Logger.Info("Network is initialized");
        }

        public void StartListen() {
            _tcpListener.Start(10);
            Logger.Info($"Network listening [tcp={_gridServer.Settings.BindAddress}]: {_gridServer.Settings.BindPort}");
            _tcpListener.BeginAcceptTcpClient(AcceptClientAsync, null);
            _isInitialized = true;
        }

        private void AcceptClientAsync(IAsyncResult result) {
            if (_tcpListener == null) {
                return;
            }

            try {
                var netManager = new GridNetClientManager(_tcpListener.EndAcceptTcpClient(result));
                netManager.GetTcpClient().Client.ReceiveTimeout = _gridServer.Settings.ReceiveTimeout;
                netManager.SetNetHandler(new NetHandlerGridServer(_gridServer, this, netManager));
                Logger.Debug($"Accept client from {netManager.GetTcpClient().Client.RemoteEndPoint as IPEndPoint}");

                var task = new Task(() => {
                    while (netManager.IsChannelOpen()) {
                        try {
                            netManager.ChannelRead();
                        } catch (IOException ioerr) {
                            Logger.Error("Unknown exception during packet reading", ioerr);
                        }
                    }
                });
                task.Start();
            } catch (Exception e) {
                if (_isRequestShutdown) {
                    return;
                }

                Logger.Error("Unable to accept new network worker client", e);
            } finally {
                try {
                    _tcpListener?.BeginAcceptTcpClient(AcceptClientAsync, null);
                } catch {
                    ;
                }
            }
        }

        public void Tick() {
            lock (NetClients) {
                foreach (var client in NetClients) {
                    if (client.IsClientFullyDisconnected()) {
                        continue;
                    }

                    client.NetTick();
                }

                NetClients.RemoveAll(x => x.IsClientFullyDisconnected());
            }
        }

        public void Shutdown() {
            if (_isInitialized) {
                _isRequestShutdown = true;

                try {
                    lock (NetClients) {
                        foreach (var client in NetClients) {
                            client.Disconnect("Server closing");
                        }
                    }

                    Thread.Sleep(1000);
                } catch {
                    ;
                }

                _tcpListener?.Stop();
            }
        }

        public void BroadcastPacket(IPacket packet) {
            lock (NetClients) {
                foreach (var client in NetClients) {
                    client.SendPacket(packet);
                }
            }
        }

        public void SendPacket(IPacket packet, NetworkStream netStream) {
            var writer = new BinaryWriter(netStream);
            using (var ms = new PacketBuffer(new MemoryStream())) {
                var packetId = PacketsRegistry.GetPacketIdByType(packet.GetType());
                packet.Write(ms);

                var buffer = ((MemoryStream)ms.BaseStream).ToArray();
                writer.Write(packetId);
                writer.Write(buffer.Length);
                writer.Write(buffer);
                netStream.Flush();
            }
        }

        public void Disconnect(GridNetClient client, string reason) {
            var netManager = client.GetNetworkManager();
            if (netManager == null) {
                return;
            }

            client.OnDisconnect(reason);
            Disconnect(netManager.GetTcpClient(), reason);
        }

        public void Disconnect(TcpClient client, string reason) {
            if (!client.Connected && client.Client == null || !client.Client.Connected) {
                return;
            }
            
            var stream = client.GetStream();
            if (stream.CanWrite) {
                try {
                    SendPacket(new PacketWorkerDisconnect(reason), client.GetStream());
                } catch {
                    ;
                }
            }
        }

        public bool IsInitialized() {
            return _isInitialized;
        }

        public GridServer GetServer() {
            return _gridServer;
        }
    }
}
