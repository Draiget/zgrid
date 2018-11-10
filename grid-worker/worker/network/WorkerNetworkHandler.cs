using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.network.packets;
using grid_shared.grid.tasks;
using log4net;

namespace grid_worker.worker.network
{
    public class WorkerNetworkHandler : INetHandlerWorkerClient, INetHandlerFiles
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridWorker));

        private readonly WorkerNetwork _network;
        private readonly WorkerNetworkSystem _netSystem;
        private bool _isLogged;

        public WorkerNetworkHandler(WorkerNetwork network, WorkerNetworkSystem netSystem) {
            _network = network;
            _netSystem = netSystem;
            _isLogged = false;
        }

        public void HandleDisconnect(PacketWorkerDisconnect packet) {
            _network.OneSideDisconnect(packet.Reason);
        }

        public void HandleLoginResponse(PacketWorkerLoginResponse packet) {
            if (_isLogged) {
                Logger.Error("Tried to login on remote server twice!");
                return;
            }

            if (!packet.IsValidHandshake()) {
                Logger.Error("Remote server response invalid handshake");
                _network.OneSideDisconnect();
                return;
            }

            if (!packet.IsAccepted()) {
                Logger.Error($"Remote server is decline your connection: {packet.GetDenyReason()}");
                _network.OneSideDisconnect();
            }

            _isLogged = true;
            Logger.Info("Logged on server successfully");
        }

        public void HandleTaskRequest(PacketWorkerTaskRequest packet) {
            var isAccepting = _network.GetWorker().IsStandbye();
            if (!isAccepting) {
                Logger.Info($"Server give a new task {packet.GetTask()}, decline, we are busy");
                _network.SendPacket(new PacketWorkerTaskResponse(packet.GetTask(), false));
                return;
            }

            Logger.Info($"Server give a new task {packet.GetTask()}, accepting");
            _network.SendPacket(new PacketWorkerTaskResponse(packet.GetTask(), true));
            var task = packet.GetTask();
            var job = task.ParentJob;

            foreach (var jobFile in job.JobFiles) {
                if (jobFile.Direction == EGridJobFileDirection.WorkerOutput) {
                    continue;
                }

                if (!GridIo.IsJobTaskFileExistsAndValid(task, jobFile)) {
                    Logger.Debug($"Request file '{jobFile}' from server for task '{task}' (not exists locally)");
                    _network.SendPacket(new PacketWorkerFileRequest(task.TaskId, job.Name, jobFile));
                }
            }
            
            if (!_network.GetWorker().RunNewTask(packet.GetTask())) {
                Logger.Info("Worker is already running a task (or receive packet twice), refuse");
                _network.SendPacket(new PacketWorkerTaskCancel(packet.GetTask()));
            }
        }

        public void HandleFileData(PacketWorkerFileData packet) {
            packet.UpdateCheckSum();
            var reqFile = packet.GetFile();
            var task = _network.GetActiveTask();
            if (task == null) {
                Logger.Warn($"Handle file data '{packet.GetFile()}' when active task is null");
                return;
            }

            if (task.TaskId != packet.GetTaskId() || task.ParentJob.Name != packet.GetJobName()) {
                Logger.Warn($"Received file data for unknown task [Id={packet.GetTaskId()}, JobName={packet.GetJobName()}]");
                return;
            }

            foreach (var fileRef in task.ParentJob.JobFiles) {
                if (fileRef.FileName == reqFile.FileName) {
                    fileRef.Bytes = packet.GetData();
                    fileRef.UpdateCheckSum();

                    Logger.Debug($"Store task '{task}' file '{fileRef}'");
                    GridIo.StoreJobTaskFile(task, fileRef);
                    _netSystem.WaitFileReceived(fileRef);
                    return;
                }
            }
        }

        public void HandleWorkerRequestStatus(PacketWorkerRequestStatus packet) {
            var status = _network.GetWorker().IsRunningTask();
            _network.SendPacket(new PacketWorkerResponseStatus(status));
        }

        public bool IsLogged() {
            return _isLogged;
        }
    }
}
