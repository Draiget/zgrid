using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_server.server.network;
using grid_shared.grid.tasks;
using log4net;

namespace grid_server.server
{
    public class GridServer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        public const string SettingFileName = "server.config.json";
        private readonly GridServerNetworkSystem _networkSystem;

        private List<GridJobTask> _lockedTasks;
        private volatile GridJob _activeJob;
        private Queue<GridJob> _jobsQueue;

        private ManualResetEvent _jobWaitHandle;

        public GridServerSettings Settings {
            get;
            private set;
        }

        public GridServer() {
            _networkSystem = new GridServerNetworkSystem(this);
            _jobsQueue = new Queue<GridJob>();
            _lockedTasks = new List<GridJobTask>();
            _activeJob = null;

            _jobWaitHandle = new ManualResetEvent(false);
        }

        public void Init() {
            if (!File.Exists(SettingFileName)) {
                Settings = GridServerSettings.Defaults;
                File.WriteAllText(SettingFileName, GridServerSettings.Serialize(Settings));
            } else {
                try {
                    Settings = GridServerSettings.Deserialize(File.ReadAllText(SettingFileName));
                } catch (Exception e) {
                    Settings = GridServerSettings.Defaults;
                    Logger.Error($"Unable to read settings from '{SettingFileName}', loading defaults", e);
                }
            }

            _networkSystem.Init();
            _networkSystem.StartListen();
        }

        public void TickNetwork() {
            _networkSystem.Tick();
        }

        public void Shutdown() {
            _networkSystem.Shutdown();
        }

        public void TickMain() {
            if (!_networkSystem.IsInitialized()) {
                return;
            }

            if (_activeJob == null) {
                if (!GetNewJobFromQueue()) {
                    // No new jobs, just wait more time
                    _jobWaitHandle.WaitOne();
                    _jobWaitHandle = null;
                }

                return;
            }

            if (_jobWaitHandle == null) {
                _jobWaitHandle = new ManualResetEvent(false);
            }

            TickActiveJob();
        }

        private void TickActiveJob() {
            var taskToExec = _activeJob.JobTasks.FirstOrDefault(x => x.State == EGridJobTaskState.Created && !_lockedTasks.Contains(x));
            if (taskToExec == null) {
                if (CheckAllTasksCompleted()) {
                    FinishCurrentJob();
                    _activeJob = null;
                    return;
                }

                // Wait for all tasks to be completed
                return;
            }

            lock (_networkSystem.NetClients) {
                var firstFreeClient = _networkSystem.NetClients.FirstOrDefault(x => x.CanAcceptNewTask());
                if (firstFreeClient == null) {
                    return;
                }

                Logger.Info($"Sendind task execute request {taskToExec} to client {firstFreeClient}");
                _lockedTasks.Add(taskToExec);
                firstFreeClient.RequestExecTask(taskToExec);
            }
        }

        public void UnlockTask(GridJobTask task) {
            if (_lockedTasks.Contains(task)) {
                _lockedTasks.Remove(task);
            }
        }

        private void FinishCurrentJob() {
            try {
                var firstTask = _activeJob.JobTasks.FirstOrDefault(x => x != null);
                firstTask?.ExecuteModuleFinishJob(Logger);
            } catch (Exception e) {
                Logger.Error("Unable to execute job module finish callback", e);
            }

            var failedTasks = _activeJob.JobTasks.Count(x => x.State == EGridJobTaskState.RunningFailed);
            Logger.Info($"Job '{_activeJob}' finished [FailedTasks={failedTasks}]");

            var outFiles = _activeJob.JobFiles.Where(x => x.Direction == EGridJobFileDirection.WorkerOutput && x.ShareMode == EGridJobFileShare.SharedBetweenTasks);
            foreach (var outFile in outFiles) {
                if (outFile.Bytes != null && outFile.Bytes.Length > 0) {
                    Logger.Info($"Output of the file '{outFile}': {Encoding.UTF8.GetString(outFile.Bytes)}");
                    continue;
                }

                Logger.Info($"Output of the file '{outFile}' is empty");
            }
        }

        private bool GetNewJobFromQueue() {
            lock (_jobsQueue) {
                if (_jobsQueue.Count > 0) {
                    _activeJob = _jobsQueue.Dequeue();
                    return true;
                }
            }

            return false;
        }

        public void RunQueueJob(GridJob job) {
            lock (_jobsQueue) {
                if (_jobsQueue.Count == 0) {
                    // Skip the queue and set directly
                    _activeJob = job;
                    _jobWaitHandle.Set();
                    return;
                }

                _jobsQueue.Enqueue(job);
                _jobWaitHandle.Set();
            }
        }

        private bool CheckAllTasksCompleted() {
            return _activeJob.JobTasks.Count(
                       x => x.State == EGridJobTaskState.RunningFinished || x.State == EGridJobTaskState.RunningFailed) 
                   == _activeJob.JobTasks.Count;
        }

        public bool IsAnyJobActive() {
            return _activeJob != null;
        }

        public GridJob GetActiveJob() {
            return _activeJob;
        }

        public int GetWorkersCount() {
            lock (_networkSystem.NetClients) {
                return _networkSystem.NetClients.Count;
            }
        }
    }
}
