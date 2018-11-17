namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    public struct ArchetypeChunkEntityType
    {
        private readonly int m_Length;
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        internal readonly AtomicSafetyHandle m_Safety;
        internal ArchetypeChunkEntityType(AtomicSafetyHandle safety)
        {
            this.m_Length = 1;
            this.m_MinIndex = 0;
            this.m_MaxIndex = 0;
            this.m_Safety = safety;
        }
    }
}

