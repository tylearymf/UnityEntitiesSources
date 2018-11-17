namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential), BurstCompile]
    internal struct GatherChunks : IJobParallelFor
    {
        [ReadOnly]
        public NativeList<EntityArchetype> Archetypes;
        [ReadOnly]
        public NativeArray<int> Offsets;
        [NativeDisableParallelForRestriction]
        public NativeArray<ArchetypeChunk> Chunks;
        public unsafe void Execute(int index)
        {
            EntityArchetype archetype = this.Archetypes[index];
            int chunkCount = archetype.Archetype.ChunkCount;
            Chunk* begin = (Chunk*) archetype.Archetype.ChunkList.Begin;
            int num2 = this.Offsets[index];
            Chunk** unsafePtr = (Chunk**) this.Chunks.GetUnsafePtr<ArchetypeChunk>();
            for (int i = 0; i < chunkCount; i++)
            {
                *((IntPtr*) (unsafePtr + (num2 + i))) = begin;
                begin = (Chunk*) begin->ChunkListNode.Next;
            }
        }
    }
}

