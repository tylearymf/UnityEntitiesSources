namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.Profiling;

    internal class ComponentJobSafetyManager
    {
        private const int kMaxReadJobHandles = 0x11;
        private const int kMaxTypes = 0x2800;
        private unsafe readonly JobHandle* m_JobDependencyCombineBuffer;
        private readonly int m_JobDependencyCombineBufferCount;
        private unsafe ComponentSafetyHandle* m_ComponentSafetyHandles;
        private JobHandle m_ExclusiveTransactionDependency;
        private bool m_HasCleanHandles;
        private unsafe JobHandle* m_ReadJobFences = ((JobHandle*) UnsafeUtility.Malloc((long) ((sizeof(JobHandle) * 0x11) * 0x2800), 0x10, Allocator.Persistent));
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool <IsInTransaction>k__BackingField;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AtomicSafetyHandle <ExclusiveTransactionSafety>k__BackingField;
        private readonly AtomicSafetyHandle m_TempSafety = AtomicSafetyHandle.Create();

        public unsafe ComponentJobSafetyManager()
        {
            UnsafeUtility.MemClear((void*) this.m_ReadJobFences, (long) ((sizeof(JobHandle) * 0x11) * 0x2800));
            this.m_ComponentSafetyHandles = (ComponentSafetyHandle*) UnsafeUtility.Malloc((long) (sizeof(ComponentSafetyHandle) * 0x2800), 0x10, Allocator.Persistent);
            UnsafeUtility.MemClear((void*) this.m_ComponentSafetyHandles, (long) (sizeof(ComponentSafetyHandle) * 0x2800));
            this.m_JobDependencyCombineBufferCount = 0x1000;
            this.m_JobDependencyCombineBuffer = (JobHandle*) UnsafeUtility.Malloc((long) (sizeof(ComponentSafetyHandle) * this.m_JobDependencyCombineBufferCount), 0x10, Allocator.Persistent);
            int index = 0;
            while (true)
            {
                if (index == 0x2800)
                {
                    this.m_HasCleanHandles = true;
                    return;
                }
                this.m_ComponentSafetyHandles[index].SafetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(this.m_ComponentSafetyHandles[index].SafetyHandle, false);
                this.m_ComponentSafetyHandles[index].BufferHandle = AtomicSafetyHandle.Create();
                index++;
            }
        }

        public unsafe JobHandle AddDependency(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount, JobHandle dependency)
        {
            JobHandle* jobs = null;
            int count = 0;
            int index = 0;
            while (true)
            {
                if (index == writerTypesCount)
                {
                    int num4 = 0;
                    while (true)
                    {
                        if (num4 == readerTypesCount)
                        {
                            JobHandle handle2;
                            if ((readerTypesCount != 0) || (writerTypesCount != 0))
                            {
                                this.m_HasCleanHandles = false;
                            }
                            if (jobs != null)
                            {
                                handle2 = JobHandleUnsafeUtility.CombineDependencies(jobs, count);
                            }
                            else
                            {
                                handle2 = dependency;
                            }
                            return handle2;
                        }
                        int num5 = readerTypes[num4];
                        this.m_ReadJobFences[(num5 * 0x11) + this.m_ComponentSafetyHandles[num5].NumReadFences] = dependency;
                        int* numPtr1 = (int*) ref this.m_ComponentSafetyHandles[num5].NumReadFences;
                        numPtr1[0]++;
                        if (this.m_ComponentSafetyHandles[num5].NumReadFences == 0x11)
                        {
                            JobHandle handle = this.CombineReadDependencies(num5);
                            if (jobs == null)
                            {
                                jobs = (JobHandle*) stackalloc byte[(((IntPtr) readerTypesCount) * sizeof(JobHandle))];
                            }
                            count++;
                            jobs[count] = handle;
                        }
                        num4++;
                    }
                }
                int num3 = writerTypes[index];
                this.m_ComponentSafetyHandles[num3].WriteFence = dependency;
                index++;
            }
        }

        public unsafe void BeginExclusiveTransaction()
        {
            if (!this.IsInTransaction)
            {
                int index = 0;
                while (true)
                {
                    if (index == TypeManager.GetTypeCount())
                    {
                        this.IsInTransaction = true;
                        this.ExclusiveTransactionSafety = AtomicSafetyHandle.Create();
                        this.m_ExclusiveTransactionDependency = this.GetAllDependencies();
                        int num2 = 0;
                        while (true)
                        {
                            if (num2 == TypeManager.GetTypeCount())
                            {
                                break;
                            }
                            AtomicSafetyHandle.Release(this.m_ComponentSafetyHandles[num2].SafetyHandle);
                            AtomicSafetyHandle.Release(this.m_ComponentSafetyHandles[num2].BufferHandle);
                            num2++;
                        }
                        break;
                    }
                    AtomicSafetyHandle.CheckDeallocateAndThrow(this.m_ComponentSafetyHandles[index].SafetyHandle);
                    AtomicSafetyHandle.CheckDeallocateAndThrow(this.m_ComponentSafetyHandles[index].BufferHandle);
                    index++;
                }
            }
        }

        private unsafe JobHandle CombineReadDependencies(int type)
        {
            JobHandle handle = JobHandleUnsafeUtility.CombineDependencies(this.m_ReadJobFences + (type * 0x11), this.m_ComponentSafetyHandles[type].NumReadFences);
            this.m_ReadJobFences[type * 0x11] = handle;
            this.m_ComponentSafetyHandles[type].NumReadFences = 1;
            return handle;
        }

        public unsafe void CompleteAllJobsAndInvalidateArrays()
        {
            if (!this.m_HasCleanHandles)
            {
                Profiler.BeginSample("CompleteAllJobsAndInvalidateArrays");
                int typeCount = TypeManager.GetTypeCount();
                int index = 0;
                while (true)
                {
                    if (index == typeCount)
                    {
                        int num5 = 0;
                        while (true)
                        {
                            if (num5 == typeCount)
                            {
                                int num6 = 0;
                                while (true)
                                {
                                    if (num6 == typeCount)
                                    {
                                        this.m_HasCleanHandles = true;
                                        Profiler.EndSample();
                                        break;
                                    }
                                    AtomicSafetyHandle.Release(this.m_ComponentSafetyHandles[num6].SafetyHandle);
                                    this.m_ComponentSafetyHandles[num6].SafetyHandle = AtomicSafetyHandle.Create();
                                    AtomicSafetyHandle.SetAllowSecondaryVersionWriting(this.m_ComponentSafetyHandles[num6].SafetyHandle, false);
                                    AtomicSafetyHandle.Release(this.m_ComponentSafetyHandles[num6].BufferHandle);
                                    this.m_ComponentSafetyHandles[num6].BufferHandle = AtomicSafetyHandle.Create();
                                    num6++;
                                }
                                break;
                            }
                            AtomicSafetyHandle.CheckDeallocateAndThrow(this.m_ComponentSafetyHandles[num5].SafetyHandle);
                            AtomicSafetyHandle.CheckDeallocateAndThrow(this.m_ComponentSafetyHandles[num5].BufferHandle);
                            num5++;
                        }
                        break;
                    }
                    this.m_ComponentSafetyHandles[index].WriteFence.Complete();
                    int numReadFences = this.m_ComponentSafetyHandles[index].NumReadFences;
                    JobHandle* handlePtr = this.m_ReadJobFences + (index * 0x11);
                    int num4 = 0;
                    while (true)
                    {
                        if (num4 == numReadFences)
                        {
                            this.m_ComponentSafetyHandles[index].NumReadFences = 0;
                            index++;
                            break;
                        }
                        (handlePtr + num4).Complete();
                        num4++;
                    }
                }
            }
        }

        public unsafe void CompleteDependenciesNoChecks(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount)
        {
            int index = 0;
            while (true)
            {
                if (index == writerTypesCount)
                {
                    for (int i = 0; i != readerTypesCount; i++)
                    {
                        this.CompleteWriteDependencyNoChecks(readerTypes[i]);
                    }
                    return;
                }
                this.CompleteReadAndWriteDependencyNoChecks(writerTypes[index]);
                index++;
            }
        }

        public unsafe void CompleteReadAndWriteDependency(int type)
        {
            this.CompleteReadAndWriteDependencyNoChecks(type);
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_ComponentSafetyHandles[type].SafetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_ComponentSafetyHandles[type].BufferHandle);
        }

        public unsafe void CompleteReadAndWriteDependencyNoChecks(int type)
        {
            int num = 0;
            while (true)
            {
                if (num >= this.m_ComponentSafetyHandles[type].NumReadFences)
                {
                    this.m_ComponentSafetyHandles[type].NumReadFences = 0;
                    this.m_ComponentSafetyHandles[type].WriteFence.Complete();
                    return;
                }
                (this.m_ReadJobFences + ((type * 0x11) + num)).Complete();
                num++;
            }
        }

        public unsafe void CompleteWriteDependency(int type)
        {
            this.CompleteWriteDependencyNoChecks(type);
            AtomicSafetyHandle.CheckReadAndThrow(this.m_ComponentSafetyHandles[type].SafetyHandle);
            AtomicSafetyHandle.CheckReadAndThrow(this.m_ComponentSafetyHandles[type].BufferHandle);
        }

        public unsafe void CompleteWriteDependencyNoChecks(int type)
        {
            this.m_ComponentSafetyHandles[type].WriteFence.Complete();
        }

        public unsafe void Dispose()
        {
            int index = 0;
            while (true)
            {
                if (index >= 0x2800)
                {
                    int num2 = 0;
                    while (true)
                    {
                        if (num2 >= 0x2a800)
                        {
                            int num3 = 0;
                            while (true)
                            {
                                if (num3 >= 0x2800)
                                {
                                    AtomicSafetyHandle.Release(this.m_TempSafety);
                                    UnsafeUtility.Free((void*) this.m_JobDependencyCombineBuffer, Allocator.Persistent);
                                    UnsafeUtility.Free((void*) this.m_ComponentSafetyHandles, Allocator.Persistent);
                                    this.m_ComponentSafetyHandles = null;
                                    UnsafeUtility.Free((void*) this.m_ReadJobFences, Allocator.Persistent);
                                    this.m_ReadJobFences = null;
                                    return;
                                }
                                EnforceJobResult result = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(this.m_ComponentSafetyHandles[num3].SafetyHandle);
                                EnforceJobResult result2 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(this.m_ComponentSafetyHandles[num3].BufferHandle);
                                if ((result == EnforceJobResult.DidSyncRunningJobs) || (result2 == EnforceJobResult.DidSyncRunningJobs))
                                {
                                    Unity.Debug.LogError("Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with JobComponentSystem.AddDependency.");
                                }
                                num3++;
                            }
                        }
                        (this.m_ReadJobFences + num2).Complete();
                        num2++;
                    }
                }
                this.m_ComponentSafetyHandles[index].WriteFence.Complete();
                index++;
            }
        }

        public unsafe void EndExclusiveTransaction()
        {
            if (this.IsInTransaction)
            {
                this.m_ExclusiveTransactionDependency.Complete();
                if (AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(this.ExclusiveTransactionSafety) != EnforceJobResult.AllJobsAlreadySynced)
                {
                    Unity.Debug.LogError("ExclusiveEntityTransaction job has not been registered");
                }
                this.IsInTransaction = false;
                int index = 0;
                while (true)
                {
                    if (index == TypeManager.GetTypeCount())
                    {
                        break;
                    }
                    this.m_ComponentSafetyHandles[index].SafetyHandle = AtomicSafetyHandle.Create();
                    AtomicSafetyHandle.SetAllowSecondaryVersionWriting(this.m_ComponentSafetyHandles[index].SafetyHandle, false);
                    this.m_ComponentSafetyHandles[index].BufferHandle = AtomicSafetyHandle.Create();
                    index++;
                }
            }
        }

        private unsafe JobHandle GetAllDependencies()
        {
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(TypeManager.GetTypeCount() * 0x12, Allocator.Temp, NativeArrayOptions.ClearMemory);
            int index = 0;
            int num2 = 0;
            while (true)
            {
                if (num2 == TypeManager.GetTypeCount())
                {
                    JobHandle handle = JobHandle.CombineDependencies(jobs);
                    jobs.Dispose();
                    return handle;
                }
                index++;
                jobs.set_Item(index, this.m_ComponentSafetyHandles[num2].WriteFence);
                int numReadFences = this.m_ComponentSafetyHandles[num2].NumReadFences;
                int num4 = 0;
                while (true)
                {
                    if (num4 == numReadFences)
                    {
                        num2++;
                        break;
                    }
                    index++;
                    jobs.set_Item(index, this.m_ReadJobFences[(num2 * 0x11) + num4]);
                    num4++;
                }
            }
        }

        public unsafe AtomicSafetyHandle GetBufferSafetyHandle(int type)
        {
            this.m_HasCleanHandles = false;
            return this.m_ComponentSafetyHandles[type].BufferHandle;
        }

        public unsafe JobHandle GetDependency(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount)
        {
            if (((readerTypesCount * 0x11) + writerTypesCount) > this.m_JobDependencyCombineBufferCount)
            {
                throw new ArgumentException("Too many readers & writers in GetDependency");
            }
            int index = 0;
            int num2 = 0;
            while (true)
            {
                if (num2 == readerTypesCount)
                {
                    int num3 = 0;
                    while (num3 != writerTypesCount)
                    {
                        int num4 = writerTypes[num3];
                        index++;
                        this.m_JobDependencyCombineBuffer[index] = this.m_ComponentSafetyHandles[num4].WriteFence;
                        int numReadFences = this.m_ComponentSafetyHandles[num4].NumReadFences;
                        int num6 = 0;
                        while (true)
                        {
                            if (num6 == numReadFences)
                            {
                                num3++;
                                break;
                            }
                            index++;
                            this.m_JobDependencyCombineBuffer[index] = this.m_ReadJobFences[(num4 * 0x11) + num6];
                            num6++;
                        }
                    }
                    return JobHandleUnsafeUtility.CombineDependencies(this.m_JobDependencyCombineBuffer, index);
                }
                index++;
                this.m_JobDependencyCombineBuffer[index] = this.m_ComponentSafetyHandles[readerTypes[num2]].WriteFence;
                num2++;
            }
        }

        public unsafe AtomicSafetyHandle GetSafetyHandle(int type, bool isReadOnly)
        {
            this.m_HasCleanHandles = false;
            AtomicSafetyHandle safetyHandle = this.m_ComponentSafetyHandles[type].SafetyHandle;
            if (isReadOnly)
            {
                AtomicSafetyHandle.UseSecondaryVersion(ref safetyHandle);
            }
            return safetyHandle;
        }

        public unsafe bool HasReaderOrWriterDependency(int type, JobHandle dependency)
        {
            bool flag2;
            JobHandle writeFence = this.m_ComponentSafetyHandles[type].WriteFence;
            if (JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, writeFence))
            {
                flag2 = true;
            }
            else
            {
                int numReadFences = this.m_ComponentSafetyHandles[type].NumReadFences;
                int num2 = 0;
                while (true)
                {
                    if (num2 >= numReadFences)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        JobHandle dependsOn = this.m_ReadJobFences[(type * 0x11) + num2];
                        if (!JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, dependsOn))
                        {
                            num2++;
                            continue;
                        }
                        flag2 = true;
                    }
                    break;
                }
            }
            return flag2;
        }

        internal unsafe void PreDisposeCheck()
        {
            int index = 0;
            while (true)
            {
                if (index >= 0x2800)
                {
                    int num2 = 0;
                    while (true)
                    {
                        if (num2 >= 0x2a800)
                        {
                            for (int i = 0; i < 0x2800; i++)
                            {
                                EnforceJobResult result = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(this.m_ComponentSafetyHandles[i].SafetyHandle);
                                EnforceJobResult result2 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(this.m_ComponentSafetyHandles[i].BufferHandle);
                                if ((result == EnforceJobResult.DidSyncRunningJobs) || (result2 == EnforceJobResult.DidSyncRunningJobs))
                                {
                                    Unity.Debug.LogError("Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with JobComponentSystem.AddDependency.");
                                }
                            }
                            return;
                        }
                        (this.m_ReadJobFences + num2).Complete();
                        num2++;
                    }
                }
                this.m_ComponentSafetyHandles[index].WriteFence.Complete();
                index++;
            }
        }

        public bool IsInTransaction { get; private set; }

        public JobHandle ExclusiveTransactionDependency
        {
            get => 
                this.m_ExclusiveTransactionDependency;
            set
            {
                if (!this.IsInTransaction)
                {
                    throw new InvalidOperationException("EntityManager.TransactionDependency can only after EntityManager.BeginExclusiveEntityTransaction has been called.");
                }
                if (!JobHandle.CheckFenceIsDependencyOrDidSyncFence(this.m_ExclusiveTransactionDependency, value))
                {
                    throw new InvalidOperationException("EntityManager.TransactionDependency must depend on the Entity Transaction job.");
                }
                this.m_ExclusiveTransactionDependency = value;
            }
        }

        public AtomicSafetyHandle ExclusiveTransactionSafety { get; private set; }

        [StructLayout(LayoutKind.Sequential)]
        private struct ComponentSafetyHandle
        {
            public AtomicSafetyHandle SafetyHandle;
            public AtomicSafetyHandle BufferHandle;
            public JobHandle WriteFence;
            public int NumReadFences;
        }
    }
}

