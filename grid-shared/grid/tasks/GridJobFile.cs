using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.utils;
using Newtonsoft.Json;

namespace grid_shared.grid.tasks
{
    [Serializable]
    public class GridJobFile
    {
        public EGridJobFileShare ShareMode;
        public EGridJobFileDirection Direction;

        public string FileName;

        [NonSerialized]
        public string InputPath;

        public uint CheckSum;

        [NonSerialized]
        public byte[] Bytes;

        public override string ToString() {
            return  $"GridJobFile [Name={FileName}, CheckSum={CheckSum}]";
        }

        public string Serialize(bool minify = false) {
            return JsonConvert.SerializeObject(this, minify ? Formatting.None : Formatting.Indented);
        }

        public static GridJobFile Deserialize(string input) {
            return JsonConvert.DeserializeObject<GridJobFile>(input);
        }

        public GridJobFileLink GetLink() {
            return new GridJobFileLink(this);
        }

        public static GridJobFile ImportFromExternal(string input, EGridJobFileDirection dir, EGridJobFileShare share) {
            if (!File.Exists(input)) {
                throw new Exception($"No such file {input}, you need to pass full path, not relative");
            }

            var file = new GridJobFile {
                FileName = Path.GetFileName(input),
                InputPath = input,
                Direction = dir,
                ShareMode = share,
                Bytes = File.ReadAllBytes(input)
            };

            file.CheckSum = CryptoUtils.CrcOfBytes(file.Bytes);
            return file;
        }

        public void UpdateCheckSum() {
            CheckSum = CryptoUtils.CrcOfBytes(Bytes);
        }
    }
}
