namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    public struct ArchetypeChunkBufferType<T> where T: struct, IBufferElementData
    {
        internal readonly int m_TypeIndex;
        internal readonly uint m_GlobalSystemVersion;
        internal readonly bool m_IsReadOnly;
        private readonly int m_Length;
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        internal readonly AtomicSafetyHandle m_Safety;
        internal readonly AtomicSafetyHandle m_ArrayInvalidationSafety;
        public int TypeIndex =>
            this.m_TypeIndex;
        public uint GlobalSystemVersion =>
            this.m_GlobalSystemVersion;
        public bool IsReadOnly =>
            this.m_IsReadOnly;
        internal ArchetypeChunkBufferType(AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly, uint globalSystemVersion)
        {
            this.m_Length = 1;
            this.m_TypeIndex = TypeManager.GetTypeIndex<T>();
            this.m_GlobalSystemVersion = globalSystemVersion;
            this.m_IsReadOnly = isReadOnly;
            this.m_MinIndex = 0;
            this.m_MaxIndex = 0;
            this.m_Safety = safety;
            this.m_ArrayInvalidationSafety = arrayInvalidationSafety;
        }
    }
}

