using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.tasks;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerTaskFinish : IPacket
    {
        private uint _taskId;
        private string _jobName;
        private EGridJobTaskState _jobState;

        public PacketWorkerTaskFinish() {
            
        }

        public PacketWorkerTaskFinish(GridJobTask task) {
            _taskId = task.TaskId;
            _jobName = task.ParentJob.Name;
            _jobState = task.State;
        }

        public void Read(PacketBuffer buffer) {
            _taskId = buffer.ReadUInt32();
            _jobName = buffer.ReadString();
            _jobState = (EGridJobTaskState)buffer.ReadInt16();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_taskId);
            buffer.Write(_jobName);
            buffer.Write((short)_jobState);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleTaskFinish(this);
        }

        public uint GetTaskId() {
            return _taskId;
        }

        public string GetJobName() {
            return _jobName;
        }

        public EGridJobTaskState GetState() {
            return _jobState;
        }
    }
}
