using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_client.client.network;
using log4net;

namespace grid_client.client
{
    public class GridClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridClient));

        public const string SettingFileName = "client.config.json";

        private readonly GridClientNetworkSystem _networkSystem;

        public GridClientSettings Settings {
            get;
            private set;
        }

        public GridClient() {
            _networkSystem = new GridClientNetworkSystem(this);
        }

        public void Init() {
            if (!File.Exists(SettingFileName)) {
                Settings = GridClientSettings.Defaults;
                File.WriteAllText(SettingFileName, GridClientSettings.Serialize(Settings));
            } else {
                try {
                    Settings = GridClientSettings.Deserialize(File.ReadAllText(SettingFileName));
                } catch (Exception e) {
                    Settings = GridClientSettings.Defaults;
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
    }
}
