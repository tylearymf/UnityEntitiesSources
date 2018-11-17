namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    public struct ArchetypeChunkComponentType<T> where T: struct, IComponentData
    {
        internal readonly int m_TypeIndex;
        internal readonly uint m_GlobalSystemVersion;
        internal readonly bool m_IsReadOnly;
        internal readonly bool m_IsZeroSized;
        private readonly int m_Length;
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        internal readonly AtomicSafetyHandle m_Safety;
        public int TypeIndex =>
            this.m_TypeIndex;
        public uint GlobalSystemVersion =>
            this.m_GlobalSystemVersion;
        public bool IsReadOnly =>
            this.m_IsReadOnly;
        internal ArchetypeChunkComponentType(AtomicSafetyHandle safety, bool isReadOnly, uint globalSystemVersion)
        {
            this.m_Length = 1;
            this.m_TypeIndex = TypeManager.GetTypeIndex<T>();
            this.m_IsZeroSized = TypeManager.GetTypeInfo(this.m_TypeIndex).IsZeroSized;
            this.m_GlobalSystemVersion = globalSystemVersion;
            this.m_IsReadOnly = isReadOnly;
            this.m_MinIndex = 0;
            this.m_MaxIndex = 0;
            this.m_Safety = safety;
        }
    }
}

