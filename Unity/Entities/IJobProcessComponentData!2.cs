namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process2<, , >))]
    public interface IJobProcessComponentData<T0, T1> : IBaseJobProcessComponentData_2, IBaseJobProcessComponentData where T0: struct, IComponentData where T1: struct, IComponentData
    {
        void Execute(ref T0 data0, ref T1 data1);
    }
}

