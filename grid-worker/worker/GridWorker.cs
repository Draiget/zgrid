using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_shared.grid.network.packets;
using grid_shared.grid.tasks;
using grid_shared.grid.utils;
using grid_worker.worker.network;
using log4net;

namespace grid_worker.worker
{
    public class GridWorker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridWorker));

        public const string SettingFileName = "worker.config.json";

        private readonly WorkerNetworkSystem _networkSystem;
        private GridJobTask _activeTask;
        private ManualResetEvent _taskWaitEvent;

        public GridWorkerSettings Settings {
            get;
            private set;
        }

        public GridWorker() {
            _networkSystem = new WorkerNetworkSystem(this);
            _taskWaitEvent = new ManualResetEvent(false);
        }

        public void Init() {
            if (!File.Exists(SettingFileName)) {
                Settings = GridWorkerSettings.Defaults;
                File.WriteAllText(SettingFileName, GridWorkerSettings.Serialize(Settings));
            } else {
                try {
                    Settings = GridWorkerSettings.Deserialize(File.ReadAllText(SettingFileName));
                } catch (Exception e) {
                    Settings = GridWorkerSettings.Defaults;
                    Logger.Error($"Unable to read settings from '{SettingFileName}', loading defaults", e);
                }
            }

            _networkSystem.Init();
        }

        public void TickNetwork() {
            _networkSystem.Tick();
        }

        public void Shutdown() {
            _networkSystem.Shutdown();
        }

        public void TickMain() {
            _taskWaitEvent.WaitOne();
            if (_activeTask == null) {
                _taskWaitEvent.Reset();
                return;
            }

            if (_activeTask.State == EGridJobTaskState.Created) {
                GridIo.CleanupTasksDirectoriesOnly(_activeTask.ParentJob);
                _networkSystem.CleanupWaitFiles();

                Logger.Info($"New task {_activeTask} is received from server");
                _activeTask.State = EGridJobTaskState.Running;

                foreach (var refFile in _activeTask.ReferencedFiles) {
                    var fileOriginal = refFile.ResolveLink(_activeTask.ParentJob);
                    if (fileOriginal == null) {
                        Logger.Error($"Unable to resolve referenced file {refFile}");
                        _activeTask.State = EGridJobTaskState.RunningFailed;
                        return;
                    }

                    var reqFiles = 0;
                    if (!GridIo.IsJobTaskFileExistsAndValid(_activeTask, fileOriginal)) {
                        _networkSystem.AddWaitFile(fileOriginal);
                        Logger.Info($"Requesting {fileOriginal} from server (not exists locally, or checksum invalid)");
                        _networkSystem.SendPacket(new PacketWorkerFileRequest(_activeTask.TaskId, _activeTask.ParentJob.Name, fileOriginal));
                        reqFiles++;
                    }

                    if (reqFiles > 0) {
                        Logger.Info($"Wait {reqFiles} files to download from server");
                    }
                }

                while (true) {
                    if (_networkSystem.AllWaitFilesReceived()) {
                        Logger.Info("All files for task are received");
                        break;
                    }
                }

                try {
                    Logger.Info($"Processing task {_activeTask}");
                    _activeTask.ExecuteModuleTask(new GridTaskExecutor(), Logger);
                    _activeTask.State = EGridJobTaskState.RunningFinished;
                    Logger.Info($"Task {_activeTask} is finished");
                } catch (GridJobTaskCommandException e) {
                    _activeTask.State = EGridJobTaskState.RunningFailed;
                    Logger.Error("Task execute internal error", e);
                } catch (Exception e) {
                    _activeTask.State = EGridJobTaskState.RunningFailed;
                    Logger.Error("Task execute unexpected error", e);
                }
            }

            if (_activeTask.State == EGridJobTaskState.RunningFinished || _activeTask.State == EGridJobTaskState.RunningFailed) {
                try {
                    if (_activeTask.State == EGridJobTaskState.RunningFinished) {
                        Logger.Info($"Sending task {_activeTask} output files");
                        SendOutputFiles();
                    }

                    _networkSystem.SendPacket(new PacketWorkerTaskFinish(_activeTask));
                } catch (Exception e) {
                    Logger.Error("Unable to send task finished packet", e);
                } finally {
                    _activeTask = null;
                }
            }
        }

        private void SendOutputFiles() {
            var outFiles = _activeTask.ParentJob.JobFiles.Where(x => x.Direction == EGridJobFileDirection.WorkerOutput);
            foreach (var outFile in outFiles) {
                outFile.Bytes = File.ReadAllBytes(GridIo.ResolveFilePath(_activeTask, outFile));
                outFile.CheckSum = CryptoUtils.CrcOfBytes(outFile.Bytes);
                _networkSystem.SendPacket(new PacketWorkerFileData(_activeTask, outFile));
            }
        }

        public bool IsStandbye() {
            return _activeTask == null || _activeTask.State != EGridJobTaskState.Running;
        }

        public bool RunNewTask(GridJobTask task) {
            if (_activeTask != null) {
                return false;
            }

            _activeTask = task;
            _taskWaitEvent.Set();
            return true;
        }

        public bool IsRunningTask() {
            return _activeTask != null && _activeTask.State == EGridJobTaskState.Running;
        }

        public GridJobTask GetActiveTask() {
            return _activeTask;
        }
    }
}
