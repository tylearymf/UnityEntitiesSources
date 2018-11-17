namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentChunkCache
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe void* CachedPtr;
        public int CachedBeginIndex;
        public int CachedEndIndex;
        public int CachedSizeOf;
        public bool IsWriting;
    }
}

