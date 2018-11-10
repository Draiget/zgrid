using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using grid_shared.grid.tasks;
using log4net;

namespace grid_server.server.network.handlers
{
    public class NetHandlerGridServer : INetHandlerWorkerServer, INetHandlerFiles
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        private readonly GridServerNetworkSystem _netSystem;
        private readonly GridNetClientManager _clientNetManager;
        private readonly GridServer _gridServer;
        private GridNetClient _netClient;

        public NetHandlerGridServer(GridServer server, GridServerNetworkSystem netSystem, GridNetClientManager clientManager) {
            _gridServer = server;
            _netSystem = netSystem;
            _clientNetManager = clientManager;
        }

        public void HandleDisconnect(PacketWorkerDisconnect packet) {
            lock (_netSystem.NetClients) {
                _netSystem.NetClients.Remove(_netClient);
                Logger.Info($"Client {_netClient} disconnected: {packet.Reason ?? "unknown"}");
            }
        }
        
        public void HandleLoginRequest(PacketWorkerLoginRequest packet) {
            if (!packet.IsValidHandshake()) {
                lock (_netSystem.NetClients) {
                    _netSystem.Disconnect(_clientNetManager.GetTcpClient(), "Invalid login handshake");
                }

                return;
            }

            lock (_netSystem.NetClients) {
                var hasSame = _netSystem.NetClients.FirstOrDefault(x => x.WorkerName == packet.GetWorkerName());
                if (hasSame != null) {
                    hasSame.Disconnect("Logged from other location");
                    Logger.Info($"Duplicate worker {packet.GetWorkerName()}, disconnecting old one");
                }

                _netClient = new GridNetClient(_gridServer, _clientNetManager, _netSystem, packet.GetWorkerName());
                _netSystem.NetClients.Add(_netClient);
            }

            Logger.Info($"Client {_netClient} connected from [{_netClient.RemoteAddress}]");
        }

        public void HandleTaskResponse(PacketWorkerTaskResponse packet) {
            if (!packet.IsAccepted()) {
                _netClient.WorkerCancelActiveTask(packet.GetTaskId(), packet.GetJobName());
                return;
            }

            _netClient.WorkerAcceptTask(packet.GetTaskId(), packet.GetJobName());
        }

        public void HandleWorkerStatusResponse(PacketWorkerResponseStatus packet) {
            _netClient?.SetRemoteStatus(packet.IsStandby());
        }

        public void HandleFileRequest(PacketWorkerFileRequest packet) {
            var task = _netClient.GetActiveTask();
            if (task == null) {
                return;
            }

            GridJobFile reqFile;

            var shareType = packet.GetShareType();
            if (shareType == EGridJobFileShare.PerEachTask) {
                var fileLink = task.ReferencedFiles.FirstOrDefault(x => packet.GetRequestFile() == x.FileName && packet.GetRequestFileCheckSum() == x.CheckSum);
                if (fileLink == null) {
                    Logger.Error($"Worker request unexisting file {packet.GetRequestFile()}");
                    return;
                }

                reqFile = fileLink.ResolveLink(task.ParentJob);
            } else {
                reqFile = task.ParentJob.JobFiles.FirstOrDefault(x => packet.GetRequestFile() == x.FileName && packet.GetRequestFileCheckSum() == x.CheckSum);
                if (reqFile == null) {
                    Logger.Error($"Worker request unexisting file {packet.GetRequestFile()}");
                    return;
                }
            }
           
            Logger.Info($"Request file {reqFile} from {_netClient}, sending data");
            _netClient.SendPacket(new PacketWorkerFileData(task, reqFile));
        }

        public void HandleFileData(PacketWorkerFileData packet) {
            _netClient.AcceptFile(packet.GetTaskId(), packet.GetJobName(), packet.GetFile());
        }

        public void HandleTaskFinish(PacketWorkerTaskFinish packet) {
            _netClient.WorkerTaskFinished(packet.GetTaskId(), packet.GetJobName(), packet.GetState());
        }

        public void HandleTaskCancel(PacketWorkerTaskCancel packet) {
            _netClient.WorkerCancelActiveTask(packet.GetTaskId(), packet.GetJobName());
        }
    }
}
