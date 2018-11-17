namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process1_WE<, >))]
    public interface IJobProcessComponentDataWithEntity<T0> : IBaseJobProcessComponentData_1_WE, IBaseJobProcessComponentData where T0: struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data);
    }
}

