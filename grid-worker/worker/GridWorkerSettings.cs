using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace grid_worker.worker
{
    public class GridWorkerSettings
    {
        public static GridWorkerSettings Defaults => new GridWorkerSettings {
            ServerAddress = "localhost",
            ServerPort = 3045,
            ReceiveTimeout = 3000,
            ConnectRepeatInterval = 1500,
            WorkerName = "zgrid-worker-1"
        };

        public uint ServerPort {
            get;
            set;
        }

        public string ServerAddress {
            get;
            set;
        }

        public int ReceiveTimeout {
            get;
            set;
        }

        public int ConnectRepeatInterval {
            get;
            set;
        }

        public string WorkerName {
            get;
            set;
        }

        public static GridWorkerSettings Deserialize(string fromJson) {
            var obj = JsonConvert.DeserializeObject<GridWorkerSettings>(fromJson);
            return obj;
        }

        public static string Serialize(GridWorkerSettings obj) {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
