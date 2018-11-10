using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using grid_shared.grid.tasks;
using grid_shared.grid.utils;
using log4net;
using log4net.Core;

namespace grid_task_lib
{
    public class StringSearchJobModule : GridJobModule
    {
        public const int BlockSize = 1024 * 600;
        
        public override void ExecuteTask(GridTaskExecutor executor, GridJobTask task, ILog logger) {
            var inputFilePath = GridIo.ResolveFilePathShared(task.ParentJob, task.Arguments[0]);

            if (!File.Exists(inputFilePath)) {
                throw new Exception($"Unable to load task input file '{task.Arguments[0]}', not found by present path '{inputFilePath}'");
            }

            byte[] buffer;
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read)) {
                var startPos = long.Parse(task.Arguments[1]);
                var endPos = long.Parse(task.Arguments[2]);

                buffer = new byte[endPos - startPos];
                fs.Seek(startPos, SeekOrigin.Begin);
                fs.Read(buffer, 0, buffer.Length);
            }

            var needleCount = 0;
            var needleString = task.Arguments[4];
            using (var sr = new StreamReader(new MemoryStream(buffer))) {
                var all = sr.ReadToEnd();

                for (var i = 0; i < all.Length; i++) {
                    if (SearchSubstringAt(all, needleString, i)) {
                        needleCount++;
                        i += needleString.Length;
                    }
                }
            }

            logger.Info($"Find '{needleCount}' in block");
            executor.WriteFileOutput(task, task.Arguments[3], $"{needleCount}");
        }

        public override GridJob GenerateJob(string jobName, params string[] args) {
            if (args.Length < 1) {
                throw new Exception("Not enough arguments");
            }

            var job = new GridJob {
                Name = jobName
            };

            var moduleFile = args[0];
            var textFile = args[1];
            var outputFile = args[2];
            var searchString = args[3];

            var jobModuleFile = LoadJobFile(moduleFile, EGridJobFileDirection.WorkerInput, EGridJobFileShare.SharedBetweenTasks);
            job.JobFiles.Add(jobModuleFile);

            var jobInputTextFile = LoadJobFile(textFile, EGridJobFileDirection.WorkerInput, EGridJobFileShare.SharedBetweenTasks);
            job.JobFiles.Add(jobInputTextFile);

            var jobOutputFile = CreateJobTaskOutFile(outputFile);
            job.JobFiles.Add(jobOutputFile);

            job.JobOutFile = CreateJobTotalOutFile(outputFile);

            uint lastTaskId = 0;

            using (var fs = new FileStream(textFile, FileMode.Open, FileAccess.Read)) {
                using (var br = new BinaryReader(fs)) {
                    while (fs.Position < fs.Length) {
                        if (fs.Length - fs.Position < BlockSize) {
                            job.JobTasks.Add(CreateTask(
                                jobModuleFile, jobInputTextFile, outputFile, fs.Position, fs.Position + fs.Length, searchString, lastTaskId));

                            return job;
                        }

                        var startPos = fs.Position;
                        var endPos = startPos + BlockSize;

                        if (fs.Length >= BlockSize + searchString.Length + 1) {
                            fs.Seek(BlockSize - (searchString.Length + 1), SeekOrigin.Current);

                            var middleBuffer = br.ReadBytes((searchString.Length + 1) * 2);
                            var middleStr = Encoding.UTF8.GetString(middleBuffer);

                            for (var i = 0; i < middleStr.Length; i++) {
                                if (SearchSubstringAt(middleStr, searchString, i) && i > searchString.Length) {
                                    endPos = startPos + BlockSize + i + searchString.Length;
                                    break;
                                }
                            }
                        } else {
                            endPos = fs.Length;
                        }

                        job.JobTasks.Add(CreateTask(
                            jobModuleFile, jobInputTextFile, outputFile, startPos, endPos, searchString, lastTaskId++));

                        fs.Seek(endPos, SeekOrigin.Begin);
                    }
                }
            }

            return job;
        }

        public override void FinishJob(GridJob job, ILog logger) {
            var count = 0;

            foreach (var task in job.JobTasks) {
                if (task.OutputFile?.Bytes != null) {
                    try {
                        count += int.Parse(Encoding.UTF8.GetString(task.OutputFile.Bytes));
                    } catch {
                        ;
                    }
                }
            }

            logger.Info($"Total count is '{count}'");
            File.WriteAllText(GridIo.ResolveFilePathShared(job, job.JobOutFile.FileName), $"{count}");
        }

        public GridJobTask CreateTask(GridJobFile moduleFile, GridJobFile inputFile, string outputFile, long startPos, long endPos, string searchHaystack, uint taskId) {
            var task = new GridJobTask {
                TaskId = taskId,
                ModuleFileRef = moduleFile.GetLink(),
                Arguments = new [] {
                    inputFile.FileName,
                    startPos.ToString(),
                    endPos.ToString(),
                    outputFile,
                    searchHaystack
                }
            };

            task.ReferencedFiles.Add(inputFile.GetLink());
            return task;
        }

        public static bool SearchSubstringAt(string input, string searchStr, int inputOffset) {
            var matchCount = 0;
            for (var i = 0; inputOffset + i < input.Length && matchCount != searchStr.Length; i++) {
                if (i >= searchStr.Length) {
                    return false;
                }

                if (input[inputOffset + i] != searchStr[i]) {
                    return false;
                }

                matchCount++;
            }

            return matchCount == searchStr.Length;
        }

        private GridJobFile LoadJobFile(string path, EGridJobFileDirection dir, EGridJobFileShare share) {
            if (!File.Exists(path)) {
                throw new Exception($"No such file {path}, you need to pass full path, not relative");
            }

            return GridJobFile.ImportFromExternal(path, dir, share);
        }

        private GridJobFile CreateJobTaskOutFile(string path) {
            return new GridJobFile {
                FileName = Path.GetFileName(path),
                InputPath = path,
                Direction = EGridJobFileDirection.WorkerOutput,
                ShareMode = EGridJobFileShare.PerEachTask
            };
        }

        private GridJobFile CreateJobTotalOutFile(string path) {
            return new GridJobFile {
                FileName = Path.GetFileName(path),
                InputPath = path,
                Direction = EGridJobFileDirection.WorkerOutput,
                ShareMode = EGridJobFileShare.SharedBetweenTasks
            };
        }

        public override string UsageArgsHelp() {
            return "<moduleFilePath> <searchTextFilePath> <outputFilePath> <searchString>";
        }
    }
}
