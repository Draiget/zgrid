using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network;
using grid_shared.grid.network.packets;
using grid_shared.grid.tasks;
using log4net;

namespace grid_server.server.network
{
    public class GridNetClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        private readonly GridNetClientManager _clientNetManager;
        private readonly GridServerNetworkSystem _gridServerNetwork;
        private readonly GridServer _gridServer;
        private GridJobTask _activeTask;

        private readonly List<GridJobFile> _tempReceivedFiles;

        private delegate void HandleTaskCallback(bool isAccepts);

        private bool _isDisconnecting;
        private bool _shouldRemove;
        private long _checkDisconnectTime;

        public GridNetClient(GridServer server, GridNetClientManager netManager, GridServerNetworkSystem netSystem, string workerName) {
            WorkerName = workerName;
            _gridServer = server;
            _gridServerNetwork = netSystem;
            _clientNetManager = netManager;
            _tempReceivedFiles = new List<GridJobFile>();
            IsAcceptingNewTasks = true;
            _isDisconnecting = false;
            _shouldRemove = false;
            _checkDisconnectTime = DateTime.Now.Ticks;
        }

        public IPEndPoint RemoteAddress => _clientNetManager.GetTcpClient().Client.RemoteEndPoint as IPEndPoint;

        public string WorkerName {
            get;
            private set;
        }

        public bool IsAcceptingNewTasks {
            get;
            private set;
        }
        
        public void NetTick() {
            CheckDisconnected();
        }

        public void SetRemoteStatus(bool status) {
            if (_isDisconnecting) {
                return;
            }

            IsAcceptingNewTasks = status;
        }

        public void OnDisconnect(string reason = "Unknown") {
            _isDisconnecting = true;
            _shouldRemove = true;
            _clientNetManager.SetFullyDisconnected(true);
            Logger.Info($"Client {this} disconnected: {reason}");
        }

        private void CheckDisconnected() {
            if (DateTime.Now.Ticks - _checkDisconnectTime < TimeSpan.TicksPerMillisecond * 500) {
                return;
            }

            _checkDisconnectTime = DateTime.Now.Ticks;

            if (!_clientNetManager.IsChannelOpen()) {
                Disconnect("Timed out, channel closed");
                return;
            }

            if (_clientNetManager.CheckIfTimeout()) {
                Disconnect("Timed out");
            }
        }

        public override string ToString() {
            return $"{WorkerName}";
        }

        public GridNetClientManager GetNetworkManager() {
            return _clientNetManager;
        }

        public void Disconnect(string reason) {
            if (_gridServerNetwork != null) {
                _gridServerNetwork.Disconnect(this, reason);

                if (_activeTask != null && _activeTask.State == EGridJobTaskState.Running) {
                    Logger.Info($"Client has unfinished task {_activeTask}, unlocking task for other workers");
                    _activeTask.State = EGridJobTaskState.Created;
                    _gridServer.UnlockTask(_activeTask);
                }
            }
        }

        public bool IsClientFullyDisconnected() {
            return _shouldRemove;
        }

        public void SendPacket(IPacket packet) {
            _gridServerNetwork.SendPacket(packet, _clientNetManager.GetNetworkStream());
        }

        public bool CanAcceptNewTask() {
            return IsAcceptingNewTasks && (_activeTask == null || _activeTask.State != EGridJobTaskState.Running && _activeTask.State != EGridJobTaskState.Created);
        }

        public void RequestExecTask(GridJobTask task) {
            _activeTask = task;
            IsAcceptingNewTasks = false;
            SendPacket(new PacketWorkerTaskRequest(task));
        }

        public void WorkerCancelActiveTask(uint taskId, string jobName) {
            if (_activeTask == null) {
                return;
            }

            if (_activeTask.ParentJob.Name == jobName && _activeTask.TaskId == taskId) {
                _gridServer.UnlockTask(_activeTask);
                IsAcceptingNewTasks = !_isDisconnecting;
                _activeTask = null;
            }
        }

        public void WorkerAcceptTask(uint taskId, string jobName) {
            if (_activeTask.ParentJob.Name == jobName && _activeTask.TaskId == taskId) {
                _tempReceivedFiles.Clear();
                _activeTask.State = EGridJobTaskState.Running;
                _gridServer.UnlockTask(_activeTask);
                IsAcceptingNewTasks = false;
                return;
            }

            Logger.Warn($"Worker {this} accepts not own active task '{_activeTask}', something went wrong (response: [taskId={taskId}, jobName={jobName}])");
        }

        public void WorkerTaskFinished(uint taskId, string jobName, EGridJobTaskState state) {
            if (_activeTask.ParentJob.Name == jobName && _activeTask.TaskId == taskId) {
                _activeTask.State = state;

                var localSaved = _tempReceivedFiles.FirstOrDefault(x => x.Direction == EGridJobFileDirection.WorkerOutput && x.ShareMode == EGridJobFileShare.PerEachTask);
                if (localSaved != null) {
                    _activeTask.OutputFile = new GridJobFile {
                        Bytes = localSaved.Bytes,
                        CheckSum = localSaved.CheckSum,
                        FileName = localSaved.FileName,
                        Direction = EGridJobFileDirection.WorkerOutput,
                        ShareMode = EGridJobFileShare.PerEachTask
                    };
                }

                IsAcceptingNewTasks = !_isDisconnecting;
                Logger.Info($"Worker {this} finish task {_activeTask}");
                return;
            }

            Logger.Warn($"Worker {this} finish not own active task '{_activeTask}', something went wrong (response: [taskId={taskId}, jobName={jobName}])");
        }

        public void AcceptFile(uint taskId, string jobName, GridJobFile file) {
            if (_activeTask.TaskId == taskId && _activeTask.ParentJob.Name == jobName) {
                Logger.Info($"Accept file {file} for task {taskId} [Job={jobName}]");
                _tempReceivedFiles.Add(file);
                return;
            }

            Logger.Warn($"Worker {this} send file {file} that not belongs to the task '{_activeTask}', something went wrong");
        }

        public GridJobTask GetActiveTask() {
            return _activeTask;
        }
    }
}
