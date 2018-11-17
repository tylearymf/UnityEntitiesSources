namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct ComponentDataFromEntity<T> where T: struct, IComponentData
    {
        private readonly AtomicSafetyHandle m_Safety;
        [NativeDisableUnsafePtrRestriction]
        private unsafe readonly EntityDataManager* m_Entities;
        private readonly int m_TypeIndex;
        private readonly uint m_GlobalSystemVersion;
        private readonly bool m_IsZeroSized;
        private int m_TypeLookupCache;
        internal unsafe ComponentDataFromEntity(int typeIndex, EntityDataManager* entityData, AtomicSafetyHandle safety)
        {
            this.m_Safety = safety;
            this.m_TypeIndex = typeIndex;
            this.m_Entities = entityData;
            this.m_TypeLookupCache = 0;
            this.m_GlobalSystemVersion = entityData.GlobalSystemVersion;
            this.m_IsZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized;
        }

        public unsafe bool Exists(Entity entity)
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
            return this.m_Entities.HasComponent(entity, this.m_TypeIndex);
        }

        public T this[Entity entity]
        {
            get
            {
                T local;
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
                this.m_Entities.AssertEntityHasComponent(entity, this.m_TypeIndex);
                if (this.m_IsZeroSized)
                {
                    throw new ArgumentException($"ComponentDataFromEntity<{typeof(T)}> indexer can not get the component because it is zero sized, you can use Exists instead.");
                }
                UnsafeUtility.CopyPtrToStructure<T>((void*) this.m_Entities.GetComponentDataWithTypeRO(entity, this.m_TypeIndex, ref this.m_TypeLookupCache), out local);
                return local;
            }
            set
            {
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
                this.m_Entities.AssertEntityHasComponent(entity, this.m_TypeIndex);
                if (this.m_IsZeroSized)
                {
                    throw new ArgumentException($"ComponentDataFromEntity<{typeof(T)}> indexer can not set the component because it is zero sized, you can use Exists instead.");
                }
                void* ptr = (void*) ref this.m_Entities.GetComponentDataWithTypeRW(entity, this.m_TypeIndex, this.m_GlobalSystemVersion, ref this.m_TypeLookupCache);
                UnsafeUtility.CopyStructureToPtr<T>(ref value, ptr);
            }
        }
    }
}

