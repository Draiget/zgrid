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
    public class PacketWorkerTaskRequest : IPacket
    {
        private static int _lastRequestId;

        private string _jobSerialized;
        private string _jobTaskSerialized;

        public PacketWorkerTaskRequest() {
            
        }

        public PacketWorkerTaskRequest(GridJobTask task) {
            _jobSerialized = task.ParentJob.Serialize(true);
            _jobTaskSerialized = task.Serialize(true);

            _lastRequestId++;
        }

        public void Read(PacketBuffer buffer) {
            _lastRequestId = buffer.ReadInt32();
            _jobSerialized = buffer.ReadString();
            _jobTaskSerialized = buffer.ReadString();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_lastRequestId);
            buffer.Write(_jobSerialized);
            buffer.Write(_jobTaskSerialized);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerClient) handler).HandleTaskRequest(this);
        }

        public GridJobTask GetTask() {
            var job = GridJob.Deserialize(_jobSerialized);
            var task = GridJobTask.Deserialize(_jobTaskSerialized);

            task.ParentJob = job;
            return task;
        }

        public int GetCallbackId() {
            return _lastRequestId;
        }
    }
}
