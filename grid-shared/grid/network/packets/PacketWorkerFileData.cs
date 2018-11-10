using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.tasks;
using grid_shared.grid.utils;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerFileData : IPacket
    {
        private string _jobName;
        private uint _taskId;
        private GridJobFile _file;

        public PacketWorkerFileData() {
            
        }

        public PacketWorkerFileData(GridJobTask task, GridJobFile file) {
            _jobName = task.ParentJob.Name;
            _taskId = task.TaskId;
            _file = file;
        }

        public void Read(PacketBuffer buffer) {
            _taskId = buffer.ReadUInt32();
            _jobName = buffer.ReadString();
            _file = GridJobFile.Deserialize(buffer.ReadString());
            var len = buffer.ReadInt32();
            _file.Bytes = buffer.ReadBytes(len);
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_taskId);
            buffer.Write(_jobName);
            buffer.Write(_file.Serialize(true));
            buffer.Write(_file.Bytes.Length);
            buffer.Write(_file.Bytes);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerFiles) handler).HandleFileData(this);
        }

        public uint GetTaskId() {
            return _taskId;
        }

        public string GetJobName() {
            return _jobName;
        }

        public byte[] GetData() {
            return _file.Bytes;
        }

        public GridJobFile GetFile() {
            return _file;
        }

        public void UpdateCheckSum() {
            _file.CheckSum = CryptoUtils.CrcOfBytes(_file.Bytes);
        }
    }
}
