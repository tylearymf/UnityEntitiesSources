namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobChunkExtensions.JobChunkLiveFilter_Process<>))]
    public interface IJobChunk
    {
        void Execute(ArchetypeChunk chunk, int chunkIndex);
    }
}

