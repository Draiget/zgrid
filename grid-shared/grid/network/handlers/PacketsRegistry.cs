using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.network.packets;

namespace grid_shared.grid.network.handlers
{
    public class PacketsRegistry
    {
        private static readonly Dictionary<Type, int> PacketTypeToIdMap;
        private static readonly Dictionary<int, Type> PacketIdToTypeMap;

        private static int _lastPacketId;

        static PacketsRegistry() {
            _lastPacketId = 0;
            PacketTypeToIdMap = new Dictionary<Type, int>();
            PacketIdToTypeMap = new Dictionary<int, Type>();
        }

        public static void Initialize() {
            RegisterPacket(typeof(PacketWorkerLoginRequest));
            RegisterPacket(typeof(PacketWorkerLoginResponse));
            RegisterPacket(typeof(PacketWorkerDisconnect));
            RegisterPacket(typeof(PacketWorkerResponseStatus));
            RegisterPacket(typeof(PacketWorkerRequestStatus));
            RegisterPacket(typeof(PacketWorkerTaskRequest));
            RegisterPacket(typeof(PacketWorkerTaskResponse));
            RegisterPacket(typeof(PacketWorkerTaskCancel));
            RegisterPacket(typeof(PacketWorkerTaskFinish));
            RegisterPacket(typeof(PacketWorkerFileData));
            RegisterPacket(typeof(PacketWorkerFileRequest));
        }

        private static void RegisterPacket(Type packet) {
            if (!typeof(IPacket).IsAssignableFrom(packet)) {
                throw new ArgumentException($"Packet type '{nameof(packet)}' must be derive from '{typeof(IPacket).FullName}'!");
            }

            if (PacketTypeToIdMap.ContainsKey(packet)) {
                throw new ArgumentException($"Packet '{nameof(packet)}' ({packet.Name}) is already registered with id {PacketTypeToIdMap[packet]}");
            }

            PacketTypeToIdMap[packet] = _lastPacketId;
            PacketIdToTypeMap[_lastPacketId] = packet;

            _lastPacketId++;
        }

        public static Type GetPacketById(int id) {
            if (PacketIdToTypeMap.ContainsKey(id)) {
                return PacketIdToTypeMap[id];
            }

            return null;
        }

        public static int GetPacketIdByType(Type type) {
            if (PacketTypeToIdMap.ContainsKey(type)) {
                return PacketTypeToIdMap[type];
            }

            return -1;
        }
    }
}
