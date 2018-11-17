namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process1<, >))]
    public interface IJobProcessComponentData<T0> : IBaseJobProcessComponentData_1, IBaseJobProcessComponentData where T0: struct, IComponentData
    {
        void Execute(ref T0 data);
    }
}

