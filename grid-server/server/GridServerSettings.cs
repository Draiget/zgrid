using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace grid_server.server
{
    public class GridServerSettings
    {
        public static GridServerSettings Defaults => new GridServerSettings {
            BindAddress = "0.0.0.0",
            BindPort = 3045,
            ReceiveTimeout = 3000
        };

        public uint BindPort {
            get;
            set;
        }

        public string BindAddress {
            get;
            set;
        }

        public int ReceiveTimeout {
            get;
            set;
        }

        public static GridServerSettings Deserialize(string fromJson) {
            var obj = JsonConvert.DeserializeObject<GridServerSettings>(fromJson);
            return obj;
        }

        public static string Serialize(GridServerSettings obj) {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
