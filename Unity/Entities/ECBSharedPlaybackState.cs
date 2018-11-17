namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ECBSharedPlaybackState
    {
        public int CreateEntityBatchOffset;
        public int InstantiateEntityBatchOffset;
        public unsafe Entity* CreateEntityBatch;
        public unsafe Entity* InstantiateEntityBatch;
        public Entity CurrentEntity;
    }
}

