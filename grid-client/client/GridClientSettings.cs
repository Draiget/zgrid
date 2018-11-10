using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace grid_client.client
{
    public class GridClientSettings
    {
        public static GridClientSettings Defaults => new GridClientSettings {
            ServerAddress = "localhost",
            ServerPort = 3045,
            ReceiveTimeout = 3000,
            ConnectRepeatInterval = 1500,
            WorkerName = "zgrid-client-1"
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

        public static GridClientSettings Deserialize(string fromJson) {
            var obj = JsonConvert.DeserializeObject<GridClientSettings>(fromJson);
            return obj;
        }

        public static string Serialize(GridClientSettings obj) {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
