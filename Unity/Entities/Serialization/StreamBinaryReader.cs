namespace Unity.Entities.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    public class StreamBinaryReader : Unity.Entities.Serialization.BinaryReader, IDisposable
    {
        private Stream stream;
        private byte[] buffer;

        public StreamBinaryReader(string fileName, int bufferSize = 0x10000)
        {
            this.stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            this.buffer = new byte[bufferSize];
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        public unsafe void ReadBytes(void* data, int bytes)
        {
            byte* numPtr;
            byte[] pinned buffer;
            int num = bytes;
            int length = this.buffer.Length;
            if (((buffer = this.buffer) == null) || (buffer.Length == 0))
            {
                numPtr = null;
            }
            else
            {
                numPtr = buffer;
            }
            while (true)
            {
                if (num == 0)
                {
                    buffer = null;
                    return;
                }
                int num3 = this.stream.Read(this.buffer, 0, Math.Min(num, length));
                num -= num3;
                UnsafeUtility.MemCpy(data, (void*) numPtr, (long) num3);
                data += num3;
            }
        }
    }
}

