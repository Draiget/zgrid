using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.utils;

namespace grid_shared.grid.tasks
{
    public static class GridIo
    {
        private static string JobsDirectory = "jobs_temp";

        public static string ResolveFilePath(GridJobTask task, string fileName) {
            return $"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}\\{fileName}";
        }

        public static string ResolveFilePath(GridJobTask task, GridJobFile file) {
            if (file.ShareMode == EGridJobFileShare.SharedBetweenTasks) {
                return $"{JobsDirectory}\\{task.ParentJob.Name}\\{file.FileName}";
            }

            return $"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}\\{file.FileName}";
        }

        public static string ResolveFilePath(GridJob job, GridJobTask task, GridJobFile file) {
            if (file.ShareMode == EGridJobFileShare.SharedBetweenTasks) {
                return $"{JobsDirectory}\\{job.Name}\\{file.FileName}";
            }

            return $"{JobsDirectory}\\{job.Name}\\task-{task.TaskId}\\{file.FileName}";
        }

        public static string ResolveFilePathShared(GridJob job, string fileName) {
            return $"{JobsDirectory}\\{job.Name}\\{fileName}";
        }

        public static string ResolveFilePathShared(string jobName, string fileName) {
            return $"{JobsDirectory}\\{jobName}\\{fileName}";
        }
        
        public static void CreateDirectoriesForJob(GridJob job) {
            CreateJobDirectoriesIfNotExists(job);
        }

        public static void CreateDirectoriesForTask(GridJobTask task) {
            if (CreateTaskDirectoriesIfNotExists(task)) {
                return;
            }

            CleanupDirectory($"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}");
            CreateDirectoriesForTask(task);
        }

        public static bool CreateJobDirectoriesIfNotExists(GridJob job) {
            if (!CheckIfJobDirectoriesExists(job)) {
                Directory.CreateDirectory($"{JobsDirectory}\\{job.Name}");
                return true;
            }

            return false;
        }

        public static bool CreateTaskDirectoriesIfNotExists(GridJobTask task) {
            if (!CheckIfTaskDirectoriesExists(task)) {
                Directory.CreateDirectory($"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}");
                return true;
            }

            return false;
        }

        public static bool CheckIfJobDirectoriesExists(GridJob job) {
            return Directory.Exists($"{JobsDirectory}\\{job.Name}");
        }

        public static bool CheckIfTaskDirectoriesExists(GridJobTask task) {
            return Directory.Exists($"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}");
        }

        public static void CleanupDirectory(GridJob jobDir) {
            CleanupDirectory($"{JobsDirectory}\\{jobDir.Name}");
        }

        public static void CleanupTasksDirectoriesOnly(GridJob jobDir) {
            foreach (var file in Directory.GetDirectories($"{JobsDirectory}\\{jobDir.Name}")) {
                try {
                    Directory.Delete(file, true);
                } catch {
                    ;
                }
            }
        }

        public static void CleanupDirectory(string path) {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                try {
                    File.Delete(file);
                } catch {
                    ;
                }
            }

            try {
                Directory.Delete(path, true);
            } catch {
                ;
            }
        }

        public static bool IsJobTaskFileExistsAndValid(GridJobTask task, GridJobFile jobFile) {
            var fp = $"{JobsDirectory}\\{task.ParentJob.Name}\\{jobFile.FileName}";
            if (jobFile.ShareMode == EGridJobFileShare.SharedBetweenTasks) {
                if (!File.Exists(fp)) {
                    return false;
                }

                return CryptoUtils.CrcOfFile(fp) == jobFile.CheckSum;
            }

            fp = $"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}\\{jobFile.FileName}";
            if (!File.Exists(fp)) {
                return false;
            }

            return CryptoUtils.CrcOfFile(fp) == jobFile.CheckSum;
        }

        public static void StoreJobTaskFile(GridJobTask task, GridJobFile file) {
            CreateTaskDirectoriesIfNotExists(task);

            var fp = $"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}\\{file.FileName}";
            if (file.ShareMode == EGridJobFileShare.SharedBetweenTasks) {
                fp = $"{JobsDirectory}\\{task.ParentJob.Name}\\{file.FileName}";
            }

            File.WriteAllBytes(fp, file.Bytes);
        }

        public static void StoreJobTaskOutputFile(GridJobTask task, string file, byte[] data) {
            CreateTaskDirectoriesIfNotExists(task);

            var fp = $"{JobsDirectory}\\{task.ParentJob.Name}\\task-{task.TaskId}\\{file}";
            File.WriteAllBytes(fp, data);
        }
    }
}
