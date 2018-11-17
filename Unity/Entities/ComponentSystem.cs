namespace Unity.Entities
{
    using System;
    using Unity.Collections;
    using Unity.Jobs;

    public abstract class ComponentSystem : ComponentSystemBase
    {
        private EntityCommandBuffer m_DeferredEntities;

        protected ComponentSystem()
        {
        }

        private void AfterOnUpdate()
        {
            base.AfterUpdateVersioning();
            JobHandle.ScheduleBatchedJobs();
            try
            {
                this.m_DeferredEntities.Playback(base.EntityManager);
            }
            catch (Exception exception)
            {
                this.m_DeferredEntities.Dispose();
                throw new ArgumentException($"{exception.Message}
EntityCommandBuffer was recorded in {base.GetType()} using PostUpdateCommands.
" + exception.StackTrace);
            }
            this.m_DeferredEntities.Dispose();
        }

        private void BeforeOnUpdate()
        {
            base.BeforeUpdateVersioning();
            base.CompleteDependencyInternal();
            base.UpdateInjectedComponentGroups();
            this.m_DeferredEntities = new EntityCommandBuffer(Allocator.TempJob);
        }

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
                this.BeforeOnUpdate();
                ComponentSystemBase base2 = ComponentSystemBase.ms_ExecutingSystem;
                ComponentSystemBase.ms_ExecutingSystem = this;
                try
                {
                    this.OnUpdate();
                }
                finally
                {
                    ComponentSystemBase.ms_ExecutingSystem = base2;
                    this.AfterOnUpdate();
                }
            }
        }

        protected sealed override void OnBeforeCreateManagerInternal(World world)
        {
            base.OnBeforeCreateManagerInternal(world);
        }

        protected sealed override void OnBeforeDestroyManagerInternal()
        {
            base.OnBeforeDestroyManagerInternal();
        }

        protected abstract void OnUpdate();

        public EntityCommandBuffer PostUpdateCommands =>
            this.m_DeferredEntities;
    }
}

