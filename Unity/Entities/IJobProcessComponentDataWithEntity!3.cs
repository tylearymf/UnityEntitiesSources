﻿namespace Unity.Entities
{
    using System;
    using Unity.Jobs.LowLevel.Unsafe;

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process3_WE<, , , >))]
    public interface IJobProcessComponentDataWithEntity<T0, T1, T2> : IBaseJobProcessComponentData_3_WE, IBaseJobProcessComponentData where T0: struct, IComponentData where T1: struct, IComponentData where T2: struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data0, ref T1 data1, ref T2 data2);
    }
}

