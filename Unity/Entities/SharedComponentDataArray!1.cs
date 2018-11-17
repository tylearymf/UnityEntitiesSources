namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public struct SharedComponentDataArray<T> where T: struct, ISharedComponentData
    {
        private ComponentChunkIterator m_Iterator;
        private ComponentChunkCache m_Cache;
        private readonly SharedComponentDataManager m_sharedComponentDataManager;
        private readonly int m_sharedComponentIndex;
        private readonly AtomicSafetyHandle m_Safety;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int <Length>k__BackingField;
        internal SharedComponentDataArray(SharedComponentDataManager sharedComponentDataManager, int sharedComponentIndex, ComponentChunkIterator iterator, int length, AtomicSafetyHandle safety)
        {
            this.m_sharedComponentDataManager = sharedComponentDataManager;
            this.m_sharedComponentIndex = sharedComponentIndex;
            this.m_Iterator = iterator;
            this.m_Cache = new ComponentChunkCache();
            this.<Length>k__BackingField = length;
            this.m_Safety = safety;
        }

        public T this[int index]
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
                if (index >= this.Length)
                {
                    this.FailOutOfRangeError(index);
                }
                if ((index < this.m_Cache.CachedBeginIndex) || (index >= this.m_Cache.CachedEndIndex))
                {
                    this.m_Iterator.MoveToEntityIndexAndUpdateCache(index, out this.m_Cache, false);
                }
                int sharedComponentFromCurrentChunk = this.m_Iterator.GetSharedComponentFromCurrentChunk(this.m_sharedComponentIndex);
                return this.m_sharedComponentDataManager.GetSharedComponentData<T>(sharedComponentFromCurrentChunk);
            }
        }
        private void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.Length}' Length.");
        }

        public int Length =>
            this.<Length>k__BackingField;
    }
}

