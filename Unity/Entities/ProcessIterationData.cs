namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    internal struct ProcessIterationData
    {
        public ComponentChunkIterator Iterator;
        public int IndexInGroup0;
        public int IndexInGroup1;
        public int IndexInGroup2;
        public int IndexInGroup3;
        public int IsReadOnly0;
        public int IsReadOnly1;
        public int IsReadOnly2;
        public int IsReadOnly3;
        public bool m_IsParallelFor;
        public int m_Length;
        public int m_MinIndex;
        public int m_MaxIndex;
        public int m_SafetyReadOnlyCount;
        public int m_SafetyReadWriteCount;
        public AtomicSafetyHandle m_Safety0;
        public AtomicSafetyHandle m_Safety1;
        public AtomicSafetyHandle m_Safety2;
        public AtomicSafetyHandle m_Safety3;
    }
}

