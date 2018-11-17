namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct BufferFromEntity<T> where T: struct, IBufferElementData
    {
        private readonly AtomicSafetyHandle m_Safety0;
        private readonly AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;
        [NativeDisableUnsafePtrRestriction]
        private unsafe readonly EntityDataManager* m_Entities;
        private readonly int m_TypeIndex;
        private readonly bool m_IsReadOnly;
        private readonly uint m_GlobalSystemVersion;
        private int m_TypeLookupCache;
        internal unsafe BufferFromEntity(int typeIndex, EntityDataManager* entityData, bool isReadOnly, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            this.m_Safety0 = safety;
            this.m_ArrayInvalidationSafety = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            this.m_TypeIndex = typeIndex;
            this.m_Entities = entityData;
            this.m_IsReadOnly = isReadOnly;
            this.m_TypeLookupCache = 0;
            this.m_GlobalSystemVersion = entityData.GlobalSystemVersion;
            if (TypeManager.GetTypeInfo(this.m_TypeIndex).Category != TypeManager.TypeCategory.BufferData)
            {
                throw new ArgumentException($"GetComponentBufferArray<{typeof(T)}> must be IBufferElementData");
            }
        }

        public unsafe bool Exists(Entity entity)
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
            return this.m_Entities.HasComponent(entity, this.m_TypeIndex);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
                this.m_Entities.AssertEntityHasComponent(entity, this.m_TypeIndex);
                return new DynamicBuffer<T>((BufferHeader*) this.m_Entities.GetComponentDataWithTypeRW(entity, this.m_TypeIndex, this.m_GlobalSystemVersion, ref this.m_TypeLookupCache), this.m_Safety0, this.m_ArrayInvalidationSafety, this.m_IsReadOnly);
            }
        }
    }
}

