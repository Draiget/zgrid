using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.tasks;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerTaskCancel : IPacket
    {
        private uint _taskId;
        private string _jobName;

        public PacketWorkerTaskCancel() {
            
        }

        public PacketWorkerTaskCancel(GridJobTask task) {
            _taskId = task.TaskId;
            _jobName = task.ParentJob.Name;
        }

        public void Read(PacketBuffer buffer) {
            _jobName = buffer.ReadString();
            _taskId = buffer.ReadUInt32();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_jobName);
            buffer.Write(_taskId);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleTaskCancel(this);
        }

        public uint GetTaskId() {
            return _taskId;
        }

        public string GetJobName() {
            return _jobName;
        }
    }
}
