namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ECBChunk
    {
        internal int Used;
        internal int Size;
        internal unsafe ECBChunk* Next;
        internal unsafe ECBChunk* Prev;
        internal int Capacity =>
            (this.Size - this.Used);
        internal int Bump(int size)
        {
            int used = this.Used;
            this.Used += size;
            return used;
        }

        internal int BaseSortIndex
        {
            get
            {
                int sortIndex;
                ECBChunk* chunkPtr = (ECBChunk*) this;
                if (this.Used < sizeof(BasicCommand))
                {
                    sortIndex = -1;
                }
                else
                {
                    sortIndex = chunkPtr[1].SortIndex;
                }
                return sortIndex;
            }
        }
    }
}

