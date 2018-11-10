using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Core;

namespace grid_shared.grid.tasks
{
    public abstract class GridJobModule
    {
        public abstract void ExecuteTask(GridTaskExecutor executor, GridJobTask task, ILog logger);

        public abstract GridJob GenerateJob(string jobName, params string[] args);

        public abstract void FinishJob(GridJob job, ILog logger);

        public abstract string UsageArgsHelp();
    }
}
