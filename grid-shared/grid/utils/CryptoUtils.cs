using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.Crc32;

namespace grid_shared.grid.utils
{
    public static class CryptoUtils
    {
        public static uint CrcOfFile(string fileName) {
            return Crc32Algorithm.Compute(File.ReadAllBytes(fileName));
        }

        public static uint CrcOfBytes(byte[] data) {
            return Crc32Algorithm.Compute(data);
        }
    }
}
