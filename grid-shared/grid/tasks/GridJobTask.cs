using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.utils;
using log4net;
using log4net.Core;
using Newtonsoft.Json;

namespace grid_shared.grid.tasks
{
    [Serializable]
    public class GridJobTask
    {
        public uint TaskId;
        public string[] Arguments;

        [NonSerialized]
        public EGridJobTaskState State;

        public GridJobFileLink ModuleFileRef;
        public List<GridJobFileLink> ReferencedFiles;

        [NonSerialized]
        public GridJobFile OutputFile;

        [NonSerialized]
        public GridJob ParentJob;

        public GridJobTask() {
            ReferencedFiles = new List<GridJobFileLink>();
        }

        public void ExecuteModuleTask(GridTaskExecutor executor, ILog logger) {
            var module = LoadModule(this, ModuleFileRef);
            if (module == null) {
                logger.Error($"Unable to load module {ModuleFileRef} for task {this}");
                return;
            }

            module.ExecuteTask(executor, this, logger);
        }

        public void ExecuteModuleFinishJob(ILog logger) {
            var module = LoadModule(this, ModuleFileRef);
            if (module == null) {
                logger.Error($"Unable to load module {ModuleFileRef} for task {this}");
                return;
            }

            module.FinishJob(ParentJob, logger);
        }

        public string Serialize(bool minify = false) {
            return JsonConvert.SerializeObject(this, minify ? Formatting.None : Formatting.Indented);
        }

        public static GridJobTask Deserialize(string input) {
            return JsonConvert.DeserializeObject<GridJobTask>(input);
        }

        public override string ToString() {
            return $"GridJobTask [JobName={ParentJob.Name}, Id={TaskId}, State={State.ToString()}]";
        }

        private static GridJobModule LoadModule(GridJobTask task, GridJobFileLink link) {
            var moduleFile = task.ParentJob.JobFiles.FirstOrDefault(x => x.FileName == link.FileName);
            if (moduleFile == null) {
                throw new GridJobTaskCommandException(task, $"Unable to locate referenced module file {link} in task instance");
            }

            moduleFile.Bytes = File.ReadAllBytes(GridIo.ResolveFilePath(task, moduleFile));
            moduleFile.UpdateCheckSum();

            Assembly assembly;
            try {
                assembly = Assembly.Load(moduleFile.Bytes);
            } catch (Exception e) {
                throw new GridJobTaskCommandException(task, $"Unable to load module assembly file {link} when try to execute task", e);
            }

            var entryPoint = assembly.GetExportedTypes().FirstOrDefault(t => t.BaseType == typeof(GridJobModule));
            if (entryPoint == null) {
                throw new GridJobTaskCommandException(task, $"Unable to load module {link}: target module do not release {typeof(GridJobModule).FullName} class");
            }

            dynamic obj;
            try {
                obj = Activator.CreateInstance(entryPoint, null);
            } catch (Exception err) {
                throw new GridJobTaskCommandException(task, $"Unable to load module {link}: unable to create instance of {entryPoint.FullName} module entrypoint class", err);
            }

            return (GridJobModule)obj;
        }
    }

    public class GridJobTaskCommandException : Exception
    {
        public readonly GridJobTask Task;

        public GridJobTaskCommandException(GridJobTask task, string msg) 
            : base(msg) {
            Task = task;
        }
        public GridJobTaskCommandException(GridJobTask task, string msg, Exception err)
            : base(msg, err) {
            Task = task;
        }
    }
}
