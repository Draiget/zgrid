using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.tasks;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerTaskResponse : IPacket
    {
        private bool _isAccept;
        private string _jobName;
        private uint _taskId;

        public PacketWorkerTaskResponse() {
            
        }

        public PacketWorkerTaskResponse(GridJobTask task, bool isAccept) {
            _taskId = task.TaskId;
            _jobName = task.ParentJob.Name;
            _isAccept = isAccept;
        }

        public void Read(PacketBuffer buffer) {
            _jobName = buffer.ReadString();
            _taskId = buffer.ReadUInt32();
            _isAccept = buffer.ReadBoolean();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_jobName);
            buffer.Write(_taskId);
            buffer.Write(_isAccept);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleTaskResponse(this);
        }

        public bool IsAccepted() {
            return _isAccept;
        }

        public string GetJobName() {
            return _jobName;
        }

        public uint GetTaskId() {
            return _taskId;
        }
    }
}
