namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process2_WE<, , >))]
    public interface IJobProcessComponentDataWithEntity<T0, T1> : IBaseJobProcessComponentData_2_WE, IBaseJobProcessComponentData where T0: struct, IComponentData where T1: struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data0, ref T1 data1);
    }
}

