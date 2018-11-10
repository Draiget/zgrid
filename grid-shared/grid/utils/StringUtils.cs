using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grid_shared.grid.utils
{
    public static class StringUtils
    {
        public static bool HasArgs(this object[] args, int count) {
            return args.Length >= count;
        }

        public static string[] OffsetArgumentsAs(this string[] self, int offset) {
            if (self.Length <= offset) {
                return new string[] { };
            }

            var args = new string[self.Length - 1];
            for (var i = 0; i < args.Length; i++) {
                args[i] = self[i + offset];
            }

            return args;
        }

        public static string[] SlitToArgsFrom(this string self, int fromIndex, string delimer = " ") {
            var buffer = self.Split(new []{ delimer }, StringSplitOptions.None);
            if (buffer.Length == 1) {
                return new string[]{ };
            }

            var args = new string[buffer.Length - 1];
            for (var i = 0; i < args.Length; i++) {
                args[i] = buffer[i + fromIndex];
            }
            return args;
        }

        public static string Repeat(this string self, int times) {
            for (var i = 0; i < times; i++) {
                self += self;
            }

            return self;
        }
    }
}
