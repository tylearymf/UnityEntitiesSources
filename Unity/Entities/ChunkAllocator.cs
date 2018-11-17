namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ChunkAllocator : IDisposable
    {
        private unsafe byte* m_FirstChunk;
        private unsafe byte* m_LastChunk;
        private int m_LastChunkUsedSize;
        private const int ms_ChunkSize = 0x10000;
        private const int ms_ChunkAlignment = 0x40;
        public unsafe void Dispose()
        {
            while (true)
            {
                if (this.m_FirstChunk == null)
                {
                    this.m_LastChunk = null;
                    return;
                }
                byte* numPtr = *((byte**) this.m_FirstChunk);
                UnsafeUtility.Free((void*) this.m_FirstChunk, Allocator.Persistent);
                this.m_FirstChunk = numPtr;
            }
        }

        public unsafe byte* Allocate(int size, int alignment)
        {
            int num = ((this.m_LastChunkUsedSize + alignment) - 1) & ~(alignment - 1);
            if ((this.m_LastChunk == null) || (size > (0x10000 - num)))
            {
                byte* numPtr2 = (byte*) ref UnsafeUtility.Malloc(0x10000L, 0x40, Allocator.Persistent);
                *((IntPtr*) numPtr2) = IntPtr.Zero;
                if (this.m_LastChunk != null)
                {
                    *((IntPtr*) this.m_LastChunk) = numPtr2;
                }
                else
                {
                    this.m_FirstChunk = numPtr2;
                }
                this.m_LastChunk = numPtr2;
                this.m_LastChunkUsedSize = sizeof(byte*);
                num = ((this.m_LastChunkUsedSize + alignment) - 1) & ~(alignment - 1);
            }
            byte* numPtr = this.m_LastChunk + num;
            this.m_LastChunkUsedSize = num + size;
            return numPtr;
        }

        public unsafe byte* Construct(int size, int alignment, void* src)
        {
            byte* numPtr = ref this.Allocate(size, alignment);
            UnsafeUtility.MemCpy((void*) numPtr, src, (long) size);
            return numPtr;
        }
    }
}

