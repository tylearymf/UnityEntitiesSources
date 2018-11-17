namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerIsReadOnly]
    public struct EntityArray
    {
        private ComponentChunkIterator m_Iterator;
        private ComponentChunkCache m_Cache;
        private readonly int m_Length;
        private readonly AtomicSafetyHandle m_Safety;
        public int Length =>
            this.m_Length;
        internal EntityArray(ComponentChunkIterator iterator, int length, AtomicSafetyHandle safety)
        {
            this.m_Length = length;
            this.m_Iterator = iterator;
            this.m_Cache = new ComponentChunkCache();
            this.m_Safety = safety;
        }

        public Entity this[int index]
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
                if (index >= this.m_Length)
                {
                    this.FailOutOfRangeError(index);
                }
                if ((index < this.m_Cache.CachedBeginIndex) || (index >= this.m_Cache.CachedEndIndex))
                {
                    this.m_Iterator.MoveToEntityIndexAndUpdateCache(index, out this.m_Cache, false);
                }
                return UnsafeUtility.ReadArrayElement<Entity>(this.m_Cache.CachedPtr, index);
            }
        }
        private void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.Length}' Length.");
        }

        public unsafe NativeArray<Entity> GetChunkArray(int startIndex, int maxCount)
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
            if (startIndex < 0)
            {
                this.FailOutOfRangeError(startIndex);
            }
            else if ((startIndex + maxCount) > this.m_Length)
            {
                this.FailOutOfRangeError(startIndex + maxCount);
            }
            this.m_Iterator.MoveToEntityIndexAndUpdateCache(startIndex, out this.m_Cache, false);
            NativeArray<Entity> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(this.m_Cache.CachedPtr + (startIndex * this.m_Cache.CachedSizeOf), Math.Min(maxCount, this.m_Cache.CachedEndIndex - startIndex), Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<Entity>(ref array, this.m_Safety);
            return array;
        }

        public void CopyTo(NativeSlice<Entity> dst, int startIndex = 0)
        {
            NativeArray<Entity> chunkArray;
            for (int i = 0; i < dst.Length; i += chunkArray.Length)
            {
                chunkArray = this.GetChunkArray(startIndex + i, dst.Length - i);
                dst.Slice<Entity>(i, chunkArray.Length).CopyFrom(chunkArray);
            }
        }

        public Entity[] ToArray()
        {
            Entity[] entityArray = new Entity[this.Length];
            for (int i = 0; i != entityArray.Length; i++)
            {
                entityArray[i] = this[i];
            }
            return entityArray;
        }
    }
}

