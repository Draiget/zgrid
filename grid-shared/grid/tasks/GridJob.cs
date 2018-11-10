using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace grid_shared.grid.tasks
{
    [Serializable]
    public class GridJob
    {
        public GridJobFile JobOutFile;
        public List<GridJobFile> JobFiles;
        public string Name;

        public List<GridJobTask> JobTasks;

        public GridJob() {
            JobFiles = new List<GridJobFile>();
            JobTasks = new List<GridJobTask>();
        }

        public string Serialize(bool minify = false) {
            return JsonConvert.SerializeObject(this, minify ? Formatting.None : Formatting.Indented);
        }

        public static GridJob Deserialize(string input) {
            return JsonConvert.DeserializeObject<GridJob>(input);
        }

        public override string ToString() {
            return $"GridJob [Name={Name}, TaskCount={JobTasks.Count}, FilesCount={JobFiles.Count}]";
        }
    }
}
