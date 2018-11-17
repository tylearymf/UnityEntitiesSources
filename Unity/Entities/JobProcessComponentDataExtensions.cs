namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.Scripting;

    public static class JobProcessComponentDataExtensions
    {
        public static ComponentGroup GetComponentGroupForIJobProcessComponentData(this ComponentSystemBase system, Type jobType)
        {
            ComponentGroup componentGroupInternal;
            ComponentType[] componentTypes = IJobProcessComponentDataUtility.GetComponentTypes(jobType);
            if (componentTypes != null)
            {
                componentGroupInternal = system.GetComponentGroupInternal(componentTypes);
            }
            else
            {
                componentGroupInternal = null;
            }
            return componentGroupInternal;
        }

        public static void PrepareComponentGroup<T>(this T jobData, ComponentSystemBase system) where T: struct, IBaseJobProcessComponentData
        {
            IJobProcessComponentDataUtility.PrepareComponentGroup(system, typeof(T));
        }

        public static void Run<T>(this T jobData, ComponentSystemBase system) where T: struct, IBaseJobProcessComponentData
        {
            JobHandle handle;
            Type c = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_1<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_1_WE<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_2<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_2_WE<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_3<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_3_WE<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(c))
            {
                handle = new JobHandle();
                ScheduleInternal_4<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
            else
            {
                if (!typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(c))
                {
                    throw new ArgumentException("Not supported");
                }
                handle = new JobHandle();
                ScheduleInternal_4_WE<T>(ref jobData, system, -1, handle, ScheduleMode.Run);
            }
        }

        public static JobHandle Schedule<T>(this T jobData, ComponentSystemBase system, JobHandle dependsOn = new JobHandle()) where T: struct, IBaseJobProcessComponentData
        {
            JobHandle handle;
            Type c = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_1<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_1_WE<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_2<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_2_WE<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_3<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_3_WE<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_4<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            else
            {
                if (!typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(c))
                {
                    throw new ArgumentException("Not supported");
                }
                handle = ScheduleInternal_4_WE<T>(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            }
            return handle;
        }

        private static unsafe JobHandle Schedule(void* fullData, int length, int innerloopBatchCount, bool isParallelFor, ref JobProcessComponentDataCache cache, JobHandle dependsOn, ScheduleMode mode)
        {
            JobHandle handle;
            if (isParallelFor)
            {
                JobsUtility.JobScheduleParameters parameters = new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionDataParallelFor, dependsOn, mode);
                handle = JobsUtility.ScheduleParallelFor(ref parameters, length, innerloopBatchCount);
            }
            else
            {
                JobsUtility.JobScheduleParameters parameters = new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionData, dependsOn, mode);
                handle = JobsUtility.Schedule(ref parameters);
            }
            return handle;
        }

        internal static unsafe JobHandle ScheduleInternal_1<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_1<T> r_;
            r_.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process1), isParallelFor, ref JobStruct_ProcessInfer_1<T>.Cache, out r_.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_1<T>>(ref r_);
            return Schedule(fullData, r_.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_1<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_1_WE<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_1_WE<T> r__we;
            r__we.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process1_WE), isParallelFor, ref JobStruct_ProcessInfer_1_WE<T>.Cache, out r__we.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_1_WE<T>>(ref r__we);
            return Schedule(fullData, r__we.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_1_WE<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_2<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_2<T> r_;
            r_.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process2), isParallelFor, ref JobStruct_ProcessInfer_2<T>.Cache, out r_.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_2<T>>(ref r_);
            return Schedule(fullData, r_.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_2<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_2_WE<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_2_WE<T> r__we;
            r__we.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process2_WE), isParallelFor, ref JobStruct_ProcessInfer_2_WE<T>.Cache, out r__we.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_2_WE<T>>(ref r__we);
            return Schedule(fullData, r__we.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_2_WE<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_3<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_3<T> r_;
            r_.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process3), isParallelFor, ref JobStruct_ProcessInfer_3<T>.Cache, out r_.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_3<T>>(ref r_);
            return Schedule(fullData, r_.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_3<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_3_WE<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_3_WE<T> r__we;
            r__we.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process3_WE), isParallelFor, ref JobStruct_ProcessInfer_3_WE<T>.Cache, out r__we.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_3_WE<T>>(ref r__we);
            return Schedule(fullData, r__we.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_3_WE<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_4<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_4<T> r_;
            r_.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process4), isParallelFor, ref JobStruct_ProcessInfer_4<T>.Cache, out r_.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_4<T>>(ref r_);
            return Schedule(fullData, r_.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_4<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_4_WE<T>(ref T jobData, ComponentSystemBase system, int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode) where T: struct
        {
            JobStruct_ProcessInfer_4_WE<T> r__we;
            r__we.Data = jobData;
            bool isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process4_WE), isParallelFor, ref JobStruct_ProcessInfer_4_WE<T>.Cache, out r__we.Iterator);
            void* fullData = ref UnsafeUtility.AddressOf<JobStruct_ProcessInfer_4_WE<T>>(ref r__we);
            return Schedule(fullData, r__we.Iterator.m_Length, innerloopBatchCount, isParallelFor, ref JobStruct_ProcessInfer_4_WE<T>.Cache, dependsOn, mode);
        }

        public static JobHandle ScheduleSingle<T>(this T jobData, ComponentSystemBase system, JobHandle dependsOn = new JobHandle()) where T: struct, IBaseJobProcessComponentData
        {
            JobHandle handle;
            Type c = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_1<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_1_WE<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_2<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_2_WE<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_3<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_3_WE<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(c))
            {
                handle = ScheduleInternal_4<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            else
            {
                if (!typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(c))
                {
                    throw new ArgumentException("Not supported");
                }
                handle = ScheduleInternal_4_WE<T>(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            }
            return handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process1<T, U0> where T: struct, IJobProcessComponentData<U0> where U0: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process1<T, U0>), typeof(T), jobType, new ExecuteJobFunction<T, U0>(JobProcessComponentDataExtensions.JobStruct_Process1<T, U0>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process1<T, U0> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process1<T, U0>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 data = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            jobData.Data.Execute(ref data);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process1<T, U0> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process1<T, U0>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process1<T, U0>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process1<T, U0> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process1_WE<T, U0> where T: struct, IJobProcessComponentDataWithEntity<U0> where U0: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0>), typeof(T), jobType, new ExecuteJobFunction<T, U0>(JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, false, 0);
                        Entity* entityPtr = (Entity*) UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0>>(ref jobData), cache2.CachedBeginIndex, cache2.CachedEndIndex - cache2.CachedBeginIndex);
                        int cachedBeginIndex = cache2.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache2.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 data = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            jobData.Data.Execute(entityPtr[cachedBeginIndex], cachedBeginIndex, ref data);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process1_WE<T, U0> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process2<T, U0, U1> where T: struct, IJobProcessComponentData<U0, U1> where U0: struct, IComponentData where U1: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1>(JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            jobData.Data.Execute(ref localRef, ref localRef2);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process2<T, U0, U1> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process2_WE<T, U0, U1> where T: struct, IJobProcessComponentDataWithEntity<U0, U1> where U0: struct, IComponentData where U1: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1>(JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        ComponentChunkCache cache3;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, false, 0);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        Entity* entityPtr = (Entity*) UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            jobData.Data.Execute(entityPtr[cachedBeginIndex], cachedBeginIndex, ref localRef, ref localRef2);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process2_WE<T, U0, U1> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process3<T, U0, U1, U2> where T: struct, IJobProcessComponentData<U0, U1, U2> where U0: struct, IComponentData where U1: struct, IComponentData where U2: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1, U2>(JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        ComponentChunkCache cache3;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        void* voidPtr3 = ref UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            ref U2 localRef3 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(voidPtr3, cachedBeginIndex);
                            jobData.Data.Execute(ref localRef, ref localRef2, ref localRef3);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process3<T, U0, U1, U2> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process3_WE<T, U0, U1, U2> where T: struct, IJobProcessComponentDataWithEntity<U0, U1, U2> where U0: struct, IComponentData where U1: struct, IComponentData where U2: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1, U2>(JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        ComponentChunkCache cache3;
                        ComponentChunkCache cache4;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache4, false, 0);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        void* voidPtr3 = ref UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                        Entity* entityPtr = (Entity*) UnsafeUtilityEx.RestrictNoAlias(cache4.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            ref U2 localRef3 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(voidPtr3, cachedBeginIndex);
                            jobData.Data.Execute(entityPtr[cachedBeginIndex], cachedBeginIndex, ref localRef, ref localRef2, ref localRef3);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process3_WE<T, U0, U1, U2> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process4<T, U0, U1, U2, U3> where T: struct, IJobProcessComponentData<U0, U1, U2, U3> where U0: struct, IComponentData where U1: struct, IComponentData where U2: struct, IComponentData where U3: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1, U2, U3>(JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        ComponentChunkCache cache3;
                        ComponentChunkCache cache4;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache4, jobData.Iterator.IsReadOnly3 == 0, jobData.Iterator.IndexInGroup3);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        void* voidPtr3 = ref UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                        void* voidPtr4 = ref UnsafeUtilityEx.RestrictNoAlias(cache4.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            ref U2 localRef3 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(voidPtr3, cachedBeginIndex);
                            ref U3 localRef4 = ref UnsafeUtilityEx.ArrayElementAsRef<U3>(voidPtr4, cachedBeginIndex);
                            jobData.Data.Execute(ref localRef, ref localRef2, ref localRef3, ref localRef4);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process4<T, U0, U1, U2, U3> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process4_WE<T, U0, U1, U2, U3> where T: struct, IJobProcessComponentDataWithEntity<U0, U1, U2, U3> where U0: struct, IComponentData where U1: struct, IComponentData where U2: struct, IComponentData where U3: struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;
            [Preserve]
            public static IntPtr Initialize(JobType jobType) => 
                JobsUtility.CreateJobReflectionData(typeof(JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3>), typeof(T), jobType, new ExecuteJobFunction<T, U0, U1, U2, U3>(JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3>.Execute));

            private static unsafe void ExecuteChunk(ref JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                for (int i = begin; i != end; i++)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(i);
                    if (jobData.Iterator.Iterator.MatchesFilter())
                    {
                        ComponentChunkCache cache;
                        ComponentChunkCache cache2;
                        ComponentChunkCache cache3;
                        ComponentChunkCache cache4;
                        ComponentChunkCache cache5;
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache4, jobData.Iterator.IsReadOnly3 == 0, jobData.Iterator.IndexInGroup3);
                        jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache5, false, 0);
                        void* ptr = ref UnsafeUtilityEx.RestrictNoAlias(cache.CachedPtr);
                        void* voidPtr2 = ref UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                        void* voidPtr3 = ref UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                        void* voidPtr4 = ref UnsafeUtilityEx.RestrictNoAlias(cache4.CachedPtr);
                        Entity* entityPtr = (Entity*) UnsafeUtilityEx.RestrictNoAlias(cache5.CachedPtr);
                        JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf<JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3>>(ref jobData), cache.CachedBeginIndex, cache.CachedEndIndex - cache.CachedBeginIndex);
                        int cachedBeginIndex = cache.CachedBeginIndex;
                        while (true)
                        {
                            if (cachedBeginIndex == cache.CachedEndIndex)
                            {
                                break;
                            }
                            ref U0 localRef = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr, cachedBeginIndex);
                            ref U1 localRef2 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(voidPtr2, cachedBeginIndex);
                            ref U2 localRef3 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(voidPtr3, cachedBeginIndex);
                            ref U3 localRef4 = ref UnsafeUtilityEx.ArrayElementAsRef<U3>(voidPtr4, cachedBeginIndex);
                            jobData.Data.Execute(entityPtr[cachedBeginIndex], cachedBeginIndex, ref localRef, ref localRef2, ref localRef3, ref localRef4);
                            cachedBeginIndex++;
                        }
                    }
                }
            }

            public static void Execute(ref JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (!jobData.Iterator.m_IsParallelFor)
                {
                    JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3>.ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
                else
                {
                    while (true)
                    {
                        int num;
                        int num2;
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                        {
                            break;
                        }
                        JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3>.ExecuteChunk(ref jobData, bufferRangePatchData, num, num2);
                    }
                }
            }
            private delegate void ExecuteJobFunction(ref JobProcessComponentDataExtensions.JobStruct_Process4_WE<T, U0, U1, U2, U3> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_1<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_1_WE<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_2<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_2_WE<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_3<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_3_WE<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_4<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_4_WE<T> where T: struct
        {
            public static JobProcessComponentDataCache Cache;
            public ProcessIterationData Iterator;
            public T Data;
        }
    }
}

