using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.modules;
using grid_shared.grid.tasks;
using grid_shared.grid.utils;

namespace grid_server.server.commands.list
{
    public class CommandJob : ConCommand
    {
        public CommandJob()
            : base("job", "Job manager") {
        }

        public override void Execute(params string[] arguments) {
            if (arguments.Length < 1) {
                Console.WriteLine("Usage:");
                Console.WriteLine("job create");
                Console.WriteLine("job load");
                return;
            }

            if (arguments[0] == "create") {
                HandleJobTestCreate(arguments.OffsetArgumentsAs(1));
                return;
            }

            if (arguments[0] == "load") {
                HandleJobTestLoad(arguments.OffsetArgumentsAs(1));
            }
        }

        private void HandleJobTestLoad(string[] args) {
            var jobFileName = args.Length <= 0 ? "job-generated.json" : args[0];
            var job = GridJob.Deserialize(File.ReadAllText(jobFileName));

            foreach (var task in job.JobTasks) {
                task.ParentJob = job;
            }

            GridIo.CreateDirectoriesForJob(job);

            foreach (var file in job.JobFiles) {
                if (file.Direction == EGridJobFileDirection.WorkerInput) {
                    var localPath = GridIo.ResolveFilePath(job, null, file);
                    file.Bytes = File.ReadAllBytes(localPath);
                    file.UpdateCheckSum();
                }
            }

            if (Server.IsAnyJobActive()) {
                Logger.Warn($"Server has already executing job {Server.GetActiveJob()}, new job will be added to the queue");
            }

            Logger.Info($"Queue new job {job}");
            Server.RunQueueJob(job);
        }

        private void HandleJobTestCreate(string[] args) {
            var moduleName = args.Length <= 0 ? "grid-task-lib.dll" : args[0];
            var modulePath = $"{Directory.GetCurrentDirectory()}\\modules\\{moduleName}";

            GridJobModule module;
            try {
                Logger.Debug($"Loading from '{modulePath}'");
                module = ModuleLoader.LoadJobModuleFile($"{modulePath}");
            } catch (Exception e) {
                Logger.Error("Unable to load module", e);
                return;
            }
            
            var job = module.GenerateJob(
                "string-find-job-1",
                modulePath, 
                @"A:\zgrid-lw\grid-server\bin\Debug\modules\loadscr.gmserver.ru.log",
                "strs-count.txt",
                "76561198357644316"); // 76561198357644316

            File.WriteAllText("job-generated.json", job.Serialize());
        }
    }
}
