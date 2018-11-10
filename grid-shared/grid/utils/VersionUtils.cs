using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace grid_shared.grid.utils
{
    public class VersionUtils
    {
        public static AssemblyName GetAssemblyNameInfo() {
            var executing = Assembly.GetExecutingAssembly();
            return executing.GetName();
        }
    }
}
