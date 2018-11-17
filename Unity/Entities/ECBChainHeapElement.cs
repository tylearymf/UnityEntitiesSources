namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ECBChainHeapElement
    {
        public int SortIndex;
        public int ChainIndex;
    }
}

