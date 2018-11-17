namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentChunkIterator
    {
        [NativeDisableUnsafePtrRestriction]
        private unsafe readonly MatchingArchetypes* m_FirstMatchingArchetype;
        [NativeDisableUnsafePtrRestriction]
        private unsafe MatchingArchetypes* m_CurrentMatchingArchetype;
        [NativeDisableUnsafePtrRestriction]
        private unsafe Chunk* m_CurrentChunk;
        private int m_CurrentArchetypeEntityIndex;
        private int m_CurrentChunkEntityIndex;
        private int m_CurrentArchetypeIndex;
        private int m_CurrentChunkIndex;
        private ComponentGroupFilter m_Filter;
        private readonly uint m_GlobalSystemVersion;
        public int IndexInComponentGroup;
        internal unsafe int GetSharedComponentFromCurrentChunk(int sharedComponentIndex)
        {
            int index = &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[sharedComponentIndex];
            int num2 = this.m_CurrentMatchingArchetype.Archetype.SharedComponentOffset[index];
            return this.m_CurrentChunk.SharedComponentValueArray[num2];
        }

        public unsafe ComponentChunkIterator(MatchingArchetypes* match, uint globalSystemVersion, ref ComponentGroupFilter filter)
        {
            this.m_FirstMatchingArchetype = match;
            this.m_CurrentMatchingArchetype = match;
            this.IndexInComponentGroup = -1;
            this.m_CurrentChunk = null;
            this.m_CurrentArchetypeIndex = this.m_CurrentArchetypeEntityIndex = 0x7fffffff;
            this.m_CurrentChunkIndex = this.m_CurrentChunkEntityIndex = 0;
            this.m_GlobalSystemVersion = globalSystemVersion;
            this.m_Filter = filter;
        }

        public unsafe object GetManagedObject(ArchetypeManager typeMan, int typeIndexInArchetype, int cachedBeginIndex, int index) => 
            typeMan.GetManagedObject(this.m_CurrentChunk, typeIndexInArchetype, index - cachedBeginIndex);

        public unsafe object GetManagedObject(ArchetypeManager typeMan, int cachedBeginIndex, int index) => 
            typeMan.GetManagedObject(this.m_CurrentChunk, &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[this.IndexInComponentGroup], index - cachedBeginIndex);

        public unsafe object[] GetManagedObjectRange(ArchetypeManager typeMan, int cachedBeginIndex, int index, out int rangeStart, out int rangeLength)
        {
            object[] objArray = typeMan.GetManagedObjectRange(this.m_CurrentChunk, &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[this.IndexInComponentGroup], out rangeStart, out rangeLength);
            rangeStart += index - cachedBeginIndex;
            rangeLength -= index - cachedBeginIndex;
            return objArray;
        }

        internal static unsafe int CalculateNumberOfChunksWithoutFiltering(MatchingArchetypes* firstMatchingArchetype)
        {
            int num = 0;
            for (MatchingArchetypes* archetypesPtr = firstMatchingArchetype; archetypesPtr != null; archetypesPtr = archetypesPtr->Next)
            {
                num += archetypesPtr->Archetype.ChunkCount;
            }
            return num;
        }

        public static unsafe NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(MatchingArchetypes* firstMatchingArchetype, Allocator allocator, out JobHandle jobHandle)
        {
            GatherChunksJob jobData = new GatherChunksJob {
                matchingArchetype = firstMatchingArchetype,
                chunks = new NativeArray<ArchetypeChunk>(CalculateNumberOfChunksWithoutFiltering(firstMatchingArchetype), allocator, NativeArrayOptions.ClearMemory)
            };
            JobHandle dependsOn = new JobHandle();
            jobHandle = jobData.Schedule<GatherChunksJob>(dependsOn);
            return jobData.chunks;
        }

        public static unsafe int CalculateLength(MatchingArchetypes* firstMatchingArchetype, ref ComponentGroupFilter filter)
        {
            int num = 0;
            if (!filter.RequiresMatchesFilter)
            {
                MatchingArchetypes* next = firstMatchingArchetype;
                while (true)
                {
                    if (next == null)
                    {
                        break;
                    }
                    num += next->Archetype.EntityCount;
                    next = next->Next;
                }
            }
            else
            {
                MatchingArchetypes* match = firstMatchingArchetype;
                while (true)
                {
                    if (match == null)
                    {
                        break;
                    }
                    if (match->Archetype.EntityCount > 0)
                    {
                        Archetype* archetype = match->Archetype;
                        Chunk* begin = (Chunk*) archetype->ChunkList.Begin;
                        while (true)
                        {
                            if (begin == archetype->ChunkList.End)
                            {
                                break;
                            }
                            if (begin.MatchesFilter(match, ref filter))
                            {
                                Assert.IsTrue(begin->Count > 0);
                                num += begin->Count;
                            }
                            begin = (Chunk*) begin->ChunkListNode.Next;
                        }
                    }
                    match = match->Next;
                }
            }
            return num;
        }

        private unsafe void MoveToNextMatchingChunk()
        {
            MatchingArchetypes* currentMatchingArchetype = this.m_CurrentMatchingArchetype;
            Chunk* currentChunk = this.m_CurrentChunk;
            Chunk* end = (Chunk*) currentMatchingArchetype->Archetype.ChunkList.End;
            while (true)
            {
                currentChunk = (Chunk*) currentChunk->ChunkListNode.Next;
                while (true)
                {
                    if (currentChunk == end)
                    {
                        this.m_CurrentArchetypeEntityIndex += this.m_CurrentChunkEntityIndex;
                        this.m_CurrentChunkEntityIndex = 0;
                        currentMatchingArchetype = currentMatchingArchetype->Next;
                        if (currentMatchingArchetype != null)
                        {
                            currentChunk = (Chunk*) currentMatchingArchetype->Archetype.ChunkList.Begin;
                            end = (Chunk*) currentMatchingArchetype->Archetype.ChunkList.End;
                            continue;
                        }
                        this.m_CurrentMatchingArchetype = null;
                        this.m_CurrentChunk = null;
                        break;
                    }
                    else if (currentChunk.MatchesFilter(currentMatchingArchetype, ref this.m_Filter) && (currentChunk->Capacity > 0))
                    {
                        this.m_CurrentMatchingArchetype = currentMatchingArchetype;
                        this.m_CurrentChunk = currentChunk;
                        break;
                    }
                    break;
                }
            }
        }

        public unsafe void MoveToEntityIndex(int index)
        {
            if (this.m_Filter.RequiresMatchesFilter)
            {
                if (index < (this.m_CurrentArchetypeEntityIndex + this.m_CurrentChunkEntityIndex))
                {
                    if (index < this.m_CurrentArchetypeEntityIndex)
                    {
                        this.m_CurrentMatchingArchetype = this.m_FirstMatchingArchetype;
                        this.m_CurrentArchetypeEntityIndex = 0;
                    }
                    this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.End;
                    this.m_CurrentChunkEntityIndex = 0;
                    this.MoveToNextMatchingChunk();
                }
                while (true)
                {
                    if (index < ((this.m_CurrentArchetypeEntityIndex + this.m_CurrentChunkEntityIndex) + this.m_CurrentChunk.Count))
                    {
                        break;
                    }
                    this.m_CurrentChunkEntityIndex += this.m_CurrentChunk.Count;
                    this.MoveToNextMatchingChunk();
                }
            }
            else
            {
                if (index < this.m_CurrentArchetypeEntityIndex)
                {
                    this.m_CurrentMatchingArchetype = this.m_FirstMatchingArchetype;
                    this.m_CurrentArchetypeEntityIndex = 0;
                    this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                    this.m_CurrentChunkEntityIndex = 0;
                }
                while (true)
                {
                    if (index < (this.m_CurrentArchetypeEntityIndex + this.m_CurrentMatchingArchetype.Archetype.EntityCount))
                    {
                        index -= this.m_CurrentArchetypeEntityIndex;
                        if (index < this.m_CurrentChunkEntityIndex)
                        {
                            this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                            this.m_CurrentChunkEntityIndex = 0;
                        }
                        while (true)
                        {
                            if (index < (this.m_CurrentChunkEntityIndex + this.m_CurrentChunk.Count))
                            {
                                break;
                            }
                            this.m_CurrentChunkEntityIndex += this.m_CurrentChunk.Count;
                            this.m_CurrentChunk = (Chunk*) this.m_CurrentChunk.ChunkListNode.Next;
                        }
                        break;
                    }
                    this.m_CurrentArchetypeEntityIndex += this.m_CurrentMatchingArchetype.Archetype.EntityCount;
                    this.m_CurrentMatchingArchetype = this.m_CurrentMatchingArchetype.Next;
                    this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                    this.m_CurrentChunkEntityIndex = 0;
                }
            }
        }

        public unsafe void MoveToChunkWithoutFiltering(int index)
        {
            if (index < this.m_CurrentArchetypeIndex)
            {
                this.m_CurrentMatchingArchetype = this.m_FirstMatchingArchetype;
                this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                this.m_CurrentArchetypeIndex = this.m_CurrentArchetypeEntityIndex = 0;
                this.m_CurrentChunkIndex = this.m_CurrentChunkEntityIndex = 0;
            }
            while (true)
            {
                if (index < (this.m_CurrentArchetypeIndex + this.m_CurrentMatchingArchetype.Archetype.ChunkCount))
                {
                    index -= this.m_CurrentArchetypeIndex;
                    if (index < this.m_CurrentChunkIndex)
                    {
                        this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                        this.m_CurrentChunkIndex = this.m_CurrentChunkEntityIndex = 0;
                    }
                    while (index >= (this.m_CurrentChunkIndex + 1))
                    {
                        this.m_CurrentChunkEntityIndex += this.m_CurrentChunk.Count;
                        this.m_CurrentChunkIndex++;
                        this.m_CurrentChunk = (Chunk*) this.m_CurrentChunk.ChunkListNode.Next;
                    }
                    return;
                }
                this.m_CurrentArchetypeEntityIndex += this.m_CurrentMatchingArchetype.Archetype.EntityCount;
                this.m_CurrentArchetypeIndex += this.m_CurrentMatchingArchetype.Archetype.ChunkCount;
                this.m_CurrentMatchingArchetype = this.m_CurrentMatchingArchetype.Next;
                this.m_CurrentChunk = (Chunk*) this.m_CurrentMatchingArchetype.Archetype.ChunkList.Begin;
                this.m_CurrentChunkIndex = this.m_CurrentChunkEntityIndex = 0;
            }
        }

        public unsafe bool MatchesFilter() => 
            this.m_CurrentChunk.MatchesFilter(this.m_CurrentMatchingArchetype, ref this.m_Filter);

        public bool RequiresFilter() => 
            this.m_Filter.RequiresMatchesFilter;

        public unsafe int GetIndexInArchetypeFromCurrentChunk(int indexInComponentGroup) => 
            &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[indexInComponentGroup];

        public unsafe void UpdateCacheToCurrentChunk(out ComponentChunkCache cache, bool isWriting, int indexInComponentGroup)
        {
            Archetype* archetype = this.m_CurrentMatchingArchetype.Archetype;
            int index = &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[indexInComponentGroup];
            cache.CachedBeginIndex = this.m_CurrentChunkEntityIndex + this.m_CurrentArchetypeEntityIndex;
            cache.CachedEndIndex = cache.CachedBeginIndex + this.m_CurrentChunk.Count;
            cache.CachedSizeOf = archetype->SizeOfs[index];
            cache.CachedPtr = (void*) ((&this.m_CurrentChunk.Buffer.FixedElementField + archetype->Offsets[index]) - (cache.CachedBeginIndex * cache.CachedSizeOf));
            cache.IsWriting = isWriting;
            if (isWriting)
            {
                this.m_CurrentChunk.ChangeVersion[index] = this.m_GlobalSystemVersion;
            }
        }

        public unsafe void UpdateChangeVersion()
        {
            int index = &this.m_CurrentMatchingArchetype.IndexInArchetype.FixedElementField[this.IndexInComponentGroup];
            this.m_CurrentChunk.ChangeVersion[index] = this.m_GlobalSystemVersion;
        }

        public void MoveToEntityIndexAndUpdateCache(int index, out ComponentChunkCache cache, bool isWriting)
        {
            Assert.IsTrue(-1 != this.IndexInComponentGroup);
            this.MoveToEntityIndex(index);
            this.UpdateCacheToCurrentChunk(out cache, isWriting, this.IndexInComponentGroup);
        }

        internal unsafe ArchetypeChunk GetCurrentChunk() => 
            new ArchetypeChunk { m_Chunk = this.m_CurrentChunk };
        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct GatherChunksJob : IJob
        {
            public NativeArray<ArchetypeChunk> chunks;
            [NativeDisableUnsafePtrRestriction]
            public unsafe MatchingArchetypes* matchingArchetype;
            public unsafe void Execute()
            {
                int index = 0;
                while (this.matchingArchetype != null)
                {
                    Archetype* archetype = this.matchingArchetype.Archetype;
                    Chunk* begin = (Chunk*) archetype->ChunkList.Begin;
                    while (true)
                    {
                        if (begin == archetype->ChunkList.End)
                        {
                            this.matchingArchetype = this.matchingArchetype.Next;
                            break;
                        }
                        index++;
                        ArchetypeChunk chunk = new ArchetypeChunk {
                            m_Chunk = begin
                        };
                        this.chunks.set_Item(index, chunk);
                        begin = (Chunk*) begin->ChunkListNode.Next;
                    }
                }
            }
        }
    }
}

