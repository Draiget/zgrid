using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace grid_shared.grid.network
{
    public class PacketBuffer : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly Stream _baseStream;

        public PacketBuffer(Stream stream) {
            _baseStream = stream;
            _reader = new BinaryReader(stream);
            _writer = new BinaryWriter(stream);
        }

        public Stream BaseStream => _baseStream;

        public string[] ReadStringArray() {
            var arrayLen = _reader.ReadUInt16();
            var array = new string[arrayLen];
            for (var i = 0; i < arrayLen; i++) {
                array[i] = _reader.ReadString();
            }

            return array;
        }

        public void Write(string[] arr) {
            _writer.Write((ushort)arr.Length);
            foreach (var str in arr) {
                _writer.Write(str);
            }
        }

        public bool ReadBoolean() {
            return _reader.ReadBoolean();
        }

        public sbyte ReadSByte() {
            return _reader.ReadSByte();
        }

        public byte ReadByte() {
            return _reader.ReadByte();
        }

        public short ReadInt16() {
            return _reader.ReadInt16();
        }

        public int ReadInt32() {
            return _reader.ReadInt32();
        }

        public long ReadInt64() {
            return _reader.ReadInt64();
        }

        public ushort ReadUInt16() {
            return _reader.ReadUInt16();
        }

        public uint ReadUInt32() {
            return _reader.ReadUInt32();
        }

        public ulong ReadUInt64() {
            return _reader.ReadUInt64();
        }

        public float ReadFloat() {
            return _reader.ReadSingle();
        }

        public double ReadDouble() {
            return _reader.ReadDouble();
        }

        public string ReadString() {
            return _reader.ReadString();
        }

        public byte[] ReadBytes(int count) {
            return _reader.ReadBytes(count);
        }

        public void Write(bool value) {
            _writer.Write(value);
        }

        public void Write(sbyte value) {
            _writer.Write(value);
        }

        public void Write(byte value) {
            _writer.Write(value);
        }

        public void Write(short value) {
            _writer.Write(value);
        }

        public void Write(int value) {
            _writer.Write(value);
        }

        public void Write(long value) {
            _writer.Write(value);
        }

        public void Write(ushort value) {
            _writer.Write(value);
        }

        public void Write(uint value) {
            _writer.Write(value);
        }

        public void Write(ulong value) {
            _writer.Write(value);
        }

        public void Write(float value) {
            _writer.Write(value);
        }

        public void Write(double value) {
            _writer.Write(value);
        }

        public void Write(string value) {
            _writer.Write(value);
        }

        public void Write(byte[] value) {
            _writer.Write(value);
        }

        public void Dispose() {
            _reader?.Dispose();
            _writer?.Dispose();
        }
    }
}
