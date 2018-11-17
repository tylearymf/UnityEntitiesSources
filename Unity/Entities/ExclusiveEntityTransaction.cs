namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct ExclusiveEntityTransaction
    {
        private AtomicSafetyHandle m_Safety;
        [NativeDisableUnsafePtrRestriction]
        private GCHandle m_ArchetypeManager;
        [NativeDisableUnsafePtrRestriction]
        private GCHandle m_EntityGroupManager;
        [NativeDisableUnsafePtrRestriction]
        private GCHandle m_SharedComponentDataManager;
        [NativeDisableUnsafePtrRestriction]
        private unsafe EntityDataManager* m_Entities;
        [NativeDisableUnsafePtrRestriction]
        private unsafe readonly ComponentTypeInArchetype* m_CachedComponentTypeInArchetypeArray;
        internal Unity.Entities.SharedComponentDataManager SharedComponentDataManager =>
            ((Unity.Entities.SharedComponentDataManager) this.m_SharedComponentDataManager.Target);
        internal Unity.Entities.ArchetypeManager ArchetypeManager =>
            ((Unity.Entities.ArchetypeManager) this.m_ArchetypeManager.Target);
        internal unsafe ExclusiveEntityTransaction(Unity.Entities.ArchetypeManager archetypes, EntityGroupManager entityGroupManager, Unity.Entities.SharedComponentDataManager sharedComponentDataManager, EntityDataManager* data)
        {
            this.m_Safety = new AtomicSafetyHandle();
            this.m_Entities = data;
            this.m_ArchetypeManager = GCHandle.Alloc(archetypes, GCHandleType.Weak);
            this.m_EntityGroupManager = GCHandle.Alloc(entityGroupManager, GCHandleType.Weak);
            this.m_SharedComponentDataManager = GCHandle.Alloc(sharedComponentDataManager, GCHandleType.Weak);
            this.m_CachedComponentTypeInArchetypeArray = (ComponentTypeInArchetype*) UnsafeUtility.Malloc((long) ((sizeof(ComponentTypeInArchetype) * 0x20) * 0x400), 0x10, Allocator.Persistent);
        }

        internal unsafe void OnDestroyManager()
        {
            UnsafeUtility.Free((void*) this.m_CachedComponentTypeInArchetypeArray, Allocator.Persistent);
            this.m_ArchetypeManager.Free();
            this.m_EntityGroupManager.Free();
            this.m_SharedComponentDataManager.Free();
            this.m_Entities = null;
        }

        internal void SetAtomicSafetyHandle(AtomicSafetyHandle safety)
        {
            this.m_Safety = safety;
        }

        private unsafe int PopulatedCachedTypeInArchetypeArray(ComponentType* requiredComponents, int count)
        {
            this.m_CachedComponentTypeInArchetypeArray[0] = new ComponentTypeInArchetype(ComponentType.Create<Entity>());
            for (int i = 0; i < count; i++)
            {
                SortingUtilities.InsertSorted(this.m_CachedComponentTypeInArchetypeArray, i + 1, requiredComponents[i]);
            }
            return (count + 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
        }

        internal unsafe EntityArchetype CreateArchetype(ComponentType* types, int count)
        {
            EntityArchetype archetype;
            this.CheckAccess();
            EntityGroupManager target = (EntityGroupManager) this.m_EntityGroupManager.Target;
            archetype.Archetype = this.ArchetypeManager.GetOrCreateArchetype(this.m_CachedComponentTypeInArchetypeArray, this.PopulatedCachedTypeInArchetypeArray(types, count), target);
            return archetype;
        }

        public unsafe EntityArchetype CreateArchetype(params ComponentType[] types)
        {
            ComponentType* typePtr;
            ComponentType[] pinned typeArray;
            if (((typeArray = types) == null) || (typeArray.Length == 0))
            {
                typePtr = null;
            }
            else
            {
                typePtr = typeArray;
            }
            return this.CreateArchetype(typePtr, types.Length);
        }

        public unsafe Entity CreateEntity(EntityArchetype archetype)
        {
            Entity entity;
            this.CheckAccess();
            this.CreateEntityInternal(archetype, &entity, 1);
            return entity;
        }

        public unsafe void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            this.CreateEntityInternal(archetype, (Entity*) entities.GetUnsafePtr<Entity>(), entities.Length);
        }

        public Entity CreateEntity(params ComponentType[] types) => 
            this.CreateEntity(this.CreateArchetype(types));

        private unsafe void CreateEntityInternal(EntityArchetype archetype, Entity* entities, int count)
        {
            this.CheckAccess();
            this.m_Entities.CreateEntities(this.ArchetypeManager, archetype.Archetype, entities, count);
        }

        public unsafe Entity Instantiate(Entity srcEntity)
        {
            Entity entity;
            this.InstantiateInternal(srcEntity, &entity, 1);
            return entity;
        }

        public unsafe void Instantiate(Entity srcEntity, NativeArray<Entity> outputEntities)
        {
            this.InstantiateInternal(srcEntity, (Entity*) outputEntities.GetUnsafePtr<Entity>(), outputEntities.Length);
        }

        private unsafe void InstantiateInternal(Entity srcEntity, Entity* outputEntities, int count)
        {
            this.CheckAccess();
            if (!this.m_Entities.Exists(srcEntity))
            {
                throw new ArgumentException("srcEntity is not a valid entity");
            }
            EntityGroupManager target = (EntityGroupManager) this.m_EntityGroupManager.Target;
            this.m_Entities.InstantiateEntities(this.ArchetypeManager, this.SharedComponentDataManager, target, srcEntity, outputEntities, count, this.m_CachedComponentTypeInArchetypeArray);
        }

        public unsafe void DestroyEntity(NativeArray<Entity> entities)
        {
            this.DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr<Entity>(), entities.Length);
        }

        public unsafe void DestroyEntity(NativeSlice<Entity> entities)
        {
            this.DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr<Entity>(), entities.Length);
        }

        public unsafe void DestroyEntity(Entity entity)
        {
            this.DestroyEntityInternal(&entity, 1);
        }

        private unsafe void DestroyEntityInternal(Entity* entities, int count)
        {
            this.CheckAccess();
            this.m_Entities.AssertEntitiesExist(entities, count);
            EntityGroupManager target = (EntityGroupManager) this.m_EntityGroupManager.Target;
            this.m_Entities.TryRemoveEntityId(entities, count, this.ArchetypeManager, this.SharedComponentDataManager, target, this.m_CachedComponentTypeInArchetypeArray);
        }

        public unsafe void AddComponent(Entity entity, ComponentType type)
        {
            this.CheckAccess();
            EntityGroupManager target = (EntityGroupManager) this.m_EntityGroupManager.Target;
            this.m_Entities.AssertEntitiesExist(&entity, 1);
            this.m_Entities.AddComponent(entity, type, this.ArchetypeManager, this.SharedComponentDataManager, target, this.m_CachedComponentTypeInArchetypeArray);
        }

        public unsafe void RemoveComponent(Entity entity, ComponentType type)
        {
            this.CheckAccess();
            EntityGroupManager target = (EntityGroupManager) this.m_EntityGroupManager.Target;
            this.m_Entities.AssertEntityHasComponent(entity, type);
            this.m_Entities.RemoveComponent(entity, type, this.ArchetypeManager, this.SharedComponentDataManager, target, this.m_CachedComponentTypeInArchetypeArray);
        }

        public unsafe bool Exists(Entity entity)
        {
            this.CheckAccess();
            return this.m_Entities.Exists(entity);
        }

        public unsafe T GetComponentData<T>(Entity entity) where T: struct, IComponentData
        {
            T local;
            this.CheckAccess();
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.m_Entities.AssertEntityHasComponent(entity, typeIndex);
            UnsafeUtility.CopyPtrToStructure<T>((void*) this.m_Entities.GetComponentDataWithTypeRO(entity, typeIndex), out local);
            return local;
        }

        public unsafe void SetComponentData<T>(Entity entity, T componentData) where T: struct, IComponentData
        {
            this.CheckAccess();
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.m_Entities.AssertEntityHasComponent(entity, typeIndex);
            byte* numPtr = ref this.m_Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.m_Entities.GlobalSystemVersion);
            UnsafeUtility.CopyStructureToPtr<T>(ref componentData, (void*) numPtr);
        }

        public unsafe T GetSharedComponentData<T>(Entity entity) where T: struct, ISharedComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.m_Entities.AssertEntityHasComponent(entity, typeIndex);
            int sharedComponentDataIndex = this.m_Entities.GetSharedComponentDataIndex(entity, typeIndex);
            return this.SharedComponentDataManager.GetSharedComponentData<T>(sharedComponentDataIndex);
        }

        public unsafe void SetSharedComponentData<T>(Entity entity, T componentData) where T: struct, ISharedComponentData
        {
            this.CheckAccess();
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.m_Entities.AssertEntityHasComponent(entity, typeIndex);
            Unity.Entities.ArchetypeManager archetypeManager = this.ArchetypeManager;
            Unity.Entities.SharedComponentDataManager sharedComponentDataManager = this.SharedComponentDataManager;
            int newSharedComponentDataIndex = sharedComponentDataManager.InsertSharedComponent<T>(componentData);
            this.m_Entities.SetSharedComponentDataIndex(archetypeManager, sharedComponentDataManager, entity, typeIndex, newSharedComponentDataIndex);
            sharedComponentDataManager.RemoveReference(newSharedComponentDataIndex, 1);
        }

        internal unsafe void AllocateConsecutiveEntitiesForLoading(int count)
        {
            this.m_Entities.AllocateConsecutiveEntitiesForLoading(count);
        }

        internal unsafe void AddExistingChunk(Chunk* chunk)
        {
            this.ArchetypeManager.AddExistingChunk(chunk);
            this.m_Entities.AddExistingChunk(chunk);
        }
    }
}

