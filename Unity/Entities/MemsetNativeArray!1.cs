namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential), BurstCompile]
    public struct MemsetNativeArray<T> : IJobParallelFor where T: struct
    {
        public NativeArray<T> Source;
        public T Value;
        public void Execute(int index)
        {
            this.Source.set_Item(index, this.Value);
        }
    }
}

