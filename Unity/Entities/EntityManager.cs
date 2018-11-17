namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using Unity;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine.Scripting;

    [Preserve]
    public sealed class EntityManager : ScriptBehaviourManager
    {
        private unsafe EntityDataManager* m_Entities;
        private Unity.Entities.ArchetypeManager m_ArchetypeManager;
        private EntityGroupManager m_GroupManager;
        internal SharedComponentDataManager m_SharedComponentManager;
        private ExclusiveEntityTransaction m_ExclusiveEntityTransaction;
        private const int m_CachedComponentTypeInArchetypeArrayLength = 0x400;
        private unsafe ComponentTypeInArchetype* m_CachedComponentTypeInArchetypeArray;
        internal object m_CachedComponentList;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Unity.Entities.ComponentJobSafetyManager <ComponentJobSafetyManager>k__BackingField;
        private EntityManagerDebug m_Debug;

        public void AddBuffer<T>(Entity entity) where T: struct, IBufferElementData
        {
            this.AddComponent(entity, ComponentType.Create<T>());
        }

        public unsafe void AddComponent(Entity entity, ComponentType type)
        {
            this.BeforeStructuralChange();
            this.Entities.AssertEntitiesExist(&entity, 1);
            this.Entities.AddComponent(entity, type, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
        }

        public void AddComponentData<T>(Entity entity, T componentData) where T: struct, IComponentData
        {
            ComponentType type = ComponentType.Create<T>();
            this.AddComponent(entity, type);
            if (!type.IsZeroSized)
            {
                this.SetComponentData<T>(entity, componentData);
            }
        }

        public unsafe void AddComponents(Entity entity, ComponentTypes types)
        {
            this.BeforeStructuralChange();
            this.Entities.AssertEntitiesExist(&entity, 1);
            this.Entities.AddComponents(entity, types, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
        }

        public unsafe void AddMatchingArchetypes(EntityArchetypeQuery query, NativeList<EntityArchetype> foundArchetypes)
        {
            ComponentType* typePtr;
            ComponentType[] pinned typeArray;
            ComponentType* typePtr2;
            ComponentType[] pinned typeArray2;
            ComponentType* typePtr3;
            ComponentType[] pinned typeArray3;
            int length = query.Any.Length;
            int noneCount = query.None.Length;
            int allCount = query.All.Length;
            if (((typeArray = query.Any) == null) || (typeArray.Length == 0))
            {
                typePtr = null;
            }
            else
            {
                typePtr = typeArray;
            }
            if (((typeArray2 = query.None) == null) || (typeArray2.Length == 0))
            {
                typePtr2 = null;
            }
            else
            {
                typePtr2 = typeArray2;
            }
            if (((typeArray3 = query.All) == null) || (typeArray3.Length == 0))
            {
                typePtr3 = null;
            }
            else
            {
                typePtr3 = typeArray3;
            }
            Archetype* lastArchetype = this.ArchetypeManager.m_LastArchetype;
            while (true)
            {
                if (lastArchetype == null)
                {
                    typeArray3 = null;
                    typeArray2 = null;
                    typeArray = null;
                    return;
                }
                bool flag2 = lastArchetype->EntityCount == 0;
                if ((!flag2 && (this.TestMatchingArchetypeAny(lastArchetype, typePtr, length) && this.TestMatchingArchetypeNone(lastArchetype, typePtr2, noneCount))) && this.TestMatchingArchetypeAll(lastArchetype, typePtr3, allCount))
                {
                    EntityArchetype archetype = new EntityArchetype {
                        Archetype = lastArchetype
                    };
                    if (!foundArchetypes.Contains<EntityArchetype, EntityArchetype>(archetype))
                    {
                        foundArchetypes.Add(archetype);
                    }
                }
                lastArchetype = lastArchetype->PrevArchetype;
            }
        }

        public void AddSharedComponentData<T>(Entity entity, T componentData) where T: struct, ISharedComponentData
        {
            this.AddComponent(entity, ComponentType.Create<T>());
            this.SetSharedComponentData<T>(entity, componentData);
        }

        internal void AddSharedComponentDataBoxed(Entity entity, int typeIndex, int hashCode, object componentData)
        {
            this.AddComponent(entity, ComponentType.FromTypeIndex(typeIndex));
            this.SetSharedComponentDataBoxed(entity, typeIndex, hashCode, componentData);
        }

        private void BeforeStructuralChange()
        {
            if (this.ComponentJobSafetyManager.IsInTransaction)
            {
                throw new InvalidOperationException("Access to EntityManager is not allowed after EntityManager.BeginExclusiveEntityTransaction(); has been called.");
            }
            this.ComponentJobSafetyManager.CompleteAllJobsAndInvalidateArrays();
        }

        public ExclusiveEntityTransaction BeginExclusiveEntityTransaction()
        {
            this.ComponentJobSafetyManager.BeginExclusiveTransaction();
            this.m_ExclusiveEntityTransaction.SetAtomicSafetyHandle(this.ComponentJobSafetyManager.ExclusiveTransactionSafety);
            return this.m_ExclusiveEntityTransaction;
        }

        public unsafe void CheckInternalConsistency()
        {
            Unity.Assertions.Assert.AreEqual(this.Entities.CheckInternalConsistency(), this.ArchetypeManager.CheckInternalConsistency());
        }

        public void CompleteAllJobs()
        {
            this.ComponentJobSafetyManager.CompleteAllJobsAndInvalidateArrays();
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

        internal unsafe EntityArchetype CreateArchetype(ComponentType* types, int count)
        {
            EntityArchetype archetype;
            EntityArchetype archetype2;
            int num = this.PopulatedCachedTypeInArchetypeArray(types, count);
            archetype.Archetype = this.ArchetypeManager.GetExistingArchetype(this.m_CachedComponentTypeInArchetypeArray, num);
            if (archetype.Archetype != null)
            {
                archetype2 = archetype;
            }
            else
            {
                this.BeforeStructuralChange();
                archetype.Archetype = this.ArchetypeManager.GetOrCreateArchetype(this.m_CachedComponentTypeInArchetypeArray, num, this.m_GroupManager);
                archetype2 = archetype;
            }
            return archetype2;
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(NativeList<EntityArchetype> archetypes, Allocator allocator)
        {
            AtomicSafetyHandle safetyHandle = AtomicSafetyHandle.Create();
            return ArchetypeChunkArray.Create(archetypes, allocator, safetyHandle);
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(EntityArchetypeQuery query, Allocator allocator)
        {
            NativeList<EntityArchetype> foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);
            this.AddMatchingArchetypes(query, foundArchetypes);
            NativeArray<ArchetypeChunk> array = this.CreateArchetypeChunkArray(foundArchetypes, allocator);
            foundArchetypes.Dispose();
            return array;
        }

        public unsafe ComponentGroup CreateComponentGroup(params ComponentType[] requiredComponents) => 
            this.m_GroupManager.CreateEntityGroup(this.ArchetypeManager, this.Entities, requiredComponents);

        internal unsafe ComponentGroup CreateComponentGroup(params EntityArchetypeQuery[] queries) => 
            this.m_GroupManager.CreateEntityGroup(this.ArchetypeManager, this.Entities, queries);

        public unsafe Entity CreateEntity(EntityArchetype archetype)
        {
            Entity entity;
            this.CreateEntityInternal(archetype, &entity, 1);
            return entity;
        }

        public Entity CreateEntity(params ComponentType[] types) => 
            this.CreateEntity(this.CreateArchetype(types));

        public unsafe void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            this.CreateEntityInternal(archetype, (Entity*) entities.GetUnsafePtr<Entity>(), entities.Length);
        }

        internal unsafe void CreateEntityInternal(EntityArchetype archetype, Entity* entities, int count)
        {
            this.BeforeStructuralChange();
            this.Entities.CreateEntities(this.ArchetypeManager, archetype.Archetype, entities, count);
        }

        public unsafe NativeArray<EntityRemapUtility.EntityRemapInfo> CreateEntityRemapArray(Allocator allocator) => 
            new NativeArray<EntityRemapUtility.EntityRemapInfo>(this.m_Entities.Capacity, allocator, NativeArrayOptions.ClearMemory);

        public unsafe void DestroyEntity(NativeArray<Entity> entities)
        {
            this.DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr<Entity>(), entities.Length);
        }

        public unsafe void DestroyEntity(NativeSlice<Entity> entities)
        {
            this.DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr<Entity>(), entities.Length);
        }

        public unsafe void DestroyEntity(ComponentGroup componentGroupFilter)
        {
            this.BeforeStructuralChange();
            EntityArray entityArray = componentGroupFilter.GetEntityArray();
            if (entityArray.Length != 0)
            {
                NativeArray<Entity> dst = new NativeArray<Entity>(entityArray.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                entityArray.CopyTo(dst, 0);
                if (dst.Length != 0)
                {
                    this.Entities.TryRemoveEntityId((Entity*) dst.GetUnsafeReadOnlyPtr<Entity>(), dst.Length, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
                }
                dst.Dispose();
            }
        }

        public unsafe void DestroyEntity(Entity entity)
        {
            this.DestroyEntityInternal(&entity, 1);
        }

        private unsafe void DestroyEntityInternal(Entity* entities, int count)
        {
            this.BeforeStructuralChange();
            this.Entities.AssertEntitiesExist(entities, count);
            this.Entities.TryRemoveEntityId(entities, count, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
        }

        public void EndExclusiveEntityTransaction()
        {
            this.ComponentJobSafetyManager.EndExclusiveTransaction();
        }

        public unsafe bool Exists(Entity entity) => 
            this.Entities.Exists(entity);

        public unsafe void GetAllArchetypes(NativeList<EntityArchetype> allArchetypes)
        {
            for (Archetype* archetypePtr = this.ArchetypeManager.m_LastArchetype; archetypePtr != null; archetypePtr = archetypePtr->PrevArchetype)
            {
                EntityArchetype element = new EntityArchetype {
                    Archetype = archetypePtr
                };
                allArchetypes.Add(element);
            }
        }

        public NativeArray<Entity> GetAllEntities(Allocator allocator = 2)
        {
            this.BeforeStructuralChange();
            EntityArchetypeQuery query1 = new EntityArchetypeQuery();
            query1.Any = Array.Empty<ComponentType>();
            query1.None = Array.Empty<ComponentType>();
            query1.All = Array.Empty<ComponentType>();
            EntityArchetypeQuery query = query1;
            EntityArchetypeQuery query4 = new EntityArchetypeQuery {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>()
            };
            query4.All = new ComponentType[] { typeof(Disabled) };
            EntityArchetypeQuery query2 = query4;
            query4 = new EntityArchetypeQuery {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>()
            };
            query4.All = new ComponentType[] { typeof(Prefab) };
            EntityArchetypeQuery query3 = query4;
            NativeList<EntityArchetype> foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);
            this.AddMatchingArchetypes(query, foundArchetypes);
            this.AddMatchingArchetypes(query2, foundArchetypes);
            this.AddMatchingArchetypes(query3, foundArchetypes);
            NativeArray<ArchetypeChunk> chunks = this.CreateArchetypeChunkArray(foundArchetypes, Allocator.TempJob);
            NativeArray<Entity> thisArray = new NativeArray<Entity>(ArchetypeChunkArray.CalculateEntityCount(chunks), allocator, NativeArrayOptions.ClearMemory);
            ArchetypeChunkEntityType archetypeChunkEntityType = this.GetArchetypeChunkEntityType();
            int start = 0;
            int num3 = 0;
            while (true)
            {
                if (num3 >= chunks.Length)
                {
                    chunks.Dispose();
                    foundArchetypes.Dispose();
                    return thisArray;
                }
                NativeArray<Entity> nativeArray = chunks[num3].GetNativeArray(archetypeChunkEntityType);
                thisArray.Slice<Entity>(start, nativeArray.Length).CopyFrom(nativeArray);
                start += nativeArray.Length;
                num3++;
            }
        }

        public void GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues) where T: struct, ISharedComponentData
        {
            this.m_SharedComponentManager.GetAllUniqueSharedComponents<T>(sharedComponentValues);
        }

        public void GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues, List<int> sharedComponentIndices) where T: struct, ISharedComponentData
        {
            this.m_SharedComponentManager.GetAllUniqueSharedComponents<T>(sharedComponentValues, sharedComponentIndices);
        }

        public ArchetypeChunkBufferType<T> GetArchetypeChunkBufferType<T>(bool isReadOnly) where T: struct, IBufferElementData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            return new ArchetypeChunkBufferType<T>(this.ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly), this.ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex), isReadOnly, this.GlobalSystemVersion);
        }

        public ArchetypeChunkComponentType<T> GetArchetypeChunkComponentType<T>(bool isReadOnly) where T: struct, IComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            return new ArchetypeChunkComponentType<T>(this.ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly), isReadOnly, this.GlobalSystemVersion);
        }

        public ArchetypeChunkEntityType GetArchetypeChunkEntityType() => 
            new ArchetypeChunkEntityType(this.ComponentJobSafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));

        public ArchetypeChunkSharedComponentType<T> GetArchetypeChunkSharedComponentType<T>() where T: struct, ISharedComponentData => 
            new ArchetypeChunkSharedComponentType<T>(this.ComponentJobSafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<T>(), true));

        public List<Type> GetAssignableComponentTypes(Type interfaceType)
        {
            int typeCount = TypeManager.GetTypeCount();
            List<Type> list = new List<Type>();
            for (int i = 0; i < typeCount; i++)
            {
                Type c = TypeManager.GetType(i);
                if (interfaceType.IsAssignableFrom(c))
                {
                    list.Add(c);
                }
            }
            return list;
        }

        public unsafe DynamicBuffer<T> GetBuffer<T>(Entity entity) where T: struct, IBufferElementData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            if (TypeManager.GetTypeInfo<T>().Category != TypeManager.TypeCategory.BufferData)
            {
                throw new ArgumentException($"GetBuffer<{typeof(T)}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo<T>().Category}");
            }
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            return new DynamicBuffer<T>((BufferHeader*) this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion), this.ComponentJobSafetyManager.GetSafetyHandle(typeIndex, false), this.ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex), false);
        }

        public BufferFromEntity<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T: struct, IBufferElementData => 
            this.GetBufferFromEntity<T>(TypeManager.GetTypeIndex<T>(), isReadOnly);

        public unsafe BufferFromEntity<T> GetBufferFromEntity<T>(int typeIndex, bool isReadOnly = false) where T: struct, IBufferElementData => 
            new BufferFromEntity<T>(typeIndex, this.Entities, isReadOnly, this.ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly), this.ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex));

        internal unsafe int GetBufferLength(Entity entity, int typeIndex)
        {
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            return this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion).Length;
        }

        internal unsafe void* GetBufferRawRW(Entity entity, int typeIndex)
        {
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            return (void*) ref BufferHeader.GetElementPointer((BufferHeader*) this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion));
        }

        internal unsafe uint GetChunkVersionHash(Entity entity)
        {
            Chunk* chunkPtr = ref this.Entities.GetComponentChunk(entity);
            int typesCount = chunkPtr->Archetype.TypesCount;
            return math.hash((void*) chunkPtr->ChangeVersion, typesCount * UnsafeUtility.SizeOf(typeof(uint)), 0);
        }

        public unsafe int GetComponentCount(Entity entity)
        {
            this.Entities.AssertEntitiesExist(&entity, 1);
            return (this.Entities.GetArchetype(entity).TypesCount - 1);
        }

        public unsafe T GetComponentData<T>(Entity entity) where T: struct, IComponentData
        {
            T local;
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            if (ComponentType.FromTypeIndex(typeIndex).IsZeroSized)
            {
                throw new ArgumentException($"GetComponentData<{typeof(T)}> can not be called with a zero sized component.");
            }
            this.ComponentJobSafetyManager.CompleteWriteDependency(typeIndex);
            UnsafeUtility.CopyPtrToStructure<T>((void*) this.Entities.GetComponentDataWithTypeRO(entity, typeIndex), out local);
            return local;
        }

        public ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(bool isReadOnly = false) where T: struct, IComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            return this.GetComponentDataFromEntity<T>(typeIndex, isReadOnly);
        }

        internal unsafe ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(int typeIndex, bool isReadOnly) where T: struct, IComponentData => 
            new ComponentDataFromEntity<T>(typeIndex, this.Entities, this.ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly));

        internal unsafe void* GetComponentDataRawRW(Entity entity, int typeIndex)
        {
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            if (TypeManager.GetTypeInfo(typeIndex).IsZeroSized)
            {
                throw new ArgumentException($"GetComponentDataRaw<{TypeManager.GetType(typeIndex)}> can not be called with a zero sized component.");
            }
            return (void*) ref this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion);
        }

        public unsafe int GetComponentOrderVersion<T>() => 
            this.Entities.GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<T>());

        internal unsafe int GetComponentTypeIndex(Entity entity, int index)
        {
            int typeIndex;
            this.Entities.AssertEntitiesExist(&entity, 1);
            Archetype* archetypePtr = ref this.Entities.GetArchetype(entity);
            if (((ulong) index) >= archetypePtr->TypesCount)
            {
                typeIndex = -1;
            }
            else
            {
                typeIndex = archetypePtr->Types[index + 1].TypeIndex;
            }
            return typeIndex;
        }

        public unsafe NativeArray<ComponentType> GetComponentTypes(Entity entity, Allocator allocator = 2)
        {
            this.Entities.AssertEntitiesExist(&entity, 1);
            Archetype* archetypePtr = ref this.Entities.GetArchetype(entity);
            NativeArray<ComponentType> array = new NativeArray<ComponentType>(archetypePtr->TypesCount - 1, allocator, NativeArrayOptions.ClearMemory);
            for (int i = 1; i < archetypePtr->TypesCount; i++)
            {
                array.set_Item(i - 1, (archetypePtr->Types + i).ToComponentType());
            }
            return array;
        }

        public int GetSharedComponentCount() => 
            this.m_SharedComponentManager.GetSharedComponentCount();

        public T GetSharedComponentData<T>(int sharedComponentIndex) where T: struct, ISharedComponentData => 
            this.m_SharedComponentManager.GetSharedComponentData<T>(sharedComponentIndex);

        public unsafe T GetSharedComponentData<T>(Entity entity) where T: struct, ISharedComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            int sharedComponentDataIndex = this.Entities.GetSharedComponentDataIndex(entity, typeIndex);
            return this.m_SharedComponentManager.GetSharedComponentData<T>(sharedComponentDataIndex);
        }

        internal unsafe object GetSharedComponentData(Entity entity, int typeIndex)
        {
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            int sharedComponentDataIndex = this.Entities.GetSharedComponentDataIndex(entity, typeIndex);
            return this.m_SharedComponentManager.GetSharedComponentDataBoxed(sharedComponentDataIndex, typeIndex);
        }

        public int GetSharedComponentOrderVersion<T>(T sharedComponent) where T: struct, ISharedComponentData => 
            this.m_SharedComponentManager.GetSharedComponentVersion<T>(sharedComponent);

        public unsafe bool HasComponent<T>(Entity entity) => 
            this.Entities.HasComponent(entity, ComponentType.Create<T>());

        public unsafe bool HasComponent(Entity entity, ComponentType type) => 
            this.Entities.HasComponent(entity, type);

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

        internal unsafe void InstantiateInternal(Entity srcEntity, Entity* outputEntities, int count)
        {
            this.BeforeStructuralChange();
            if (!this.Entities.Exists(srcEntity))
            {
                throw new ArgumentException("srcEntity is not a valid entity");
            }
            this.Entities.InstantiateEntities(this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, srcEntity, outputEntities, count, this.m_CachedComponentTypeInArchetypeArray);
        }

        internal override void InternalUpdate()
        {
        }

        public void MoveEntitiesFrom(EntityManager srcEntities)
        {
            using (NativeArray<EntityRemapUtility.EntityRemapInfo> array = srcEntities.CreateEntityRemapArray(Allocator.TempJob))
            {
                this.MoveEntitiesFrom(srcEntities, array);
            }
        }

        public unsafe void MoveEntitiesFrom(EntityManager srcEntities, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            if (srcEntities == this)
            {
                throw new ArgumentException("srcEntities must not be the same as this EntityManager.");
            }
            if (!srcEntities.m_SharedComponentManager.AllSharedComponentReferencesAreFromChunks(srcEntities.ArchetypeManager))
            {
                throw new ArgumentException("EntityManager.MoveEntitiesFrom failed - All ISharedComponentData references must be from EntityManager. (For example ComponentGroup.SetFilter with a shared component type is not allowed during EntityManager.MoveEntitiesFrom)");
            }
            this.BeforeStructuralChange();
            srcEntities.BeforeStructuralChange();
            Unity.Entities.ArchetypeManager.MoveChunks(srcEntities, this.ArchetypeManager, this.m_GroupManager, this.Entities, this.m_SharedComponentManager, this.m_CachedComponentTypeInArchetypeArray, entityRemapping);
        }

        internal unsafe void MoveEntitiesFrom(EntityManager srcEntities, NativeArray<ArchetypeChunk> chunks, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            if (srcEntities == this)
            {
                throw new ArgumentException("srcEntities must not be the same as this EntityManager.");
            }
            this.BeforeStructuralChange();
            srcEntities.BeforeStructuralChange();
            Unity.Entities.ArchetypeManager.MoveChunks(srcEntities, chunks, this.ArchetypeManager, this.m_GroupManager, this.Entities, this.m_SharedComponentManager, this.m_CachedComponentTypeInArchetypeArray, entityRemapping);
        }

        public void MoveEntitiesFrom(EntityManager srcEntities, ComponentGroup filter, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            if (filter.ArchetypeManager != srcEntities.ArchetypeManager)
            {
                throw new ArgumentException("EntityManager.MoveEntitiesFrom failed - srcEntities and filter must belong to the same World)");
            }
            NativeList<ArchetypeChunk> allMatchingChunks = filter.GetAllMatchingChunks(Allocator.TempJob);
            this.MoveEntitiesFrom(srcEntities, (NativeArray<ArchetypeChunk>) allMatchingChunks, entityRemapping);
            allMatchingChunks.Dispose();
        }

        protected override void OnAfterDestroyManagerInternal()
        {
        }

        protected override void OnBeforeCreateManagerInternal(World world)
        {
        }

        protected override void OnBeforeDestroyManagerInternal()
        {
        }

        protected override unsafe void OnCreateManager()
        {
            TypeManager.Initialize();
            this.Entities = (EntityDataManager*) UnsafeUtility.Malloc((long) sizeof(EntityDataManager), 0x40, Allocator.Persistent);
            this.Entities.OnCreate();
            this.m_SharedComponentManager = new SharedComponentDataManager();
            this.ArchetypeManager = new Unity.Entities.ArchetypeManager(this.m_SharedComponentManager);
            this.ComponentJobSafetyManager = new Unity.Entities.ComponentJobSafetyManager();
            this.m_GroupManager = new EntityGroupManager(this.ComponentJobSafetyManager);
            this.m_ExclusiveEntityTransaction = new ExclusiveEntityTransaction(this.ArchetypeManager, this.m_GroupManager, this.m_SharedComponentManager, this.Entities);
            this.m_CachedComponentTypeInArchetypeArray = (ComponentTypeInArchetype*) UnsafeUtility.Malloc((long) (sizeof(ComponentTypeInArchetype) * 0x400), 0x10, Allocator.Persistent);
        }

        protected override unsafe void OnDestroyManager()
        {
            this.EndExclusiveEntityTransaction();
            this.ComponentJobSafetyManager.PreDisposeCheck();
            using (NativeArray<Entity> array = this.GetAllEntities(Allocator.Temp))
            {
                this.DestroyEntity(array);
            }
            this.ComponentJobSafetyManager.Dispose();
            this.ComponentJobSafetyManager = null;
            this.Entities.OnDestroy();
            UnsafeUtility.Free((void*) this.Entities, Allocator.Persistent);
            this.Entities = null;
            this.ArchetypeManager.Dispose();
            this.ArchetypeManager = null;
            this.m_GroupManager.Dispose();
            this.m_GroupManager = null;
            this.m_ExclusiveEntityTransaction.OnDestroyManager();
            this.m_SharedComponentManager.Dispose();
            UnsafeUtility.Free((void*) this.m_CachedComponentTypeInArchetypeArray, Allocator.Persistent);
            this.m_CachedComponentTypeInArchetypeArray = null;
        }

        private unsafe int PopulatedCachedTypeInArchetypeArray(ComponentType* requiredComponents, int count)
        {
            if ((count + 1) > 0x400)
            {
                throw new ArgumentException($"Archetypes can't hold more than {0x400}");
            }
            this.m_CachedComponentTypeInArchetypeArray[0] = new ComponentTypeInArchetype(ComponentType.Create<Entity>());
            for (int i = 0; i < count; i++)
            {
                SortingUtilities.InsertSorted(this.m_CachedComponentTypeInArchetypeArray, i + 1, requiredComponents[i]);
            }
            return (count + 1);
        }

        public void PrepareForDeserialize()
        {
            Unity.Assertions.Assert.AreEqual(0, this.Debug.EntityCount);
            this.m_SharedComponentManager.PrepareForDeserialize();
        }

        public void RemoveComponent<T>(Entity entity)
        {
            this.RemoveComponent(entity, ComponentType.Create<T>());
        }

        public unsafe void RemoveComponent(Entity entity, ComponentType type)
        {
            this.BeforeStructuralChange();
            this.Entities.AssertEntityHasComponent(entity, type);
            this.Entities.RemoveComponent(entity, type, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
            if (this.Entities.GetArchetype(entity).SystemStateCleanupComplete)
            {
                this.Entities.TryRemoveEntityId(&entity, 1, this.ArchetypeManager, this.m_SharedComponentManager, this.m_GroupManager, this.m_CachedComponentTypeInArchetypeArray);
            }
        }

        internal unsafe void SetBufferRaw(Entity entity, int componentTypeIndex, BufferHeader* tempBuffer, int sizeInChunk)
        {
            this.Entities.AssertEntityHasComponent(entity, componentTypeIndex);
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(componentTypeIndex);
            byte* numPtr = ref this.Entities.GetComponentDataWithTypeRW(entity, componentTypeIndex, this.Entities.GlobalSystemVersion);
            BufferHeader.Destroy((BufferHeader*) numPtr);
            UnsafeUtility.MemCpy((void*) numPtr, (void*) tempBuffer, (long) sizeInChunk);
        }

        public unsafe void SetComponentData<T>(Entity entity, T componentData) where T: struct, IComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            if (ComponentType.FromTypeIndex(typeIndex).IsZeroSized)
            {
                throw new ArgumentException($"GetComponentData<{typeof(T)}> can not be called with a zero sized component.");
            }
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            byte* numPtr = ref this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion);
            UnsafeUtility.CopyStructureToPtr<T>(ref componentData, (void*) numPtr);
        }

        internal unsafe void SetComponentDataRaw(Entity entity, int typeIndex, void* data, int size)
        {
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            this.ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);
            if (TypeManager.GetTypeInfo(typeIndex).SizeInChunk != size)
            {
                throw new ArgumentException($"SetComponentDataRaw<{TypeManager.GetType(typeIndex)}> can not be called with a zero sized component and must have same size as sizeof(T).");
            }
            UnsafeUtility.MemCpy((void*) this.Entities.GetComponentDataWithTypeRW(entity, typeIndex, this.Entities.GlobalSystemVersion), data, (long) size);
        }

        internal unsafe void SetComponentObject(Entity entity, ComponentType componentType, object componentObject)
        {
            Chunk* chunkPtr;
            int num;
            this.Entities.AssertEntityHasComponent(entity, componentType.TypeIndex);
            this.Entities.GetComponentChunk(entity, out chunkPtr, out num);
            this.ArchetypeManager.SetManagedObject(chunkPtr, componentType, num, componentObject);
        }

        public unsafe void SetSharedComponentData<T>(Entity entity, T componentData) where T: struct, ISharedComponentData
        {
            this.BeforeStructuralChange();
            int typeIndex = TypeManager.GetTypeIndex<T>();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            int newSharedComponentDataIndex = this.m_SharedComponentManager.InsertSharedComponent<T>(componentData);
            this.Entities.SetSharedComponentDataIndex(this.ArchetypeManager, this.m_SharedComponentManager, entity, typeIndex, newSharedComponentDataIndex);
            this.m_SharedComponentManager.RemoveReference(newSharedComponentDataIndex, 1);
        }

        internal unsafe void SetSharedComponentDataBoxed(Entity entity, int typeIndex, int hashCode, object componentData)
        {
            this.BeforeStructuralChange();
            this.Entities.AssertEntityHasComponent(entity, typeIndex);
            int newSharedComponentDataIndex = 0;
            if (componentData != null)
            {
                newSharedComponentDataIndex = this.m_SharedComponentManager.InsertSharedComponentAssumeNonDefault(typeIndex, hashCode, componentData, TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo);
            }
            this.Entities.SetSharedComponentDataIndex(this.ArchetypeManager, this.m_SharedComponentManager, entity, typeIndex, newSharedComponentDataIndex);
            this.m_SharedComponentManager.RemoveReference(newSharedComponentDataIndex, 1);
        }

        private unsafe bool TestMatchingArchetypeAll(Archetype* archetype, ComponentType* allTypes, int allCount)
        {
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            int num2 = 0;
            int typeIndex = TypeManager.GetTypeIndex<Disabled>();
            int num4 = TypeManager.GetTypeIndex<Prefab>();
            bool flag = false;
            bool flag2 = false;
            int index = 0;
            while (true)
            {
                if (index >= typesCount)
                {
                    bool flag9;
                    if (archetype.Disabled && !flag)
                    {
                        flag9 = false;
                    }
                    else if (archetype.Prefab && !flag2)
                    {
                        flag9 = false;
                    }
                    else
                    {
                        flag9 = num2 == allCount;
                    }
                    return flag9;
                }
                int num6 = types[index].TypeIndex;
                int num7 = 0;
                while (true)
                {
                    if (num7 >= allCount)
                    {
                        index++;
                        break;
                    }
                    int num8 = allTypes[num7].TypeIndex;
                    if (num8 == typeIndex)
                    {
                        flag = true;
                    }
                    if (num8 == num4)
                    {
                        flag2 = true;
                    }
                    if (num6 == num8)
                    {
                        num2++;
                    }
                    num7++;
                }
            }
        }

        private unsafe bool TestMatchingArchetypeAny(Archetype* archetype, ComponentType* anyTypes, int anyCount)
        {
            ComponentTypeInArchetype* types;
            int typesCount;
            int num2;
            if (anyCount != 0)
            {
                types = archetype.Types;
                typesCount = archetype.TypesCount;
                num2 = 0;
            }
            else
            {
                return true;
            }
            while (true)
            {
                while (true)
                {
                    bool flag2;
                    if (num2 < typesCount)
                    {
                        int typeIndex = types[num2].TypeIndex;
                        int index = 0;
                        while (true)
                        {
                            if (index < anyCount)
                            {
                                int num5 = anyTypes[index].TypeIndex;
                                if (typeIndex != num5)
                                {
                                    index++;
                                    continue;
                                }
                                flag2 = true;
                            }
                            else
                            {
                                num2++;
                                continue;
                            }
                            break;
                        }
                    }
                    else
                    {
                        flag2 = false;
                    }
                    return flag2;
                }
            }
        }

        private unsafe bool TestMatchingArchetypeNone(Archetype* archetype, ComponentType* noneTypes, int noneCount)
        {
            bool flag2;
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            int index = 0;
            while (true)
            {
                if (index < typesCount)
                {
                    int typeIndex = types[index].TypeIndex;
                    int num4 = 0;
                    while (true)
                    {
                        if (num4 < noneCount)
                        {
                            int num5 = noneTypes[num4].TypeIndex;
                            if (typeIndex != num5)
                            {
                                num4++;
                                continue;
                            }
                            return false;
                        }
                        else
                        {
                            index++;
                        }
                        break;
                    }
                    continue;
                }
                else
                {
                    flag2 = true;
                }
                break;
            }
            return flag2;
        }

        internal EntityDataManager* Entities
        {
            get => 
                this.m_Entities;
            private set => 
                (this.m_Entities = value);
        }

        internal Unity.Entities.ArchetypeManager ArchetypeManager
        {
            get => 
                this.m_ArchetypeManager;
            private set => 
                (this.m_ArchetypeManager = value);
        }

        public int Version =>
            (this.IsCreated ? this.m_Entities.Version : 0);

        public uint GlobalSystemVersion =>
            (this.IsCreated ? this.Entities.GlobalSystemVersion : 0);

        public bool IsCreated =>
            (this.m_CachedComponentTypeInArchetypeArray != null);

        public int EntityCapacity
        {
            get => 
                this.Entities.Capacity;
            set
            {
                this.BeforeStructuralChange();
                this.Entities.Capacity = value;
            }
        }

        internal Unity.Entities.ComponentJobSafetyManager ComponentJobSafetyManager { get; private set; }

        public JobHandle ExclusiveEntityTransactionDependency
        {
            get => 
                this.ComponentJobSafetyManager.ExclusiveTransactionDependency;
            set => 
                (this.ComponentJobSafetyManager.ExclusiveTransactionDependency = value);
        }

        public EntityManagerDebug Debug
        {
            get
            {
                EntityManagerDebug debug = this.m_Debug;
                if (this.m_Debug == null)
                {
                    object obj1 = this.m_Debug;
                    debug = this.m_Debug = new EntityManagerDebug(this);
                }
                return debug;
            }
        }

        public class EntityManagerDebug
        {
            private readonly EntityManager m_Manager;

            public EntityManagerDebug(EntityManager entityManager)
            {
                this.m_Manager = entityManager;
            }

            internal static unsafe string GetArchetypeDebugString(Archetype* a)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("(");
                int index = 0;
                while (true)
                {
                    if (index >= a.TypesCount)
                    {
                        builder.Append(")");
                        return builder.ToString();
                    }
                    ComponentTypeInArchetype archetype = a.Types[index];
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(archetype.ToString());
                    index++;
                }
            }

            public bool IsSharedComponentManagerEmpty() => 
                this.m_Manager.m_SharedComponentManager.IsEmpty();

            public unsafe void LogEntityInfo(Entity entity)
            {
                Archetype* archetypePtr = ref this.m_Manager.Entities.GetArchetype(entity);
                Unity.Debug.Log($"Entity {entity.Index}.{entity.Version}");
                for (int i = 0; i < archetypePtr->TypesCount; i++)
                {
                    ComponentTypeInArchetype archetype = archetypePtr->Types[i];
                    Unity.Debug.Log($"  - {archetype.ToString()}");
                }
            }

            public unsafe void PoisonUnusedDataInAllChunks(EntityArchetype archetype, byte value)
            {
                for (UnsafeLinkedListNode* nodePtr = archetype.Archetype.ChunkList.Begin; nodePtr != archetype.Archetype.ChunkList.End; nodePtr = nodePtr->Next)
                {
                    ChunkDataUtility.PoisonUnusedChunkData((Chunk*) nodePtr, value);
                }
            }

            public unsafe void SetGlobalSystemVersion(uint version)
            {
                this.m_Manager.Entities.GlobalSystemVersion = version;
            }

            public int EntityCount
            {
                get
                {
                    NativeArray<Entity> allEntities = this.m_Manager.GetAllEntities(Allocator.Temp);
                    int length = allEntities.Length;
                    allEntities.Dispose();
                    return length;
                }
            }
        }
    }
}

