namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential), BurstCompile]
    public struct CopyComponentData<T> : IJobParallelFor where T: struct, IComponentData
    {
        [ReadOnly]
        public ComponentDataArray<T> Source;
        public NativeArray<T> Results;
        public void Execute(int index)
        {
            this.Results.set_Item(index, this.Source[index]);
        }
    }
}

