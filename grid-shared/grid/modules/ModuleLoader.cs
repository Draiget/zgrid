using System;
using System.Linq;
using System.Reflection;
using grid_shared.grid.tasks;

namespace grid_shared.grid.modules
{
    public static class ModuleLoader
    {
        public static GridJobModule LoadJobModuleFile(string fileName) {
            var assembly = Assembly.LoadFile(fileName);
            var entryPoint = assembly.GetExportedTypes().FirstOrDefault(t => t.BaseType == typeof(GridJobModule));
            if (entryPoint == null) {
                throw new Exception($"Unable to load module {fileName}: target module do not release {typeof(GridJobModule).FullName} class");
            }

            dynamic obj;
            try {
                obj = Activator.CreateInstance(entryPoint, null);
            } catch (Exception err) {
                throw new Exception($"Unable to load module {fileName}: unable to create instance of {entryPoint.FullName} module entrypoint class", err);
            }

            return (GridJobModule)obj;
        }
    }
}
