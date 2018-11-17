namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size=0x40)]
    internal struct EntityCommandBufferChain
    {
        public unsafe ECBChunk* m_Tail;
        public unsafe ECBChunk* m_Head;
        public unsafe EntitySharedComponentCommand* m_CleanupList;
        public unsafe CreateCommand* m_PrevCreateCommand;
        public unsafe EntityCommand* m_PrevEntityCommand;
        public unsafe EntityCommandBufferChain* m_NextChain;
        public int m_LastSortIndex;
        public unsafe void TemporaryForceDisableBatching()
        {
            this.m_PrevCreateCommand = null;
            this.m_PrevEntityCommand = null;
        }
    }
}

