namespace Unity.Entities
{
    using System;
    using UnityEngine.Profiling;

    public abstract class ScriptBehaviourManager
    {
        private CustomSampler m_Sampler;

        protected ScriptBehaviourManager()
        {
        }

        internal void CreateInstance(World world)
        {
            this.OnBeforeCreateManagerInternal(world);
            try
            {
                this.OnCreateManager();
                Type type = base.GetType();
                this.m_Sampler = CustomSampler.Create($"{world.Name} {type.FullName}");
            }
            catch
            {
                this.OnBeforeDestroyManagerInternal();
                this.OnAfterDestroyManagerInternal();
                throw;
            }
        }

        internal void DestroyInstance()
        {
            this.OnBeforeDestroyManagerInternal();
            this.OnDestroyManager();
            this.OnAfterDestroyManagerInternal();
        }

        internal abstract void InternalUpdate();
        protected abstract void OnAfterDestroyManagerInternal();
        protected abstract void OnBeforeCreateManagerInternal(World world);
        protected abstract void OnBeforeDestroyManagerInternal();
        protected virtual void OnCreateManager()
        {
        }

        protected virtual void OnDestroyManager()
        {
        }

        public void Update()
        {
            if (this.m_Sampler == null)
            {
                object sampler = this.m_Sampler;
            }
            else
            {
                this.m_Sampler.Begin();
            }
            this.InternalUpdate();
            if (this.m_Sampler == null)
            {
                object sampler = this.m_Sampler;
            }
            else
            {
                this.m_Sampler.End();
            }
        }
    }
}

