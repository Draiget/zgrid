using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.handlers;
using grid_shared.grid.tasks;

namespace grid_shared.grid.network.packets
{
    public class PacketWorkerFileRequest : IPacket
    {
        private uint _taskId;
        private string _jobName;
        private string _requestFile;
        private uint _requestFileCheckSum;
        private EGridJobFileShare _shareType;

        public PacketWorkerFileRequest() {
            
        }

        public PacketWorkerFileRequest(uint taskId, string jobName, GridJobFile file) {
            _taskId = taskId;
            _jobName = jobName;
            _requestFile = file.FileName;
            _requestFileCheckSum = file.CheckSum;
            _shareType = file.ShareMode;
        }

        public void Read(PacketBuffer buffer) {
            _taskId = buffer.ReadUInt32();
            _jobName = buffer.ReadString();
            _requestFile = buffer.ReadString();
            _requestFileCheckSum = buffer.ReadUInt32();
            _shareType = (EGridJobFileShare)buffer.ReadUInt16();
        }

        public void Write(PacketBuffer buffer) {
            buffer.Write(_taskId);
            buffer.Write(_jobName);
            buffer.Write(_requestFile);
            buffer.Write(_requestFileCheckSum);
            buffer.Write((ushort)_shareType);
        }

        public void Handle(IPacketHandler handler) {
            ((INetHandlerWorkerServer) handler).HandleFileRequest(this);
        }

        public string GetJobName() {
            return _jobName;
        }

        public string GetRequestFile() {
            return _requestFile;
        }

        public uint GetRequestFileCheckSum() {
            return _requestFileCheckSum;
        }

        public uint GetTaskId() {
            return _taskId;
        }

        public EGridJobFileShare GetShareType() {
            return _shareType;
        }
    }
}
