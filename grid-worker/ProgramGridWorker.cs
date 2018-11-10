using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_shared.grid.utils;
using grid_worker.worker;
using log4net;

namespace grid_worker
{
    public class ProgramGridWorker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridWorker));

        private static Thread _mainThread;
        private static Thread _netThread;
        private static bool _isRequestShutdown;

        private static GridWorker _gridWorker;

        public static void Main(string[] args) {
            var info = VersionUtils.GetAssemblyNameInfo();
            _isRequestShutdown = false;

            Console.Title = "ZGrid Worker";
            Logger.Info($"Starting ZGrid Worker {info.Version.Major}.{info.Version.Minor}.{info.Version.MinorRevision} ({info.ProcessorArchitecture}) ...");
            _gridWorker = new GridWorker();

            _mainThread = new Thread(ThreadMain) { IsBackground = true };
            _netThread = new Thread(ThreadNetwork) { IsBackground = true };

            if (!Directory.Exists("jobs_temp")) {
                Directory.CreateDirectory("jobs_temp");
            }

            try {
                _gridWorker.Init();
                Console.Title = $"ZGrid Worker: {_gridWorker.Settings.WorkerName}";
                Logger.Info("Server is initialized");
            } catch (Exception e) {
                Logger.Error("Unable to initialize ZGrid Worker", e);
                return;
            }

            _mainThread.Start();
            _netThread.Start();

            Logger.Info("Type 'exit' to exit from application");
            
            string input;
            while ((input = Console.ReadLine()) != null) {
                if (string.IsNullOrEmpty(input)) {
                    continue;
                }

                if (input.Equals("exit")) {
                    Shutdown();
                    return;
                }
            }
        }

        private static void Shutdown() {
            _isRequestShutdown = true;
        }

        public static void ThreadMain() {
            while (!_isRequestShutdown) {
                try {
                    _gridWorker.TickMain();
                } catch (Exception e) {
                    Logger.Error("Unexpected error occured on worker tick", e);
                    Thread.Sleep(500);
                }
            }
        }

        public static void ThreadNetwork() {
            while (true) {
                if (_isRequestShutdown) {
                    try {
                        _gridWorker?.Shutdown();
                    } catch (Exception e) {
                        Logger.Error("Unable to gracefuly shutdown worker", e);
                    }

                    return;
                }

                _gridWorker?.TickNetwork();
            }
        }
    }
}
