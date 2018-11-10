using System.Net;
using System.Net.Sockets;
using grid_shared.grid.network;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using log4net;

namespace grid_client.client.network
{
    public class GridClientNetworkSystem
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridClient));

        private readonly GridClient _gridClient;
        private GridClientNetwork _clientNetwork;

        private TcpClient _tcpClient;
        private bool _isInitialized;

        public GridClientNetworkSystem(GridClient client) {
            _gridClient = client;
            _isInitialized = false;
        }

        public void Init() {
            PacketsRegistry.Initialize();

            _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0));

            _isInitialized = true;
            Logger.Info("Network is initialized");
        }


        public void Tick() {
            if (!_isInitialized) {
                return;
            }
        }

        public bool IsConnected() {
            return _tcpClient != null && _tcpClient.Connected && _tcpClient.Client.Connected;
        }

        public void Shutdown() {
            if (!IsConnected()) {
                return;
            }

            if (_clientNetwork == null) {
                return;
            }

            try {
                SendPacket(new PacketWorkerDisconnect("client exit"));
            } catch {
                ;
            } finally {
                _clientNetwork = null;
            }
        }

        public void SendPacket(IPacket packet) {
            _clientNetwork?.SendPacket(packet);
        }

        public void ForceDisconnect() {
            if (IsConnected()) {
                _tcpClient.Close();
            }
        }
    }
}
