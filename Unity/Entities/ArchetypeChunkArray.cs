namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential, Size=1)]
    public struct ArchetypeChunkArray
    {
        internal static unsafe NativeArray<ArchetypeChunk> Create(NativeList<EntityArchetype> archetypes, Allocator allocator, AtomicSafetyHandle safetyHandle)
        {
            int length = 0;
            int num2 = archetypes.Length;
            NativeArray<int> array = new NativeArray<int>(num2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            int index = 0;
            while (true)
            {
                if (index >= num2)
                {
                    NativeArray<ArchetypeChunk> array2 = new NativeArray<ArchetypeChunk>(length, allocator, NativeArrayOptions.UninitializedMemory);
                    GatherChunks jobData = new GatherChunks {
                        Archetypes = archetypes,
                        Offsets = array,
                        Chunks = array2
                    };
                    JobHandle dependsOn = new JobHandle();
                    jobData.Schedule<GatherChunks>(num2, 1, dependsOn).Complete();
                    array.Dispose();
                    return array2;
                }
                array.set_Item(index, length);
                length += archetypes[index].Archetype.ChunkCount;
                index++;
            }
        }

        public static int CalculateEntityCount(NativeArray<ArchetypeChunk> chunks)
        {
            int num = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                ArchetypeChunk chunk = chunks[i];
                num += chunk.Count;
            }
            return num;
        }
    }
}

