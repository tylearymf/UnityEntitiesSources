namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    public struct BufferArray<T> where T: struct, IBufferElementData
    {
        private ComponentChunkCache m_Cache;
        private ComponentChunkIterator m_Iterator;
        private readonly bool m_IsReadOnly;
        private readonly int m_Length;
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        private readonly AtomicSafetyHandle m_Safety0;
        private readonly AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;
        public int Length =>
            this.m_Length;
        internal BufferArray(ComponentChunkIterator iterator, int length, bool isReadOnly, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            this.m_Length = length;
            this.m_IsReadOnly = isReadOnly;
            this.m_Iterator = iterator;
            this.m_Cache = new ComponentChunkCache();
            this.m_MinIndex = 0;
            this.m_MaxIndex = length - 1;
            this.m_Safety0 = safety;
            this.m_ArrayInvalidationSafety = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
        }

        public DynamicBuffer<T> this[int index]
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
                if ((index < this.m_MinIndex) || (index > this.m_MaxIndex))
                {
                    this.FailOutOfRangeError(index);
                }
                if ((index < this.m_Cache.CachedBeginIndex) || (index >= this.m_Cache.CachedEndIndex))
                {
                    this.m_Iterator.MoveToEntityIndexAndUpdateCache(index, out this.m_Cache, !this.m_IsReadOnly);
                    if (this.m_Cache.CachedSizeOf < sizeof(BufferHeader))
                    {
                        throw new InvalidOperationException("size cache info is broken");
                    }
                }
                return new DynamicBuffer<T>((BufferHeader*) (this.m_Cache.CachedPtr + (index * this.m_Cache.CachedSizeOf)), this.m_Safety0, this.m_ArrayInvalidationSafety, this.m_IsReadOnly);
            }
        }
        private void FailOutOfRangeError(int index)
        {
            if ((index < this.Length) && ((this.m_MinIndex != 0) || (this.m_MaxIndex != (this.Length - 1))))
            {
                throw new IndexOutOfRangeException($"Index {index} is out of restricted IJobParallelFor range [{this.m_MinIndex}...{this.m_MaxIndex}] in ReadWriteBuffer.
ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.Length}' Length.");
        }
    }
}

