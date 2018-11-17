namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;

    public abstract class BarrierSystem : ComponentSystem
    {
        private List<EntityCommandBuffer> m_PendingBuffers;
        private JobHandle m_ProducerHandle;

        protected BarrierSystem()
        {
        }

        internal void AddJobHandleForProducer(JobHandle foo)
        {
            this.m_ProducerHandle = JobHandle.CombineDependencies(this.m_ProducerHandle, foo);
        }

        public unsafe EntityCommandBuffer CreateCommandBuffer()
        {
            EntityCommandBuffer* bufferPtr1;
            EntityCommandBuffer item = new EntityCommandBuffer(Allocator.TempJob);
            bufferPtr1->SystemID = (ComponentSystemBase.ms_ExecutingSystem != null) ? ComponentSystemBase.ms_ExecutingSystem.m_SystemID : 0;
            bufferPtr1 = (EntityCommandBuffer*) ref item;
            this.m_PendingBuffers.Add(item);
            return item;
        }

        private void FlushBuffers(bool playBack)
        {
            this.m_ProducerHandle.Complete();
            this.m_ProducerHandle = new JobHandle();
            int count = this.m_PendingBuffers.Count;
            Exception exception = null;
            int num2 = 0;
            while (true)
            {
                if (num2 >= count)
                {
                    this.m_PendingBuffers.Clear();
                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }
                EntityCommandBuffer buffer = this.m_PendingBuffers[num2];
                if (playBack)
                {
                    try
                    {
                        buffer.Playback(base.EntityManager);
                    }
                    catch (Exception exception2)
                    {
                        ComponentSystemBase systemFromSystemID = base.GetSystemFromSystemID(base.World, buffer.SystemID);
                        string str = (systemFromSystemID != null) ? systemFromSystemID.GetType().ToString() : "Unknown";
                        exception = new ArgumentException($"{exception2.Message}
EntityCommandBuffer was recorded in {str} and played back in {base.GetType()}.
" + exception2.StackTrace);
                    }
                }
                buffer.Dispose();
                num2++;
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            this.m_PendingBuffers = new List<EntityCommandBuffer>();
        }

        protected override void OnDestroyManager()
        {
            this.FlushBuffers(false);
            base.OnDestroyManager();
        }

        protected sealed override void OnUpdate()
        {
            this.FlushBuffers(true);
        }
    }
}

