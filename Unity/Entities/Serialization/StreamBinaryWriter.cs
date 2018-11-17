namespace Unity.Entities.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    public class StreamBinaryWriter : Unity.Entities.Serialization.BinaryWriter, IDisposable
    {
        private Stream stream;
        private byte[] buffer;

        public StreamBinaryWriter(string fileName, int bufferSize = 0x10000)
        {
            this.stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
            this.buffer = new byte[bufferSize];
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        public unsafe void WriteBytes(void* data, int bytes)
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
                int count = Math.Min(num, length);
                UnsafeUtility.MemCpy((void*) numPtr, data, (long) count);
                this.stream.Write(this.buffer, 0, count);
                data += count;
                num -= count;
            }
        }
    }
}

