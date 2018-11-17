namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ECBChainPlaybackState
    {
        public unsafe ECBChunk* Chunk;
        public int Offset;
        public int NextSortIndex;
    }
}

