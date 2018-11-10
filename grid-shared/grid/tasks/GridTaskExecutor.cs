using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.utils;

namespace grid_shared.grid.tasks
{
    public class GridTaskExecutor
    {
        public string ResolveFilePath(GridJobTask task, GridJobFile file) {
            return GridIo.ResolveFilePath(task.ParentJob, task, file);
        }

        public string ResolveFilePath(GridJobTask task, string fileName) {
            return GridIo.ResolveFilePath(task, fileName);
        }

        public void PrepareJobTaskDirectories(GridJobTask task) {
            GridIo.CreateDirectoriesForTask(task);
        }

        public void CleanupJobDirectory(GridJob job) {
            GridIo.CleanupDirectory(job);
        }

        public void WriteFileOutput(GridJobTask task, string outputFile, string text) {
            GridIo.StoreJobTaskOutputFile(task, outputFile, Encoding.UTF8.GetBytes(text));
        }
    }
}
