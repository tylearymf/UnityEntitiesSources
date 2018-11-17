namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential), BurstCompile]
    public struct CopyEntities : IJobParallelFor
    {
        [ReadOnly]
        public EntityArray Source;
        public NativeArray<Entity> Results;
        public void Execute(int index)
        {
            this.Results.set_Item(index, this.Source[index]);
        }
    }
}

