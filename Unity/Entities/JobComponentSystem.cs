namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    public abstract class JobComponentSystem : ComponentSystemBase
    {
        private JobHandle m_PreviousFrameDependency;
        private BarrierSystem[] m_BarrierList;

        protected JobComponentSystem()
        {
        }

        private unsafe void AddDependencyInternal(JobHandle dependency)
        {
            this.m_PreviousFrameDependency = this.m_SafetyManager.AddDependency(this.m_JobDependencyForReadingManagersPtr, this.m_JobDependencyForReadingManagersLength, this.m_JobDependencyForWritingManagersPtr, this.m_JobDependencyForWritingManagersLength, dependency);
        }

        private unsafe void AfterOnUpdate(JobHandle outputJob, bool throwException)
        {
            base.AfterUpdateVersioning();
            JobHandle.ScheduleBatchedJobs();
            this.AddDependencyInternal(outputJob);
            int index = 0;
            while (true)
            {
                if (index >= this.m_BarrierList.Length)
                {
                    if (JobsUtility.JobDebuggerEnabled)
                    {
                        string message = null;
                        int num2 = 0;
                        while (true)
                        {
                            if ((num2 >= this.m_JobDependencyForReadingManagersLength) || (message != null))
                            {
                                int num4 = 0;
                                while (true)
                                {
                                    if ((num4 >= this.m_JobDependencyForWritingManagersLength) || (message != null))
                                    {
                                        if (message != null)
                                        {
                                            this.EmergencySyncAllJobs();
                                            if (throwException)
                                            {
                                                throw new InvalidOperationException(message);
                                            }
                                        }
                                        break;
                                    }
                                    int num5 = this.m_JobDependencyForWritingManagersPtr[num4];
                                    message = this.CheckJobDependencies(num5);
                                    num4++;
                                }
                                break;
                            }
                            int type = this.m_JobDependencyForReadingManagersPtr[num2];
                            message = this.CheckJobDependencies(type);
                            num2++;
                        }
                    }
                    return;
                }
                this.m_BarrierList[index].AddJobHandleForProducer(outputJob);
                index++;
            }
        }

        private JobHandle BeforeOnUpdate()
        {
            base.BeforeUpdateVersioning();
            base.UpdateInjectedComponentGroups();
            this.m_PreviousFrameDependency.Complete();
            return this.GetDependency();
        }

        private unsafe string CheckJobDependencies(int type)
        {
            AtomicSafetyHandle safetyHandle = this.m_SafetyManager.GetSafetyHandle(type, true);
            int maxCount = AtomicSafetyHandle.GetReaderArray(safetyHandle, 0, IntPtr.Zero);
            JobHandle* handlePtr = (JobHandle*) stackalloc byte[(((IntPtr) maxCount) * sizeof(JobHandle))];
            AtomicSafetyHandle.GetReaderArray(safetyHandle, maxCount, (IntPtr) handlePtr);
            int index = 0;
            while (true)
            {
                string str;
                if (index < maxCount)
                {
                    if (this.m_SafetyManager.HasReaderOrWriterDependency(type, handlePtr[index]))
                    {
                        index++;
                        continue;
                    }
                    str = $"The system {base.GetType()} reads {TypeManager.GetType(type)} via {AtomicSafetyHandle.GetReaderName(safetyHandle, index)} but that type was not returned as a job dependency. To ensure correct behavior of other systems, the job or a dependency of it must be returned from the OnUpdate method.";
                }
                else if (!this.m_SafetyManager.HasReaderOrWriterDependency(type, AtomicSafetyHandle.GetWriter(safetyHandle)))
                {
                    str = $"The system {base.GetType()} writes {TypeManager.GetType(type)} via {AtomicSafetyHandle.GetWriterName(safetyHandle)} but that was not returned as a job dependency. To ensure correct behavior of other systems, the job or a dependency of it must be returned from the OnUpdate method.";
                }
                else
                {
                    str = null;
                }
                return str;
            }
        }

        private unsafe void EmergencySyncAllJobs()
        {
            int index = 0;
            while (true)
            {
                if (index == this.m_JobDependencyForReadingManagersLength)
                {
                    for (int i = 0; i != this.m_JobDependencyForWritingManagersLength; i++)
                    {
                        int num4 = this.m_JobDependencyForWritingManagersPtr[i];
                        AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(this.m_SafetyManager.GetSafetyHandle(num4, true));
                    }
                    return;
                }
                int type = this.m_JobDependencyForReadingManagersPtr[index];
                AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(this.m_SafetyManager.GetSafetyHandle(type, true));
                index++;
            }
        }

        public BufferFromEntity<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T: struct, IBufferElementData
        {
            this.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.Create<T>());
            return base.EntityManager.GetBufferFromEntity<T>(isReadOnly);
        }

        private unsafe JobHandle GetDependency() => 
            this.m_SafetyManager.GetDependency(this.m_JobDependencyForReadingManagersPtr, this.m_JobDependencyForReadingManagersLength, this.m_JobDependencyForWritingManagersPtr, this.m_JobDependencyForWritingManagersLength);

        internal sealed override void InternalUpdate()
        {
            if (!(base.Enabled && base.ShouldRunSystem()))
            {
                if (this.m_PreviouslyEnabled)
                {
                    this.m_PreviouslyEnabled = false;
                    this.OnStopRunning();
                }
            }
            else
            {
                if (!this.m_PreviouslyEnabled)
                {
                    this.m_PreviouslyEnabled = true;
                    this.OnStartRunning();
                }
                JobHandle inputDeps = this.BeforeOnUpdate();
                JobHandle outputJob = new JobHandle();
                ComponentSystemBase base2 = ComponentSystemBase.ms_ExecutingSystem;
                ComponentSystemBase.ms_ExecutingSystem = this;
                try
                {
                    outputJob = this.OnUpdate(inputDeps);
                }
                catch
                {
                    ComponentSystemBase.ms_ExecutingSystem = base2;
                    this.AfterOnUpdate(outputJob, false);
                    throw;
                }
                this.AfterOnUpdate(outputJob, true);
            }
        }

        protected sealed override void OnBeforeCreateManagerInternal(World world)
        {
            base.OnBeforeCreateManagerInternal(world);
            this.m_BarrierList = ComponentSystemInjection.GetAllInjectedManagers<BarrierSystem>(this, world);
        }

        protected sealed override void OnBeforeDestroyManagerInternal()
        {
            base.OnBeforeDestroyManagerInternal();
            this.m_PreviousFrameDependency.Complete();
        }

        protected virtual JobHandle OnUpdate(JobHandle inputDeps) => 
            inputDeps;
    }
}

