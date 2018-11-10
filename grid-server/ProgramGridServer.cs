using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using grid_server.server;
using grid_server.server.commands;
using grid_shared.grid.utils;
using log4net;

namespace grid_server
{
    public class ProgramGridServer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        private static Thread _mainThread;
        private static Thread _netThread;
        private static bool _isRequestShutdown;

        private static GridServer _gridServer;

        public static void Main(string[] args) {
            var info = VersionUtils.GetAssemblyNameInfo();
            _isRequestShutdown = false;

            Console.Title = "ZGrid Server";
            Logger.Info($"Starting ZGrid Server {info.Version.Major}.{info.Version.Minor}.{info.Version.MinorRevision} ({info.ProcessorArchitecture}) ...");
            _gridServer = new GridServer();

            ConCommand.Register(_gridServer);

            _mainThread = new Thread(ThreadMain) { IsBackground = true };
            _netThread = new Thread(ThreadNetwork) { IsBackground = true };

            try {
                _gridServer.Init();
                Logger.Info("Server is initialized");
            } catch (Exception e) {
                Logger.Error("Unable to initialize ZGrid Server", e);
                return;
            }

            _mainThread.Start();
            _netThread.Start();

            Logger.Info("Type 'exit' to exit from application");

            string input;
            while (!_isRequestShutdown && (input = Console.ReadLine()) != null) {
                if (string.IsNullOrEmpty(input)) {
                    continue;
                }

                var cmdSelf = input.Contains(' ') ? input.Split(' ')[0] : input;

                var cmd = ConCommand.SearchByName(cmdSelf);
                if (cmd != null) {
                    try {
                        cmd.Execute(input.SlitToArgsFrom(1));
                    } catch (Exception e) {
                        Logger.Error($"Unable to execute command {cmd.GetName()}", e);
                    }

                    continue;
                }

                Console.WriteLine($"Unknown command '{cmdSelf}'");
            }

            WaitForThreadJoin(_netThread);
            WaitForThreadJoin(_mainThread);
        }

        private static void WaitForThreadJoin(Thread thread) {
            try {
                thread?.Join(1000);
            } catch {
                ;
            }
        }

        public static void RequestShutdown() {
            _isRequestShutdown = true;
        }

        public static void ThreadMain() {
            while (!_isRequestShutdown) {
                try {
                    _gridServer.TickMain();
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
                        _gridServer?.Shutdown();
                    } catch (Exception e) {
                        Logger.Error("Unable to gracefuly shutdown worker", e);
                    }

                    return;
                }

                _gridServer?.TickNetwork();
            }
        }
    }
}
