using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grid_shared.grid.tasks
{
    [Serializable]
    public class GridJobFileLink
    {
        public string FileName;
        public uint CheckSum;

        public GridJobFileLink() {
            
        }

        public GridJobFileLink(GridJobFile file) {
            FileName = file.FileName;
            CheckSum = file.CheckSum;
        }

        public GridJobFile ResolveLink(GridJob job, bool isOutput = false) {
            if (isOutput) {
                return job.JobFiles.FirstOrDefault(x => x.FileName == FileName && x.Direction == EGridJobFileDirection.WorkerOutput);
            }

            return job.JobFiles.FirstOrDefault(x => x.FileName == FileName && x.CheckSum == CheckSum);
        }

        public override string ToString() {
            return $"GridJobFileLink[Name={FileName}, CheckSum={CheckSum}]";
        }
    }
}
