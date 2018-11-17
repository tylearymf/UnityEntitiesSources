namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    public struct ComponentDataArray<T> where T: struct, IComponentData
    {
        private ComponentChunkIterator m_Iterator;
        private ComponentChunkCache m_Cache;
        private readonly int m_Length;
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        private readonly AtomicSafetyHandle m_Safety;
        public int Length =>
            this.m_Length;
        internal ComponentDataArray(ComponentChunkIterator iterator, int length, AtomicSafetyHandle safety)
        {
            this.m_Iterator = iterator;
            this.m_Cache = new ComponentChunkCache();
            this.m_Length = length;
            this.m_MinIndex = 0;
            this.m_MaxIndex = length - 1;
            this.m_Safety = safety;
        }

        internal unsafe void* GetUnsafeChunkPtr(int startIndex, int maxCount, out int actualCount, bool isWriting)
        {
            this.GetUnsafeChunkPtrCheck(startIndex, maxCount);
            this.m_Iterator.MoveToEntityIndexAndUpdateCache(startIndex, out this.m_Cache, isWriting);
            void* voidPtr = this.m_Cache.CachedPtr + (startIndex * this.m_Cache.CachedSizeOf);
            actualCount = Math.Min(maxCount, this.m_Cache.CachedEndIndex - startIndex);
            return voidPtr;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void GetUnsafeChunkPtrCheck(int startIndex, int maxCount)
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
            if (startIndex < this.m_MinIndex)
            {
                this.FailOutOfRangeError(startIndex);
            }
            else if ((startIndex + maxCount) > (this.m_MaxIndex + 1))
            {
                this.FailOutOfRangeError(startIndex + maxCount);
            }
        }

        public unsafe NativeArray<T> GetChunkArray(int startIndex, int maxCount)
        {
            int num;
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(this.GetUnsafeChunkPtr(startIndex, maxCount, out num, true), num, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref array, this.m_Safety);
            return array;
        }

        public void CopyTo(NativeSlice<T> dst, int startIndex = 0)
        {
            NativeArray<T> chunkArray;
            for (int i = 0; i < dst.Length; i += chunkArray.Length)
            {
                chunkArray = this.GetChunkArray(startIndex + i, dst.Length - i);
                dst.Slice<T>(i, chunkArray.Length).CopyFrom(chunkArray);
            }
        }

        public T this[int index]
        {
            get
            {
                this.GetValueCheck(index);
                if ((index < this.m_Cache.CachedBeginIndex) || (index >= this.m_Cache.CachedEndIndex))
                {
                    this.m_Iterator.MoveToEntityIndexAndUpdateCache(index, out this.m_Cache, false);
                }
                return UnsafeUtility.ReadArrayElement<T>(this.m_Cache.CachedPtr, index);
            }
            set
            {
                this.SetValueCheck(index);
                if ((index < this.m_Cache.CachedBeginIndex) || (index >= this.m_Cache.CachedEndIndex))
                {
                    this.m_Iterator.MoveToEntityIndexAndUpdateCache(index, out this.m_Cache, true);
                }
                else if (!this.m_Cache.IsWriting)
                {
                    this.m_Cache.IsWriting = true;
                    this.m_Iterator.UpdateChangeVersion();
                }
                UnsafeUtility.WriteArrayElement<T>(this.m_Cache.CachedPtr, index, value);
            }
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void SetValueCheck(int index)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
            if ((index < this.m_MinIndex) || (index > this.m_MaxIndex))
            {
                this.FailOutOfRangeError(index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void GetValueCheck(int index)
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
            if ((index < this.m_MinIndex) || (index > this.m_MaxIndex))
            {
                this.FailOutOfRangeError(index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if ((index < this.Length) && ((this.m_MinIndex != 0) || (this.m_MaxIndex != (this.Length - 1))))
            {
                throw new IndexOutOfRangeException($"Index {index} is out of restricted IJobParallelFor range [{this.m_MinIndex}...{this.m_MaxIndex}] in ReadWriteBuffer.
" + "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.Length}' Length.");
        }
    }
}

