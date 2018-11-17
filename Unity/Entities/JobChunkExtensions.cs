namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    public static class JobChunkExtensions
    {
        public static void Run<T>(this T jobData, ComponentGroup group) where T: struct, IJobChunk
        {
            JobHandle dependsOn = new JobHandle();
            ScheduleInternal<T>(ref jobData, group, dependsOn, ScheduleMode.Run);
        }

        public static JobHandle Schedule<T>(this T jobData, ComponentGroup group, JobHandle dependsOn = new JobHandle()) where T: struct, IJobChunk => 
            ScheduleInternal<T>(ref jobData, group, dependsOn, ScheduleMode.Batched);

        internal static unsafe JobHandle ScheduleInternal<T>(ref T jobData, ComponentGroup group, JobHandle dependsOn, ScheduleMode mode) where T: struct, IJobChunk
        {
            ComponentChunkIterator iterator;
            group.GetComponentChunkIterator(out iterator);
            JobDataLiveFilter<T> output = new JobDataLiveFilter<T> {
                data = jobData,
                iterator = iterator
            };
            JobsUtility.JobScheduleParameters parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf<JobDataLiveFilter<T>>(ref output), JobChunkLiveFilter_Process<T>.Initialize(), dependsOn, mode);
            return JobsUtility.ScheduleParallelFor(ref parameters, group.CalculateNumberOfChunksWithoutFiltering(), 1);
        }

        [StructLayout(LayoutKind.Sequential, Size=1)]
        internal struct JobChunkLiveFilter_Process<T> where T: struct, IJobChunk
        {
            public static IntPtr jobReflectionData;
            public static IntPtr Initialize()
            {
                if (JobChunkExtensions.JobChunkLiveFilter_Process<T>.jobReflectionData == IntPtr.Zero)
                {
                    JobChunkExtensions.JobChunkLiveFilter_Process<T>.jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobChunkExtensions.JobDataLiveFilter<T>), typeof(T), JobType.ParallelFor, new ExecuteJobFunction<T>(JobChunkExtensions.JobChunkLiveFilter_Process<T>.Execute));
                }
                return JobChunkExtensions.JobChunkLiveFilter_Process<T>.jobReflectionData;
            }

            public static void Execute(ref JobChunkExtensions.JobDataLiveFilter<T> jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                JobChunkExtensions.JobChunkLiveFilter_Process<T>.ExecuteInternal(ref jobData, ref ranges, jobIndex);
            }

            internal static void ExecuteInternal(ref JobChunkExtensions.JobDataLiveFilter<T> jobData, ref JobRanges ranges, int jobIndex)
            {
                int num;
                int num2;
                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out num, out num2))
                {
                    jobData.iterator.MoveToChunkWithoutFiltering(num);
                    if (jobData.iterator.MatchesFilter())
                    {
                        ArchetypeChunk currentChunk = jobData.iterator.GetCurrentChunk();
                        jobData.data.Execute(currentChunk, num);
                    }
                }
            }
            public delegate void ExecuteJobFunction(ref JobChunkExtensions.JobDataLiveFilter<T> data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobDataLiveFilter<T> where T: struct
        {
            public ComponentChunkIterator iterator;
            public T data;
        }
    }
}

