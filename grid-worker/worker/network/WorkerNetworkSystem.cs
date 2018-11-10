using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_shared.grid.network;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using grid_shared.grid.tasks;
using log4net;

namespace grid_worker.worker.network
{
    public class WorkerNetworkSystem
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridWorker));

        private bool _isInitialized;
        private readonly GridWorker _gridWorker;
        private WorkerNetwork _workerNetwork;

        private TcpClient _tcpClient;
        private IPAddress _remoteAddress;

        private List<GridJobFile> _filesToDownload;

        public WorkerNetworkSystem(GridWorker gridWorker) {
            _isInitialized = false;
            _gridWorker = gridWorker;
            _filesToDownload = new List<GridJobFile>();
        }

        public void Init() {
            PacketsRegistry.Initialize();
            _isInitialized = true;
            Logger.Info("Network is initialized");
        }

        private bool _repeatRefuseMsg = true;

        public void Tick() {
            if (!_isInitialized) {
                return;
            }

            while (!IsConnected()) {
                try {
                    _remoteAddress = Dns.GetHostAddresses(_gridWorker.Settings.ServerAddress).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    if (_remoteAddress == null) {
                        throw new Exception($"Unable to lookup ip for remote '{_gridWorker.Settings.ServerAddress}'");
                    }

                    _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0)) {
                        ReceiveTimeout = _gridWorker.Settings.ReceiveTimeout,
                        NoDelay = true
                    };

                    _tcpClient.Connect(new IPEndPoint(_remoteAddress, (int) _gridWorker.Settings.ServerPort));
                    _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    _workerNetwork = new WorkerNetwork(this, _tcpClient, _gridWorker);
                    Logger.Debug("Server is connected to remote grid, wait for logon");

                    _workerNetwork.SendPacket(new PacketWorkerLoginRequest(_gridWorker.Settings.WorkerName));
                    _repeatRefuseMsg = true;
                } catch (SocketException se) {
                    if (_repeatRefuseMsg) {
                        if (se.SocketErrorCode == SocketError.ConnectionRefused) {
                            Logger.Warn("Remote server refused connection, maybe not running (trying to connect in background)");
                        } else {
                            Logger.Warn("Unable to connect to main server (trying to connect in background)", se);
                        }

                        _repeatRefuseMsg = false;
                    }

                    Thread.Sleep(_gridWorker.Settings.ConnectRepeatInterval);
                } catch (Exception e) {
                    Logger.Warn("Unable to connect to main server", e);
                    Thread.Sleep(_gridWorker.Settings.ConnectRepeatInterval);
                }
            }

            if (_workerNetwork == null) {
                _tcpClient.Close();
                return;
            }

            try {
                _workerNetwork.ChannelRead();
                _workerNetwork.CheckTimedOut();

                if (!IsConnected()) {
                    Logger.Info("Disconnected from server: Read timeout");
                }
            } catch (IOException e) {
                if (_workerNetwork.IsLoggedOnServer()) {
                    ForceDisconnect();
                    return;
                }

                Logger.Error("IO Error during worker network channel read", e);
            }
        }

        public bool IsConnected() {
            return _tcpClient != null && _tcpClient.Connected && _tcpClient.Client.Connected;
        }

        public void Shutdown() {
            if (!IsConnected()) {
                return;
            }

            if (_workerNetwork == null) {
                return;
            }

            try {
                SendPacket(new PacketWorkerDisconnect("client exit"));
            } catch {
                ;
            } finally {
                _workerNetwork = null;
            }
        }

        public void SendPacket(IPacket packet) {
            _workerNetwork?.SendPacket(packet);
        }

        public void ForceDisconnect() {
            if (IsConnected()) {
                _tcpClient.Close();
            }
        }

        public void AddWaitFile(GridJobFile file) {
            _filesToDownload.Add(file);
        }

        public bool AllWaitFilesReceived() {
            return _filesToDownload.Count == 0;
        }

        public void CleanupWaitFiles() {
            _filesToDownload.Clear();
        }

        public void WaitFileReceived(GridJobFile file) {
            _filesToDownload.Remove(file);
        }
    }
}
