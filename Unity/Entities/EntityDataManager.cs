namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityDataManager
    {
        private EntityData m_Entities;
        private int m_EntitiesCapacity;
        private int m_EntitiesFreeIndex;
        private unsafe int* m_ComponentTypeOrderVersion;
        public uint GlobalSystemVersion;
        public int Version =>
            this.GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<Entity>());
        public void IncrementGlobalSystemVersion()
        {
            ChangeVersionUtility.IncrementGlobalSystemVersion(ref this.GlobalSystemVersion);
        }

        private unsafe EntityData CreateEntityData(int newCapacity)
        {
            EntityData data = new EntityData();
            int num = ((newCapacity * 4) + 0x3f) & -64;
            int num2 = ((newCapacity * sizeof(Archetype*)) + 0x3f) & -64;
            int num3 = ((newCapacity * sizeof(EntityChunkData)) + 0x3f) & -64;
            byte* numPtr = (byte*) UnsafeUtility.Malloc((long) ((num + num2) + num3), 0x40, Allocator.Persistent);
            data.Version = (int*) numPtr;
            data.Archetype = (Archetype**) (numPtr + num);
            data.ChunkData = (EntityChunkData*) ((numPtr + num) + num2);
            return data;
        }

        private unsafe void FreeEntityData(ref EntityData entities)
        {
            UnsafeUtility.Free((void*) entities.Version, Allocator.Persistent);
            entities.Version = null;
            entities.Archetype = null;
            entities.ChunkData = null;
        }

        private unsafe void CopyEntityData(ref EntityData dstEntityData, EntityData srcEntityData, long copySize)
        {
            UnsafeUtility.MemCpy((void*) dstEntityData.Version, (void*) srcEntityData.Version, copySize * 4L);
            UnsafeUtility.MemCpy((void*) dstEntityData.Archetype, (void*) srcEntityData.Archetype, copySize * sizeof(Archetype*));
            UnsafeUtility.MemCpy((void*) dstEntityData.ChunkData, (void*) srcEntityData.ChunkData, copySize * sizeof(EntityChunkData));
        }

        public unsafe void OnCreate()
        {
            this.m_EntitiesCapacity = 10;
            this.m_Entities = this.CreateEntityData(this.m_EntitiesCapacity);
            this.m_EntitiesFreeIndex = 0;
            this.GlobalSystemVersion = 1;
            this.InitializeAdditionalCapacity(0);
            this.m_ComponentTypeOrderVersion = (int*) UnsafeUtility.Malloc(0xa000L, UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            UnsafeUtility.MemClear((void*) this.m_ComponentTypeOrderVersion, 0xa000L);
        }

        public unsafe void OnDestroy()
        {
            this.FreeEntityData(ref this.m_Entities);
            this.m_EntitiesCapacity = 0;
            UnsafeUtility.Free((void*) this.m_ComponentTypeOrderVersion, Allocator.Persistent);
            this.m_ComponentTypeOrderVersion = null;
        }

        private unsafe void InitializeAdditionalCapacity(int start)
        {
            int index = start;
            while (true)
            {
                if (index == this.m_EntitiesCapacity)
                {
                    this.m_Entities.ChunkData[this.m_EntitiesCapacity - 1].IndexInChunk = -1;
                    return;
                }
                this.m_Entities.ChunkData[index].IndexInChunk = index + 1;
                this.m_Entities.Version[index] = 1;
                this.m_Entities.ChunkData[index].Chunk = null;
                index++;
            }
        }

        private void IncreaseCapacity()
        {
            this.Capacity = 2 * this.Capacity;
        }

        public int Capacity
        {
            get => 
                this.m_EntitiesCapacity;
            set
            {
                if (value > this.m_EntitiesCapacity)
                {
                    EntityData dstEntityData = this.CreateEntityData(value);
                    this.CopyEntityData(ref dstEntityData, this.m_Entities, (long) this.m_EntitiesCapacity);
                    this.FreeEntityData(ref this.m_Entities);
                    int start = this.m_EntitiesCapacity - 1;
                    this.m_Entities = dstEntityData;
                    this.m_EntitiesCapacity = value;
                    this.InitializeAdditionalCapacity(start);
                }
            }
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateEntity(Entity entity)
        {
            if (entity.Index >= this.m_EntitiesCapacity)
            {
                throw new ArgumentException("All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
            }
        }

        public unsafe bool Exists(Entity entity)
        {
            int index = entity.Index;
            this.ValidateEntity(entity);
            return ((this.m_Entities.Version[index] == entity.Version) & (this.m_Entities.ChunkData[index].Chunk != null));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public unsafe void AssertEntitiesExist(Entity* entities, int count)
        {
            for (int i = 0; i != count; i++)
            {
                Entity* entityPtr = entities + i;
                int index = entityPtr->Index;
                if (index >= this.m_EntitiesCapacity)
                {
                    throw new ArgumentException("All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
                }
                if (this.m_Entities.Version[index] != entityPtr->Version)
                {
                    throw new ArgumentException("All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, ComponentType componentType)
        {
            if (!this.HasComponent(entity, componentType))
            {
                if (!this.Exists(entity))
                {
                    throw new ArgumentException("The Entity does not exist");
                }
                if (this.HasComponent(entity, componentType.TypeIndex))
                {
                    throw new ArgumentException($"The component typeof({componentType.GetManagedType()}) exists on the entity but the exact type {componentType} does not");
                }
                throw new ArgumentException($"{componentType} component has not been added to the entity.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, int componentType)
        {
            if (!this.HasComponent(entity, componentType))
            {
                if (!this.Exists(entity))
                {
                    throw new ArgumentException("The entity does not exist");
                }
                throw new ArgumentException("The component has not been added to the entity.");
            }
        }

        private static unsafe Chunk* EntityChunkBatch(EntityDataManager* entityDataManager, Entity* entities, int count, out int indexInChunk, out int batchCount)
        {
            int index = entities.Index;
            Chunk* chunk = entityDataManager.m_Entities.ChunkData[index].Chunk;
            indexInChunk = entityDataManager.m_Entities.ChunkData[index].IndexInChunk;
            batchCount = 0;
            while (true)
            {
                if (batchCount < count)
                {
                    int num2 = entities[batchCount].Index;
                    Chunk* chunkPtr2 = entityDataManager.m_Entities.ChunkData[num2].Chunk;
                    int num3 = entityDataManager.m_Entities.ChunkData[num2].IndexInChunk;
                    if ((chunkPtr2 == chunk) && (num3 == (indexInChunk + batchCount)))
                    {
                        batchCount++;
                        continue;
                    }
                }
                return chunk;
            }
        }

        private static unsafe void DeallocateDataEntitiesInChunk(EntityDataManager* entityDataManager, Entity* entities, Chunk* chunk, int indexInChunk, int batchCount)
        {
            DeallocateBuffers(entityDataManager, entities, chunk, batchCount);
            int entitiesFreeIndex = entityDataManager.m_EntitiesFreeIndex;
            int index = batchCount - 1;
            while (true)
            {
                if (index < 0)
                {
                    entityDataManager.m_EntitiesFreeIndex = entitiesFreeIndex;
                    int count = Math.Min(batchCount, (chunk.Count - indexInChunk) - batchCount);
                    if (count != 0)
                    {
                        Entity* entityPtr = (Entity*) (&chunk.Buffer.FixedElementField + ((chunk.Count - count) * sizeof(Entity)));
                        int num5 = 0;
                        while (true)
                        {
                            if (num5 == count)
                            {
                                ChunkDataUtility.Copy(chunk, chunk.Count - count, chunk, indexInChunk, count);
                                break;
                            }
                            entityDataManager.m_Entities.ChunkData[entityPtr[num5].Index].IndexInChunk = indexInChunk + num5;
                            num5++;
                        }
                    }
                    return;
                }
                int num4 = entities[index].Index;
                entityDataManager.m_Entities.ChunkData[num4].Chunk = null;
                int* numPtr1 = entityDataManager.m_Entities.Version + num4;
                numPtr1[0]++;
                entityDataManager.m_Entities.ChunkData[num4].IndexInChunk = entitiesFreeIndex;
                entitiesFreeIndex = num4;
                index--;
            }
        }

        private static unsafe void DeallocateBuffers(EntityDataManager* entityDataManager, Entity* entities, Chunk* chunk, int batchCount)
        {
            Archetype* archetypePtr = chunk.Archetype;
            for (int i = 0; i < archetypePtr->TypesCount; i++)
            {
                ComponentTypeInArchetype archetype = archetypePtr->Types[i];
                if (archetype.IsBuffer)
                {
                    byte* numPtr = &chunk.Buffer.FixedElementField + archetypePtr->Offsets[i];
                    int num2 = archetypePtr->SizeOfs[i];
                    int index = 0;
                    while (true)
                    {
                        if (index >= batchCount)
                        {
                            break;
                        }
                        Entity entity = entities[index];
                        int indexInChunk = entityDataManager.m_Entities.ChunkData[entity.Index].IndexInChunk;
                        byte* numPtr2 = numPtr + (num2 * indexInChunk);
                        BufferHeader.Destroy((BufferHeader*) numPtr2);
                        index++;
                    }
                }
            }
        }

        public unsafe int CheckInternalConsistency()
        {
            int num = 0;
            int typeIndex = TypeManager.GetTypeIndex<Entity>();
            for (int i = 0; i != this.m_EntitiesCapacity; i++)
            {
                if (this.m_Entities.ChunkData[i].Chunk != null)
                {
                    num++;
                    Unity.Assertions.Assert.AreEqual(typeIndex, *(((IntPtr*) (this.m_Entities.Archetype + i))).Types.TypeIndex);
                    Entity entity = *((Entity*) ChunkDataUtility.GetComponentDataRO(this.m_Entities.ChunkData[i].Chunk, this.m_Entities.ChunkData[i].IndexInChunk, 0));
                    Unity.Assertions.Assert.AreEqual(i, entity.Index);
                    Unity.Assertions.Assert.AreEqual(this.m_Entities.Version[i], entity.Version);
                    Unity.Assertions.Assert.IsTrue(this.Exists(entity));
                }
            }
            return num;
        }

        public unsafe void AllocateConsecutiveEntitiesForLoading(int count)
        {
            int num = count + 1;
            this.Capacity = num;
            this.m_EntitiesFreeIndex = (this.Capacity == num) ? -1 : num;
            for (int i = 1; i < num; i++)
            {
                if (this.m_Entities.ChunkData[i].Chunk != null)
                {
                    throw new ArgumentException("loading into non-empty entity manager is not supported");
                }
                this.m_Entities.ChunkData[i].IndexInChunk = 0;
                this.m_Entities.Version[i] = 0;
            }
        }

        internal unsafe void AddExistingChunk(Chunk* chunk)
        {
            for (int i = 0; i < chunk.Count; i++)
            {
                Entity* entityPtr = (Entity*) ref ChunkDataUtility.GetComponentDataRO(chunk, i, 0);
                this.m_Entities.ChunkData[entityPtr->Index].Chunk = chunk;
                this.m_Entities.ChunkData[entityPtr->Index].IndexInChunk = i;
                *((IntPtr*) (this.m_Entities.Archetype + entityPtr->Index)) = chunk.Archetype;
            }
        }

        public unsafe void AllocateEntities(Archetype* arch, Chunk* chunk, int baseIndex, int count, Entity* outputEntities)
        {
            Unity.Assertions.Assert.AreEqual(chunk.Archetype.Offsets[0], 0);
            Unity.Assertions.Assert.AreEqual(chunk.Archetype.SizeOfs[0], sizeof(Entity));
            Entity* entityPtr = (Entity*) (&chunk.Buffer.FixedElementField + (baseIndex * sizeof(Entity)));
            for (int i = 0; i != count; i++)
            {
                int indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                if (indexInChunk == -1)
                {
                    this.IncreaseCapacity();
                    indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                }
                int num3 = this.m_Entities.Version[this.m_EntitiesFreeIndex];
                outputEntities[i].Index = this.m_EntitiesFreeIndex;
                outputEntities[i].Version = num3;
                Entity* entityPtr2 = entityPtr + i;
                entityPtr2->Index = this.m_EntitiesFreeIndex;
                entityPtr2->Version = num3;
                this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk = baseIndex + i;
                *((IntPtr*) (this.m_Entities.Archetype + this.m_EntitiesFreeIndex)) = arch;
                this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].Chunk = chunk;
                this.m_EntitiesFreeIndex = indexInChunk;
            }
        }

        public unsafe void AllocateEntitiesForRemapping(EntityDataManager* srcEntityDataManager, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            EntityData entities = srcEntityDataManager.m_Entities;
            int entitiesCapacity = srcEntityDataManager.m_EntitiesCapacity;
            for (int i = 0; i != entitiesCapacity; i++)
            {
                if (entities.ChunkData[i].Chunk != null)
                {
                    int indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                    if (indexInChunk == -1)
                    {
                        this.IncreaseCapacity();
                        indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                    }
                    Entity source = new Entity {
                        Version = entities.Version[i],
                        Index = i
                    };
                    source = new Entity {
                        Version = this.m_Entities.Version[this.m_EntitiesFreeIndex],
                        Index = this.m_EntitiesFreeIndex
                    };
                    EntityRemapUtility.AddEntityRemapping(ref entityRemapping, source, source);
                    this.m_EntitiesFreeIndex = indexInChunk;
                }
            }
        }

        public unsafe void AllocateEntitiesForRemapping(EntityDataManager* srcEntityDataManager, Chunk* chunk, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            int count = chunk.Count;
            Entity* entityPtr = (Entity*) &chunk.Buffer.FixedElementField;
            for (int i = 0; i != count; i++)
            {
                int indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                if (indexInChunk == -1)
                {
                    this.IncreaseCapacity();
                    indexInChunk = this.m_Entities.ChunkData[this.m_EntitiesFreeIndex].IndexInChunk;
                }
                Entity source = new Entity {
                    Version = entityPtr[i].Version,
                    Index = entityPtr[i].Index
                };
                source = new Entity {
                    Version = this.m_Entities.Version[this.m_EntitiesFreeIndex],
                    Index = this.m_EntitiesFreeIndex
                };
                EntityRemapUtility.AddEntityRemapping(ref entityRemapping, source, source);
                this.m_EntitiesFreeIndex = indexInChunk;
            }
        }

        public unsafe void RemapChunk(Archetype* arch, Chunk* chunk, int baseIndex, int count, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            Unity.Assertions.Assert.AreEqual(chunk.Archetype.Offsets[0], 0);
            Unity.Assertions.Assert.AreEqual(chunk.Archetype.SizeOfs[0], sizeof(Entity));
            Entity* entityPtr = (Entity*) (&chunk.Buffer.FixedElementField + (baseIndex * sizeof(Entity)));
            for (int i = 0; i != count; i++)
            {
                Entity* entityPtr2 = entityPtr + i;
                Entity entity = EntityRemapUtility.RemapEntity(ref entityRemapping, entityPtr2[0]);
                int expected = this.m_Entities.Version[entity.Index];
                Unity.Assertions.Assert.AreEqual(expected, entity.Version);
                entityPtr2->Index = entity.Index;
                entityPtr2->Version = expected;
                this.m_Entities.ChunkData[entity.Index].IndexInChunk = baseIndex + i;
                *((IntPtr*) (this.m_Entities.Archetype + entity.Index)) = arch;
                this.m_Entities.ChunkData[entity.Index].Chunk = chunk;
            }
        }

        public unsafe void FreeAllEntities()
        {
            int index = 0;
            while (true)
            {
                if (index == this.m_EntitiesCapacity)
                {
                    this.m_Entities.ChunkData[this.m_EntitiesCapacity - 1].IndexInChunk = -1;
                    this.m_EntitiesFreeIndex = 0;
                    return;
                }
                this.m_Entities.ChunkData[index].IndexInChunk = index + 1;
                int* numPtr1 = this.m_Entities.Version + index;
                numPtr1[0]++;
                this.m_Entities.ChunkData[index].Chunk = null;
                index++;
            }
        }

        public unsafe void FreeEntities(Chunk* chunk)
        {
            int count = chunk.Count;
            Entity* entityPtr = (Entity*) &chunk.Buffer.FixedElementField;
            int entitiesFreeIndex = this.m_EntitiesFreeIndex;
            int index = 0;
            while (true)
            {
                if (index == count)
                {
                    this.m_EntitiesFreeIndex = entitiesFreeIndex;
                    return;
                }
                int num4 = entityPtr[index].Index;
                int* numPtr1 = this.m_Entities.Version + num4;
                numPtr1[0]++;
                this.m_Entities.ChunkData[num4].Chunk = null;
                this.m_Entities.ChunkData[num4].IndexInChunk = entitiesFreeIndex;
                entitiesFreeIndex = num4;
                index++;
            }
        }

        public unsafe bool HasComponent(Entity entity, int type)
        {
            bool flag2;
            if (!this.Exists(entity))
            {
                flag2 = false;
            }
            else
            {
                flag2 = ChunkDataUtility.GetIndexInTypeArray(this.m_Entities.Archetype[entity.Index], type) != -1;
            }
            return flag2;
        }

        public unsafe bool HasComponent(Entity entity, ComponentType type)
        {
            bool flag2;
            if (!this.Exists(entity))
            {
                flag2 = false;
            }
            else
            {
                flag2 = ChunkDataUtility.GetIndexInTypeArray(this.m_Entities.Archetype[entity.Index], type.TypeIndex) != -1;
            }
            return flag2;
        }

        public unsafe int GetSizeInChunk(Entity entity, int typeIndex, ref int typeLookupCache) => 
            ChunkDataUtility.GetSizeInChunk(this.m_Entities.ChunkData[entity.Index].Chunk, typeIndex, ref typeLookupCache);

        public unsafe byte* GetComponentDataWithTypeRO(Entity entity, int typeIndex) => 
            ref ChunkDataUtility.GetComponentDataWithTypeRO(this.m_Entities.ChunkData[entity.Index].Chunk, this.m_Entities.ChunkData[entity.Index].IndexInChunk, typeIndex);

        public unsafe byte* GetComponentDataWithTypeRW(Entity entity, int typeIndex, uint globalVersion) => 
            ref ChunkDataUtility.GetComponentDataWithTypeRW(this.m_Entities.ChunkData[entity.Index].Chunk, this.m_Entities.ChunkData[entity.Index].IndexInChunk, typeIndex, globalVersion);

        public unsafe byte* GetComponentDataWithTypeRO(Entity entity, int typeIndex, ref int typeLookupCache) => 
            ref ChunkDataUtility.GetComponentDataWithTypeRO(this.m_Entities.ChunkData[entity.Index].Chunk, this.m_Entities.ChunkData[entity.Index].IndexInChunk, typeIndex, ref typeLookupCache);

        public unsafe byte* GetComponentDataWithTypeRW(Entity entity, int typeIndex, uint globalVersion, ref int typeLookupCache) => 
            ref ChunkDataUtility.GetComponentDataWithTypeRW(this.m_Entities.ChunkData[entity.Index].Chunk, this.m_Entities.ChunkData[entity.Index].IndexInChunk, typeIndex, globalVersion, ref typeLookupCache);

        public unsafe Chunk* GetComponentChunk(Entity entity) => 
            this.m_Entities.ChunkData[entity.Index].Chunk;

        public unsafe void GetComponentChunk(Entity entity, out Chunk* chunk, out int chunkIndex)
        {
            Chunk* chunkPtr = this.m_Entities.ChunkData[entity.Index].Chunk;
            int indexInChunk = this.m_Entities.ChunkData[entity.Index].IndexInChunk;
            chunk = chunkPtr;
            chunkIndex = indexInChunk;
        }

        public unsafe Archetype* GetArchetype(Entity entity) => 
            this.m_Entities.Archetype[entity.Index];

        public unsafe Archetype* GetInstantiableArchetype(Entity entity, ArchetypeManager archetypeManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray) => 
            this.GetArchetype(entity).InstantiableArchetype;

        public unsafe void SetArchetype(ArchetypeManager typeMan, Entity entity, Archetype* archetype, int* sharedComponentDataIndices)
        {
            Chunk* chunk = ref typeMan.GetChunkWithEmptySlots(archetype, sharedComponentDataIndices);
            int dstIndex = typeMan.AllocateIntoChunk(chunk);
            Archetype* archetypePtr = this.m_Entities.Archetype[entity.Index];
            Chunk* srcChunk = this.m_Entities.ChunkData[entity.Index].Chunk;
            int indexInChunk = this.m_Entities.ChunkData[entity.Index].IndexInChunk;
            ChunkDataUtility.Convert(srcChunk, indexInChunk, chunk, dstIndex);
            if ((chunk->ManagedArrayIndex >= 0) && (srcChunk->ManagedArrayIndex >= 0))
            {
                ChunkDataUtility.CopyManagedObjects(typeMan, srcChunk, indexInChunk, chunk, dstIndex, 1);
            }
            *((IntPtr*) (this.m_Entities.Archetype + entity.Index)) = archetype;
            this.m_Entities.ChunkData[entity.Index].Chunk = chunk;
            this.m_Entities.ChunkData[entity.Index].IndexInChunk = dstIndex;
            int index = srcChunk->Count - 1;
            if (index != indexInChunk)
            {
                Entity* entityPtr = (Entity*) ref ChunkDataUtility.GetComponentDataRO(srcChunk, index, 0);
                this.m_Entities.ChunkData[entityPtr->Index].IndexInChunk = indexInChunk;
                ChunkDataUtility.Copy(srcChunk, index, srcChunk, indexInChunk, 1);
                if (srcChunk->ManagedArrayIndex >= 0)
                {
                    ChunkDataUtility.CopyManagedObjects(typeMan, srcChunk, index, srcChunk, indexInChunk, 1);
                }
            }
            if (srcChunk->ManagedArrayIndex >= 0)
            {
                ChunkDataUtility.ClearManagedObjects(typeMan, srcChunk, index, 1);
            }
            int* numPtr1 = (int*) ref archetypePtr->EntityCount;
            numPtr1[0]--;
            typeMan.SetChunkCount(srcChunk, index);
        }

        public unsafe void AddComponents(Entity entity, ComponentTypes types, ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            Archetype* archetypePtr = ref this.GetArchetype(entity);
            int* numPtr = (int*) stackalloc byte[(((IntPtr) types.Length) * 4)];
            int typesCount = archetypePtr->TypesCount;
            int length = types.Length;
            int actual = typesCount + length;
            while (true)
            {
                if ((typesCount <= 0) || (length <= 0))
                {
                    while (true)
                    {
                        if (length <= 0)
                        {
                            Unity.Assertions.Assert.AreEqual(length, 0);
                            Unity.Assertions.Assert.AreEqual(typesCount, actual);
                            int count = archetypePtr->TypesCount + types.Length;
                            Archetype* archetypePtr2 = ref archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, count, groupManager);
                            int* sharedComponentValueArray = this.GetComponentChunk(entity).SharedComponentValueArray;
                            if (types.m_masks.m_SharedComponentMask != 0)
                            {
                                int* numPtr4 = sharedComponentValueArray;
                                sharedComponentValueArray = (int*) stackalloc byte[(((IntPtr) archetypePtr2->NumSharedComponents) * 4)];
                                ulong x = 0L;
                                int index = 0;
                                while (true)
                                {
                                    if (index >= types.Length)
                                    {
                                        Unity.Assertions.Assert.AreEqual(math.countbits(x), types.m_masks.SharedComponents);
                                        int expected = 0;
                                        int num10 = 0;
                                        while (true)
                                        {
                                            if (num10 >= archetypePtr2->NumSharedComponents)
                                            {
                                                Unity.Assertions.Assert.AreEqual(expected, archetypePtr->NumSharedComponents);
                                                break;
                                            }
                                            if ((x & ((ulong) (1L << (num10 & 0x3f)))) != 0L)
                                            {
                                                sharedComponentValueArray[num10] = 0;
                                            }
                                            else
                                            {
                                                Unity.Assertions.Assert.IsTrue(expected < archetypePtr->NumSharedComponents);
                                                expected++;
                                                sharedComponentValueArray[num10] = numPtr4[expected];
                                            }
                                            num10++;
                                        }
                                        break;
                                    }
                                    if (types.m_masks.IsSharedComponent(index))
                                    {
                                        int num8 = numPtr[index];
                                        Unity.Assertions.Assert.IsTrue((num8 >= 0) && (num8 < archetypePtr2->TypesCount));
                                        int num9 = archetypePtr2->SharedComponentOffset[num8];
                                        Unity.Assertions.Assert.IsTrue((num9 >= 0) && (num9 < archetypePtr2->NumSharedComponents));
                                        x |= (ulong) (1L << (num9 & 0x3f));
                                    }
                                    index++;
                                }
                            }
                            this.SetArchetype(archetypeManager, entity, archetypePtr2, sharedComponentValueArray);
                            this.IncrementComponentOrderVersion(archetypePtr2, this.GetComponentChunk(entity), sharedComponentDataManager);
                            return;
                        }
                        ComponentType type = types.GetComponentType(length - 1);
                        ComponentTypeInArchetype archetype3 = new ComponentTypeInArchetype(type);
                        componentTypeInArchetypeArray[--actual] = archetype3;
                        length--;
                        numPtr[length] = actual;
                    }
                }
                ComponentTypeInArchetype archetype = componentTypeInArchetypeArray[typesCount - 1];
                ComponentType componentType = types.GetComponentType(length - 1);
                if (archetype.TypeIndex > componentType.TypeIndex)
                {
                    componentTypeInArchetypeArray[--actual] = archetype;
                    typesCount--;
                    continue;
                }
                ComponentTypeInArchetype archetype2 = new ComponentTypeInArchetype(componentType);
                componentTypeInArchetypeArray[--actual] = archetype2;
                numPtr[length - 1] = actual;
            }
        }

        public unsafe void AddComponent(Entity entity, ComponentType type, ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            ComponentTypeInArchetype archetype = new ComponentTypeInArchetype(type);
            Archetype* archetypePtr = ref this.GetArchetype(entity);
            int index = 0;
            while (true)
            {
                if ((index >= archetypePtr->TypesCount) || (archetypePtr->Types[index] >= archetype))
                {
                    int num2 = index;
                    componentTypeInArchetypeArray[index] = archetype;
                    while (true)
                    {
                        if (index >= archetypePtr->TypesCount)
                        {
                            Archetype* archetypePtr2 = ref archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, archetypePtr->TypesCount + 1, groupManager);
                            int* sharedComponentValueArray = this.GetComponentChunk(entity).SharedComponentValueArray;
                            if ((archetypePtr2->NumSharedComponents > 0) && (archetypePtr2->NumSharedComponents != archetypePtr->NumSharedComponents))
                            {
                                int* numPtr2 = sharedComponentValueArray;
                                sharedComponentValueArray = (int*) stackalloc byte[(((IntPtr) archetypePtr2->NumSharedComponents) * 4)];
                                int num3 = archetypePtr2->SharedComponentOffset[num2];
                                UnsafeUtility.MemCpy((void*) sharedComponentValueArray, (void*) numPtr2, (long) (num3 * 4));
                                sharedComponentValueArray[num3] = 0;
                                UnsafeUtility.MemCpy((void*) ((sharedComponentValueArray + num3) + 1), (void*) (numPtr2 + num3), (long) ((archetypePtr->NumSharedComponents - num3) * 4));
                            }
                            this.SetArchetype(archetypeManager, entity, archetypePtr2, sharedComponentValueArray);
                            this.IncrementComponentOrderVersion(archetypePtr2, this.GetComponentChunk(entity), sharedComponentDataManager);
                            return;
                        }
                        componentTypeInArchetypeArray[index + 1] = archetypePtr->Types[index];
                        index++;
                    }
                }
                componentTypeInArchetypeArray[index] = archetypePtr->Types[index];
                index++;
            }
        }

        public unsafe void TryRemoveEntityId(Entity* entities, int count, ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            int index = 0;
            while (index != count)
            {
                int num2;
                int num3;
                EntityDataManager* entityDataManager = (EntityDataManager*) this;
                Chunk* chunk = ref EntityChunkBatch(entityDataManager, entities + index, count - index, out num2, out num3);
                Archetype* archetypePtr = ref this.GetArchetype(entities[index]);
                if (!archetypePtr->SystemStateCleanupNeeded)
                {
                    DeallocateDataEntitiesInChunk(entityDataManager, entities + index, chunk, num2, num3);
                    this.IncrementComponentOrderVersion(chunk->Archetype, chunk, sharedComponentDataManager);
                    if (chunk->ManagedArrayIndex >= 0)
                    {
                        if (chunk->Count != (num2 + num3))
                        {
                            ChunkDataUtility.CopyManagedObjects(archetypeManager, chunk, chunk->Count - num3, chunk, num2, num3);
                        }
                        ChunkDataUtility.ClearManagedObjects(archetypeManager, chunk, chunk->Count - num3, num3);
                    }
                    int* numPtr1 = (int*) ref chunk->Archetype.EntityCount;
                    numPtr1[0] -= num3;
                    archetypeManager.SetChunkCount(chunk, chunk->Count - num3);
                }
                else
                {
                    Archetype* systemStateResidueArchetype = archetypePtr->SystemStateResidueArchetype;
                    int* sharedComponentValueArray = chunk->SharedComponentValueArray;
                    if ((systemStateResidueArchetype->NumSharedComponents > 0) && (systemStateResidueArchetype->NumSharedComponents != archetypePtr->NumSharedComponents))
                    {
                        int* numPtr2 = sharedComponentValueArray;
                        sharedComponentValueArray = (int*) stackalloc byte[(((IntPtr) systemStateResidueArchetype->NumSharedComponents) * 4)];
                        int num4 = 0;
                        int num5 = 0;
                        while (true)
                        {
                            if (num5 >= systemStateResidueArchetype->TypesCount)
                            {
                                break;
                            }
                            if (systemStateResidueArchetype->SharedComponentOffset[num5] != -1)
                            {
                                ComponentTypeInArchetype archetype = systemStateResidueArchetype->Types[num5];
                                while (true)
                                {
                                    if (!(archetype != archetypePtr->Types[num4]))
                                    {
                                        sharedComponentValueArray[systemStateResidueArchetype->SharedComponentOffset[num5]] = numPtr2[archetypePtr->SharedComponentOffset[num4]];
                                        break;
                                    }
                                    num4++;
                                }
                            }
                            num5++;
                            num4++;
                        }
                    }
                    int num8 = 0;
                    while (true)
                    {
                        if (num8 >= num3)
                        {
                            break;
                        }
                        Entity entity = entities[index + num8];
                        this.IncrementComponentOrderVersion(archetypePtr, this.GetComponentChunk(entity), sharedComponentDataManager);
                        this.SetArchetype(archetypeManager, entity, systemStateResidueArchetype, sharedComponentValueArray);
                        num8++;
                    }
                }
                fixed (EntityDataManager* managerRef = null)
                {
                    index += num3;
                }
            }
        }

        public unsafe void RemoveComponent(Entity entity, ComponentType type, ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            ComponentTypeInArchetype archetype = new ComponentTypeInArchetype(type);
            Archetype* archetypePtr = ref this.GetArchetype(entity);
            int num = 0;
            int index = -1;
            int num3 = 0;
            while (true)
            {
                if (num3 >= archetypePtr->TypesCount)
                {
                    Archetype* archetypePtr2 = ref archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, archetypePtr->TypesCount - num, groupManager);
                    int* sharedComponentValueArray = this.GetComponentChunk(entity).SharedComponentValueArray;
                    if ((archetypePtr2->NumSharedComponents > 0) && (archetypePtr2->NumSharedComponents != archetypePtr->NumSharedComponents))
                    {
                        int* numPtr2 = sharedComponentValueArray;
                        sharedComponentValueArray = (int*) stackalloc byte[(((IntPtr) archetypePtr2->NumSharedComponents) * 4)];
                        int num4 = archetypePtr->SharedComponentOffset[index];
                        UnsafeUtility.MemCpy((void*) sharedComponentValueArray, (void*) numPtr2, (long) (num4 * 4));
                        UnsafeUtility.MemCpy((void*) (sharedComponentValueArray + num4), (void*) ((numPtr2 + num4) + 1), (long) ((archetypePtr2->NumSharedComponents - num4) * 4));
                    }
                    this.IncrementComponentOrderVersion(archetypePtr, this.GetComponentChunk(entity), sharedComponentDataManager);
                    this.SetArchetype(archetypeManager, entity, archetypePtr2, sharedComponentValueArray);
                    return;
                }
                if (archetypePtr->Types[num3].TypeIndex != archetype.TypeIndex)
                {
                    componentTypeInArchetypeArray[num3 - num] = archetypePtr->Types[num3];
                }
                else
                {
                    index = num3;
                    num++;
                }
                num3++;
            }
        }

        public unsafe void MoveEntityToChunk(ArchetypeManager typeMan, Entity entity, Chunk* newChunk, int newChunkIndex)
        {
            Chunk* chunk = this.m_Entities.ChunkData[entity.Index].Chunk;
            Unity.Assertions.Assert.IsTrue(chunk->Archetype == newChunk.Archetype);
            int indexInChunk = this.m_Entities.ChunkData[entity.Index].IndexInChunk;
            ChunkDataUtility.Copy(chunk, indexInChunk, newChunk, newChunkIndex, 1);
            if (chunk->ManagedArrayIndex >= 0)
            {
                ChunkDataUtility.CopyManagedObjects(typeMan, chunk, indexInChunk, newChunk, newChunkIndex, 1);
            }
            this.m_Entities.ChunkData[entity.Index].Chunk = newChunk;
            this.m_Entities.ChunkData[entity.Index].IndexInChunk = newChunkIndex;
            int index = chunk->Count - 1;
            if (index != indexInChunk)
            {
                Entity* entityPtr = (Entity*) ref ChunkDataUtility.GetComponentDataRO(chunk, index, 0);
                this.m_Entities.ChunkData[entityPtr->Index].IndexInChunk = indexInChunk;
                ChunkDataUtility.Copy(chunk, index, chunk, indexInChunk, 1);
                if (chunk->ManagedArrayIndex >= 0)
                {
                    ChunkDataUtility.CopyManagedObjects(typeMan, chunk, index, chunk, indexInChunk, 1);
                }
            }
            if (chunk->ManagedArrayIndex >= 0)
            {
                ChunkDataUtility.ClearManagedObjects(typeMan, chunk, index, 1);
            }
            int* numPtr1 = (int*) ref newChunk.Archetype.EntityCount;
            numPtr1[0]--;
            typeMan.SetChunkCount(chunk, chunk->Count - 1);
        }

        public unsafe void CreateEntities(ArchetypeManager archetypeManager, Archetype* archetype, Entity* entities, int count)
        {
            int* sharedComponentDataIndices = (int*) stackalloc byte[(((IntPtr) archetype.NumSharedComponents) * 4)];
            UnsafeUtility.MemClear((void*) sharedComponentDataIndices, (long) (archetype.NumSharedComponents * 4));
            while (true)
            {
                int num;
                if (count == 0)
                {
                    this.IncrementComponentTypeOrderVersion(archetype);
                    return;
                }
                Chunk* chunk = ref archetypeManager.GetChunkWithEmptySlots(archetype, sharedComponentDataIndices);
                int num2 = archetypeManager.AllocateIntoChunk(chunk, count, out num);
                this.AllocateEntities(archetype, chunk, num, num2, entities);
                ChunkDataUtility.InitializeComponents(chunk, num, num2);
                entities += num2;
                count -= num2;
            }
        }

        public unsafe void InstantiateEntities(ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, Entity srcEntity, Entity* outputEntities, int count, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            int indexInChunk = this.m_Entities.ChunkData[srcEntity.Index].IndexInChunk;
            Chunk* chunk = this.m_Entities.ChunkData[srcEntity.Index].Chunk;
            Archetype* archetype = ref this.GetInstantiableArchetype(srcEntity, archetypeManager, groupManager, componentTypeInArchetypeArray);
            int* sharedComponentValueArray = this.GetComponentChunk(srcEntity).SharedComponentValueArray;
            while (true)
            {
                int num2;
                if (count == 0)
                {
                    this.IncrementComponentOrderVersion(archetype, chunk, sharedComponentDataManager);
                    return;
                }
                Chunk* chunkPtr2 = ref archetypeManager.GetChunkWithEmptySlots(archetype, sharedComponentValueArray);
                int num3 = archetypeManager.AllocateIntoChunk(chunkPtr2, count, out num2);
                ChunkDataUtility.ReplicateComponents(chunk, indexInChunk, chunkPtr2, num2, num3);
                this.AllocateEntities(archetype, chunkPtr2, num2, num3, outputEntities);
                outputEntities += num3;
                count -= num3;
            }
        }

        public unsafe int GetSharedComponentDataIndex(Entity entity, int typeIndex)
        {
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(this.GetArchetype(entity), typeIndex);
            return this.m_Entities.ChunkData[entity.Index].Chunk.SharedComponentValueArray[*(((IntPtr*) (this.m_Entities.Archetype + entity.Index))).SharedComponentOffset[indexInTypeArray]];
        }

        public unsafe void SetSharedComponentDataIndex(ArchetypeManager archetypeManager, SharedComponentDataManager sharedComponentDataManager, Entity entity, int typeIndex, int newSharedComponentDataIndex)
        {
            Archetype* archetype = ref this.GetArchetype(entity);
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            Chunk* chunk = ref this.GetComponentChunk(entity);
            int index = archetype->SharedComponentOffset[indexInTypeArray];
            int num3 = chunk->SharedComponentValueArray[index];
            if (newSharedComponentDataIndex != num3)
            {
                int* sharedComponentDataIndices = (int*) stackalloc byte[(((IntPtr) archetype->NumSharedComponents) * 4)];
                UnsafeUtility.MemCpy((void*) sharedComponentDataIndices, (void*) chunk->SharedComponentValueArray, (long) (archetype->NumSharedComponents * 4));
                sharedComponentDataIndices[index] = newSharedComponentDataIndex;
                Chunk* newChunk = ref archetypeManager.GetChunkWithEmptySlots(archetype, sharedComponentDataIndices);
                this.IncrementComponentOrderVersion(archetype, chunk, sharedComponentDataManager);
                this.MoveEntityToChunk(archetypeManager, entity, newChunk, archetypeManager.AllocateIntoChunk(newChunk));
            }
        }

        internal unsafe void IncrementComponentOrderVersion(Archetype* archetype, Chunk* chunk, SharedComponentDataManager sharedComponentDataManager)
        {
            int* sharedComponentValueArray = chunk.SharedComponentValueArray;
            int index = 0;
            while (true)
            {
                if (index >= archetype.NumSharedComponents)
                {
                    this.IncrementComponentTypeOrderVersion(archetype);
                    return;
                }
                sharedComponentDataManager.IncrementSharedComponentVersion(sharedComponentValueArray[index]);
                index++;
            }
        }

        internal unsafe void IncrementComponentTypeOrderVersion(Archetype* archetype)
        {
            for (int i = 0; i < archetype.TypesCount; i++)
            {
                int typeIndex = archetype.Types[i].TypeIndex;
                int* numPtr1 = this.m_ComponentTypeOrderVersion + typeIndex;
                numPtr1[0]++;
            }
        }

        public unsafe int GetComponentTypeOrderVersion(int typeIndex) => 
            this.m_ComponentTypeOrderVersion[typeIndex];
        [StructLayout(LayoutKind.Sequential)]
        private struct EntityChunkData
        {
            public unsafe Unity.Entities.Chunk* Chunk;
            public int IndexInChunk;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EntityData
        {
            public unsafe int* Version;
            public unsafe Unity.Entities.Archetype** Archetype;
            public unsafe EntityDataManager.EntityChunkData* ChunkData;
        }
    }
}

