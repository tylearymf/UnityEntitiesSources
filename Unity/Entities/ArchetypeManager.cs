namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Profiling;

    internal class ArchetypeManager : IDisposable
    {
        private unsafe readonly UnsafeLinkedListNode* m_EmptyChunkPool;
        private readonly SharedComponentDataManager m_SharedComponentManager;
        private ChunkAllocator m_ArchetypeChunkAllocator;
        internal unsafe Archetype* m_LastArchetype;
        private ManagedArrayStorage[] m_ManagedArrays = new ManagedArrayStorage[1];
        private NativeMultiHashMap<uint, IntPtr> m_TypeLookup;
        private unsafe Chunk* lastChunkWithSharedComponentsAllocatedInto;
        private unsafe Archetype* m_entityOnlyArchetype;

        public unsafe ArchetypeManager(SharedComponentDataManager sharedComponentManager)
        {
            this.m_SharedComponentManager = sharedComponentManager;
            this.m_TypeLookup = new NativeMultiHashMap<uint, IntPtr>(0x100, Allocator.Persistent);
            this.m_EmptyChunkPool = (UnsafeLinkedListNode*) this.m_ArchetypeChunkAllocator.Allocate(sizeof(UnsafeLinkedListNode), UnsafeUtility.AlignOf<UnsafeLinkedListNode>());
            UnsafeLinkedListNode.InitializeList(this.m_EmptyChunkPool);
            Unity.Assertions.Assert.IsTrue((UnsafeUtility.GetFieldOffset(typeof(Chunk).GetField("Buffer")) % 0x10) == 0, "Chunk buffer must be 16 byte aligned");
        }

        public unsafe void AddExistingChunk(Chunk* chunk)
        {
            Archetype* archetype = chunk.Archetype;
            archetype->ChunkList.Add(&chunk.ChunkListNode);
            int* numPtr1 = (int*) ref archetype->ChunkCount;
            numPtr1[0]++;
            int* numPtr2 = (int*) ref archetype->EntityCount;
            numPtr2[0] += chunk.Count;
            int index = 0;
            while (true)
            {
                if (index >= archetype->NumSharedComponents)
                {
                    if (chunk.Count < chunk.Capacity)
                    {
                        if (archetype->NumSharedComponents == 0)
                        {
                            archetype->ChunkListWithEmptySlots.Add(&chunk.ChunkListWithEmptySlotsNode);
                        }
                        else
                        {
                            archetype->FreeChunksBySharedComponents.Add(chunk);
                        }
                    }
                    return;
                }
                this.m_SharedComponentManager.AddReference(chunk.SharedComponentValueArray[index]);
                index++;
            }
        }

        public unsafe int AllocateIntoChunk(Chunk* chunk)
        {
            int num;
            Unity.Assertions.Assert.AreEqual(1, this.AllocateIntoChunk(chunk, 1, out num));
            return num;
        }

        public unsafe int AllocateIntoChunk(Chunk* chunk, int count, out int outIndex)
        {
            int num = Math.Min(chunk.Capacity - chunk.Count, count);
            outIndex = chunk.Count;
            this.SetChunkCount(chunk, chunk.Count + num);
            int* numPtr1 = (int*) ref chunk.Archetype.EntityCount;
            numPtr1[0] += num;
            return num;
        }

        private int AllocateManagedArrayStorage(int length)
        {
            int index = 0;
            while (true)
            {
                int num3;
                if (index >= this.m_ManagedArrays.Length)
                {
                    int num = this.m_ManagedArrays.Length;
                    Array.Resize<ManagedArrayStorage>(ref this.m_ManagedArrays, this.m_ManagedArrays.Length * 2);
                    this.m_ManagedArrays[num].ManagedArray = new object[length];
                    num3 = num;
                }
                else
                {
                    if (this.m_ManagedArrays[index].ManagedArray != null)
                    {
                        index++;
                        continue;
                    }
                    this.m_ManagedArrays[index].ManagedArray = new object[length];
                    num3 = index;
                }
                return num3;
            }
        }

        private unsafe bool ArchetypeSystemStateCleanupComplete(Archetype* archetype)
        {
            bool flag2;
            if ((archetype.TypesCount == 2) && (archetype.Types[1].TypeIndex == TypeManager.GetTypeIndex<CleanupEntity>()))
            {
                flag2 = true;
            }
            else
            {
                flag2 = false;
            }
            return flag2;
        }

        private unsafe bool ArchetypeSystemStateCleanupNeeded(Archetype* archetype)
        {
            int index = 1;
            while (true)
            {
                bool flag2;
                if (index >= archetype.TypesCount)
                {
                    flag2 = false;
                }
                else
                {
                    ComponentTypeInArchetype archetype2 = archetype.Types[index];
                    if (!(archetype2.IsSystemStateComponent || archetype2.IsSystemStateSharedComponent))
                    {
                        index++;
                        continue;
                    }
                    flag2 = true;
                }
                return flag2;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static unsafe void AssertArchetypeComponents(ComponentTypeInArchetype* types, int count)
        {
            if (count < 1)
            {
                throw new ArgumentException("Invalid component count");
            }
            if (types.TypeIndex == 0)
            {
                throw new ArgumentException("Component type may not be null");
            }
            if (types.TypeIndex != TypeManager.GetTypeIndex<Entity>())
            {
                throw new ArgumentException("The Entity ID must always be the first component");
            }
            for (int i = 1; i < count; i++)
            {
                if (types[i - 1].TypeIndex == types[i].TypeIndex)
                {
                    throw new ArgumentException($"It is not allowed to have two components of the same type on the same entity. ({types[i - 1]} and {types[i]})");
                }
            }
        }

        public unsafe int CheckInternalConsistency()
        {
            Archetype* lastArchetype = this.m_LastArchetype;
            int num = 0;
            while (lastArchetype != null)
            {
                int expected = 0;
                int num3 = 0;
                UnsafeLinkedListNode* begin = lastArchetype->ChunkList.Begin;
                while (true)
                {
                    if (begin == lastArchetype->ChunkList.End)
                    {
                        Unity.Assertions.Assert.AreEqual(expected, lastArchetype->EntityCount);
                        Unity.Assertions.Assert.AreEqual(num3, lastArchetype->ChunkCount);
                        num += expected;
                        lastArchetype = lastArchetype->PrevArchetype;
                        break;
                    }
                    Chunk* chunkPtr = (Chunk*) begin;
                    Unity.Assertions.Assert.IsTrue(chunkPtr->Archetype == lastArchetype);
                    Unity.Assertions.Assert.IsTrue(chunkPtr->Capacity >= chunkPtr->Count);
                    Unity.Assertions.Assert.AreEqual(chunkPtr->ChunkListWithEmptySlotsNode.IsInList, chunkPtr->Capacity != chunkPtr->Count);
                    expected += chunkPtr->Count;
                    num3++;
                    begin = begin->Next;
                }
            }
            return num;
        }

        private static unsafe bool ChunkHasSharedComponents(Chunk* chunk, int* sharedComponentDataIndices)
        {
            int* sharedComponentValueArray = chunk.SharedComponentValueArray;
            return (UnsafeUtility.MemCmp((void*) sharedComponentDataIndices, (void*) sharedComponentValueArray, (long) (chunk.Archetype.NumSharedComponents * 4)) == 0);
        }

        public unsafe void ConstructChunk(Archetype* archetype, Chunk* chunk, int* sharedComponentDataIndices)
        {
            chunk.Archetype = archetype;
            chunk.Count = 0;
            chunk.Capacity = archetype.ChunkCapacity;
            chunk.ChunkListNode = new UnsafeLinkedListNode();
            chunk.ChunkListWithEmptySlotsNode = new UnsafeLinkedListNode();
            int numSharedComponents = archetype.NumSharedComponents;
            int sharedComponentOffset = Chunk.GetSharedComponentOffset(numSharedComponents);
            int changedComponentOffset = Chunk.GetChangedComponentOffset(archetype.TypesCount, numSharedComponents);
            chunk.SharedComponentValueArray = (int*) (chunk + sharedComponentOffset);
            chunk.ChangeVersion = (uint*) (chunk + changedComponentOffset);
            archetype.ChunkList.Add(&chunk.ChunkListNode);
            int* numPtr1 = (int*) ref archetype.ChunkCount;
            numPtr1[0]++;
            Unity.Assertions.Assert.IsTrue(!archetype.ChunkList.IsEmpty);
            Unity.Assertions.Assert.IsTrue(chunk == archetype.ChunkList.Back);
            if (numSharedComponents == 0)
            {
                archetype.ChunkListWithEmptySlots.Add(&chunk.ChunkListWithEmptySlotsNode);
                Unity.Assertions.Assert.IsTrue(chunk == GetChunkFromEmptySlotNode(archetype.ChunkListWithEmptySlots.Back));
                Unity.Assertions.Assert.IsTrue(!archetype.ChunkListWithEmptySlots.IsEmpty);
            }
            else
            {
                int* sharedComponentValueArray = chunk.SharedComponentValueArray;
                UnsafeUtility.MemCpy((void*) sharedComponentValueArray, (void*) sharedComponentDataIndices, (long) (archetype.NumSharedComponents * 4));
                int index = 0;
                while (true)
                {
                    if (index >= archetype.NumSharedComponents)
                    {
                        archetype.FreeChunksBySharedComponents.Add(chunk);
                        Unity.Assertions.Assert.IsTrue(archetype.FreeChunksBySharedComponents.GetChunkWithEmptySlots(sharedComponentDataIndices, archetype.NumSharedComponents) != null);
                        break;
                    }
                    int num6 = sharedComponentValueArray[index];
                    this.m_SharedComponentManager.AddReference(num6);
                    index++;
                }
            }
            if (archetype.NumManagedArrays > 0)
            {
                chunk.ManagedArrayIndex = this.AllocateManagedArrayStorage(archetype.NumManagedArrays * chunk.Capacity);
            }
            else
            {
                chunk.ManagedArrayIndex = -1;
            }
            for (int i = 0; i < archetype.TypesCount; i++)
            {
                chunk.ChangeVersion[i] = 0;
            }
        }

        public unsafe int CountEntities()
        {
            int num = 0;
            for (Archetype* archetypePtr = this.m_LastArchetype; archetypePtr != null; archetypePtr = archetypePtr->PrevArchetype)
            {
                num += archetypePtr->EntityCount;
            }
            return num;
        }

        private unsafe Archetype* CreateArchetypeInternal(ComponentTypeInArchetype* types, int count, EntityGroupManager groupManager)
        {
            AssertArchetypeComponents(types, count);
            Archetype* archetype = (Archetype*) ref this.m_ArchetypeChunkAllocator.Allocate(sizeof(Archetype), 8);
            archetype->TypesCount = count;
            archetype->Types = (ComponentTypeInArchetype*) this.m_ArchetypeChunkAllocator.Construct(sizeof(ComponentTypeInArchetype) * count, 4, (void*) types);
            archetype->EntityCount = 0;
            archetype->ChunkCount = 0;
            archetype->NumSharedComponents = 0;
            archetype->SharedComponentOffset = null;
            int typeIndex = TypeManager.GetTypeIndex<Disabled>();
            int num2 = TypeManager.GetTypeIndex<Prefab>();
            archetype->Disabled = false;
            archetype->Prefab = false;
            int index = 0;
            while (true)
            {
                if (index >= count)
                {
                    int num3 = 0;
                    int num4 = 0;
                    int num9 = 0;
                    while (true)
                    {
                        if (num9 >= count)
                        {
                            int chunkBufferSize = Chunk.GetChunkBufferSize(archetype->TypesCount, archetype->NumSharedComponents);
                            archetype->Offsets = (int*) this.m_ArchetypeChunkAllocator.Allocate(4 * count, 4);
                            archetype->SizeOfs = (int*) this.m_ArchetypeChunkAllocator.Allocate(4 * count, 4);
                            archetype->TypeMemoryOrder = (int*) this.m_ArchetypeChunkAllocator.Allocate(4 * count, 4);
                            archetype->ScalarEntityPatches = (EntityRemapUtility.EntityPatchInfo*) this.m_ArchetypeChunkAllocator.Allocate(sizeof(EntityRemapUtility.EntityPatchInfo) * num3, 4);
                            archetype->ScalarEntityPatchCount = num3;
                            archetype->BufferEntityPatches = (EntityRemapUtility.BufferEntityPatchInfo*) this.m_ArchetypeChunkAllocator.Allocate(sizeof(EntityRemapUtility.BufferEntityPatchInfo) * num4, 4);
                            archetype->BufferEntityPatchCount = num4;
                            int num6 = 0;
                            int num10 = 0;
                            while (true)
                            {
                                if (num10 >= count)
                                {
                                    archetype->ChunkCapacity = chunkBufferSize / num6;
                                    if (num6 > chunkBufferSize)
                                    {
                                        throw new ArgumentException($"Entity archetype component data is too large. The maximum component data is {chunkBufferSize} but the component data is {num6}");
                                    }
                                    Unity.Assertions.Assert.IsTrue(0x7e0 >= archetype->ChunkCapacity);
                                    NativeArray<ulong> array = new NativeArray<ulong>(count, Allocator.Temp, NativeArrayOptions.ClearMemory);
                                    int num12 = 0;
                                    while (true)
                                    {
                                        if (num12 >= count)
                                        {
                                            int num13 = 0;
                                            while (true)
                                            {
                                                if (num13 >= count)
                                                {
                                                    array.Dispose();
                                                    int num7 = 0;
                                                    int num15 = 0;
                                                    while (true)
                                                    {
                                                        if (num15 >= count)
                                                        {
                                                            archetype->NumManagedArrays = 0;
                                                            archetype->ManagedArrayOffset = null;
                                                            int num18 = 0;
                                                            while (true)
                                                            {
                                                                if (num18 >= count)
                                                                {
                                                                    if (archetype->NumManagedArrays > 0)
                                                                    {
                                                                        archetype->ManagedArrayOffset = (int*) this.m_ArchetypeChunkAllocator.Allocate(4 * count, 4);
                                                                        int num19 = 0;
                                                                        int num20 = 0;
                                                                        while (true)
                                                                        {
                                                                            if (num20 >= count)
                                                                            {
                                                                                break;
                                                                            }
                                                                            int num21 = archetype->TypeMemoryOrder[num20];
                                                                            TypeManager.TypeInfo info3 = TypeManager.GetTypeInfo(types[num21].TypeIndex);
                                                                            if (info3.Category != TypeManager.TypeCategory.Class)
                                                                            {
                                                                                archetype->ManagedArrayOffset[num21] = -1;
                                                                            }
                                                                            else
                                                                            {
                                                                                num19++;
                                                                                archetype->ManagedArrayOffset[num21] = num19;
                                                                            }
                                                                            num20++;
                                                                        }
                                                                    }
                                                                    if (archetype->NumSharedComponents > 0)
                                                                    {
                                                                        archetype->SharedComponentOffset = (int*) this.m_ArchetypeChunkAllocator.Allocate(4 * count, 4);
                                                                        int num22 = 0;
                                                                        int num23 = 0;
                                                                        while (true)
                                                                        {
                                                                            if (num23 >= count)
                                                                            {
                                                                                break;
                                                                            }
                                                                            int num24 = archetype->TypeMemoryOrder[num23];
                                                                            TypeManager.TypeInfo info4 = TypeManager.GetTypeInfo(types[num24].TypeIndex);
                                                                            if (info4.Category != TypeManager.TypeCategory.ISharedComponentData)
                                                                            {
                                                                                archetype->SharedComponentOffset[num24] = -1;
                                                                            }
                                                                            else
                                                                            {
                                                                                num22++;
                                                                                archetype->SharedComponentOffset[num24] = num22;
                                                                            }
                                                                            num23++;
                                                                        }
                                                                    }
                                                                    EntityRemapUtility.EntityPatchInfo* scalarEntityPatches = archetype->ScalarEntityPatches;
                                                                    EntityRemapUtility.BufferEntityPatchInfo* bufferEntityPatches = archetype->BufferEntityPatches;
                                                                    int num25 = 0;
                                                                    while (true)
                                                                    {
                                                                        if (num25 == count)
                                                                        {
                                                                            archetype->ScalarEntityPatchCount = num3;
                                                                            archetype->BufferEntityPatchCount = num4;
                                                                            archetype->PrevArchetype = this.m_LastArchetype;
                                                                            this.m_LastArchetype = archetype;
                                                                            UnsafeLinkedListNode.InitializeList(&archetype->ChunkList);
                                                                            UnsafeLinkedListNode.InitializeList(&archetype->ChunkListWithEmptySlots);
                                                                            archetype->FreeChunksBySharedComponents.Init(8);
                                                                            this.m_TypeLookup.Add(GetHash(types, count), (IntPtr) archetype);
                                                                            archetype->SystemStateCleanupComplete = this.ArchetypeSystemStateCleanupComplete(archetype);
                                                                            archetype->SystemStateCleanupNeeded = this.ArchetypeSystemStateCleanupNeeded(archetype);
                                                                            groupManager.AddArchetypeIfMatching(archetype);
                                                                            return archetype;
                                                                        }
                                                                        TypeManager.TypeInfo info5 = TypeManager.GetTypeInfo(types[num25].TypeIndex);
                                                                        TypeManager.EntityOffsetInfo[] offsets = info5.EntityOffsets;
                                                                        if (info5.BufferCapacity >= 0)
                                                                        {
                                                                            bufferEntityPatches = EntityRemapUtility.AppendBufferEntityPatches(bufferEntityPatches, offsets, archetype->Offsets[num25], archetype->SizeOfs[num25], info5.ElementSize);
                                                                        }
                                                                        else
                                                                        {
                                                                            scalarEntityPatches = EntityRemapUtility.AppendEntityPatches(scalarEntityPatches, offsets, archetype->Offsets[num25], archetype->SizeOfs[num25]);
                                                                        }
                                                                        num25++;
                                                                    }
                                                                }
                                                                if (TypeManager.GetTypeInfo(types[num18].TypeIndex).Category == TypeManager.TypeCategory.Class)
                                                                {
                                                                    int* numPtr1 = (int*) ref archetype->NumManagedArrays;
                                                                    numPtr1[0]++;
                                                                }
                                                                num18++;
                                                            }
                                                        }
                                                        int num16 = archetype->TypeMemoryOrder[num15];
                                                        int num17 = archetype->SizeOfs[num16];
                                                        archetype->Offsets[num16] = num7;
                                                        num7 += num17 * archetype->ChunkCapacity;
                                                        num15++;
                                                    }
                                                }
                                                int num14 = num13;
                                                while (true)
                                                {
                                                    if ((num14 <= 1) || (array[num13] >= array[archetype->TypeMemoryOrder[num14 - 1]]))
                                                    {
                                                        archetype->TypeMemoryOrder[num14] = num13;
                                                        num13++;
                                                        break;
                                                    }
                                                    archetype->TypeMemoryOrder[num14] = archetype->TypeMemoryOrder[num14 - 1];
                                                    num14--;
                                                }
                                            }
                                        }
                                        array.set_Item(num12, TypeManager.GetTypeInfo(types[num12].TypeIndex).MemoryOrdering);
                                        num12++;
                                    }
                                }
                                TypeManager.TypeInfo info2 = TypeManager.GetTypeInfo(types[num10].TypeIndex);
                                int sizeInChunk = info2.SizeInChunk;
                                archetype->SizeOfs[num10] = sizeInChunk;
                                num6 += sizeInChunk;
                                num10++;
                            }
                        }
                        TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo(types[num9].TypeIndex);
                        TypeManager.EntityOffsetInfo[] entityOffsets = typeInfo.EntityOffsets;
                        if (entityOffsets != null)
                        {
                            if (typeInfo.BufferCapacity >= 0)
                            {
                                num4 += entityOffsets.Length;
                            }
                            else
                            {
                                num3 += entityOffsets.Length;
                            }
                        }
                        num9++;
                    }
                }
                if (TypeManager.GetTypeInfo(types[index].TypeIndex).Category == TypeManager.TypeCategory.ISharedComponentData)
                {
                    int* numPtr2 = (int*) ref archetype->NumSharedComponents;
                    numPtr2[0]++;
                }
                if (types[index].TypeIndex == typeIndex)
                {
                    archetype->Disabled = true;
                }
                if (types[index].TypeIndex == num2)
                {
                    archetype->Prefab = true;
                }
                index++;
            }
        }

        private void DeallocateManagedArrayStorage(int index)
        {
            Unity.Assertions.Assert.IsTrue(this.m_ManagedArrays[index].ManagedArray != null);
            this.m_ManagedArrays[index].ManagedArray = null;
        }

        public unsafe void Dispose()
        {
            while (true)
            {
                if (this.m_LastArchetype == null)
                {
                    while (true)
                    {
                        if (this.m_EmptyChunkPool.IsEmpty)
                        {
                            this.m_ManagedArrays = null;
                            this.m_TypeLookup.Dispose();
                            this.m_ArchetypeChunkAllocator.Dispose();
                            return;
                        }
                        UnsafeLinkedListNode* begin = this.m_EmptyChunkPool.Begin;
                        begin.Remove();
                        UnsafeUtility.Free((void*) begin, Allocator.Persistent);
                    }
                }
                while (true)
                {
                    if (this.m_LastArchetype.ChunkList.IsEmpty)
                    {
                        this.m_LastArchetype.FreeChunksBySharedComponents.Dispose();
                        this.m_LastArchetype = this.m_LastArchetype.PrevArchetype;
                        break;
                    }
                    Chunk* begin = (Chunk*) this.m_LastArchetype.ChunkList.Begin;
                    this.SetChunkCount(begin, 0);
                }
            }
        }

        public static unsafe Chunk* GetChunkFromEmptySlotNode(UnsafeLinkedListNode* node) => 
            ((Chunk*) (node - 1));

        public unsafe Chunk* GetChunkWithEmptySlots(Archetype* archetype, int* sharedComponentDataIndices)
        {
            Chunk* begin;
            if (archetype.NumSharedComponents != 0)
            {
                Chunk* chunkWithEmptySlots = archetype.FreeChunksBySharedComponents.GetChunkWithEmptySlots(sharedComponentDataIndices, archetype.NumSharedComponents);
                if (chunkWithEmptySlots != null)
                {
                    return chunkWithEmptySlots;
                }
            }
            else if (!archetype.ChunkListWithEmptySlots.IsEmpty)
            {
                Chunk* chunkPtr2 = ref GetChunkFromEmptySlotNode(archetype.ChunkListWithEmptySlots.Begin);
                Unity.Assertions.Assert.AreNotEqual(chunkPtr2->Count, chunkPtr2->Capacity);
                return chunkPtr2;
            }
            if (!archetype.ChunkListWithEmptySlots.IsEmpty)
            {
                int num1;
                if ((this.lastChunkWithSharedComponentsAllocatedInto == null) || (this.lastChunkWithSharedComponentsAllocatedInto.Archetype != archetype))
                {
                    num1 = 0;
                }
                else
                {
                    num1 = (int) (this.lastChunkWithSharedComponentsAllocatedInto.Count < this.lastChunkWithSharedComponentsAllocatedInto.Capacity);
                }
                if ((num1 != 0) && ChunkHasSharedComponents(this.lastChunkWithSharedComponentsAllocatedInto, sharedComponentDataIndices))
                {
                    return this.lastChunkWithSharedComponentsAllocatedInto;
                }
                if (archetype.NumSharedComponents == 0)
                {
                    Chunk* chunkPtr5 = ref GetChunkFromEmptySlotNode(archetype.ChunkListWithEmptySlots.Begin);
                    Unity.Assertions.Assert.AreNotEqual(chunkPtr5->Count, chunkPtr5->Capacity);
                    return chunkPtr5;
                }
            }
            if (this.m_EmptyChunkPool.IsEmpty)
            {
                begin = (Chunk*) UnsafeUtility.Malloc(0x3f00L, 0x40, Allocator.Persistent);
            }
            else
            {
                begin = (Chunk*) this.m_EmptyChunkPool.Begin;
                begin->ChunkListNode.Remove();
            }
            this.ConstructChunk(archetype, begin, sharedComponentDataIndices);
            if (archetype.NumSharedComponents > 0)
            {
                this.lastChunkWithSharedComponentsAllocatedInto = begin;
            }
            return begin;
        }

        private unsafe Archetype* GetEntityOnlyArchetype(ComponentTypeInArchetype* types, EntityGroupManager groupManager)
        {
            if (this.m_entityOnlyArchetype == null)
            {
                this.m_entityOnlyArchetype = this.GetOrCreateArchetypeInternal(types, 1, groupManager);
                this.m_entityOnlyArchetype.InstantiableArchetype = this.m_entityOnlyArchetype;
                this.m_entityOnlyArchetype.SystemStateResidueArchetype = null;
            }
            return this.m_entityOnlyArchetype;
        }

        public unsafe Archetype* GetExistingArchetype(ComponentTypeInArchetype* types, int count)
        {
            IntPtr ptr;
            NativeMultiHashMapIterator<uint> iterator;
            Archetype* archetypePtr;
            if (!this.m_TypeLookup.TryGetFirstValue(GetHash(types, count), out ptr, out iterator))
            {
                archetypePtr = null;
            }
            else
            {
                while (true)
                {
                    Archetype* archetypePtr2 = (Archetype*) ptr;
                    if (ComponentTypeInArchetype.CompareArray(archetypePtr2->Types, archetypePtr2->TypesCount, types, count))
                    {
                        archetypePtr = archetypePtr2;
                    }
                    else
                    {
                        if (this.m_TypeLookup.TryGetNextValue(out ptr, ref iterator))
                        {
                            continue;
                        }
                        archetypePtr = null;
                    }
                    break;
                }
            }
            return archetypePtr;
        }

        private static unsafe uint GetHash(ComponentTypeInArchetype* types, int count) => 
            HashUtility.Fletcher32((ushort*) types, (count * sizeof(ComponentTypeInArchetype)) / 2);

        internal unsafe object GetManagedObject(Chunk* chunk, int type, int index)
        {
            int num = chunk.Archetype.ManagedArrayOffset[type] * chunk.Capacity;
            return this.m_ManagedArrays[chunk.ManagedArrayIndex].ManagedArray[index + num];
        }

        public unsafe object GetManagedObject(Chunk* chunk, ComponentType type, int index)
        {
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype, type.TypeIndex);
            if ((indexInTypeArray < 0) || (chunk.Archetype.ManagedArrayOffset[indexInTypeArray] < 0))
            {
                throw new InvalidOperationException("Trying to get managed object for non existing component");
            }
            return this.GetManagedObject(chunk, indexInTypeArray, index);
        }

        public unsafe object[] GetManagedObjectRange(Chunk* chunk, int type, out int rangeStart, out int rangeLength)
        {
            rangeStart = chunk.Archetype.ManagedArrayOffset[type] * chunk.Capacity;
            rangeLength = chunk.Count;
            return this.m_ManagedArrays[chunk.ManagedArrayIndex].ManagedArray;
        }

        public unsafe Archetype* GetOrCreateArchetype(ComponentTypeInArchetype* types, int count, EntityGroupManager groupManager)
        {
            Archetype* archetypePtr3;
            Archetype* existingArchetype = this.GetExistingArchetype(types, count);
            if (existingArchetype != null)
            {
                archetypePtr3 = existingArchetype;
            }
            else
            {
                existingArchetype = this.CreateArchetypeInternal(types, count, groupManager);
                int num = 0;
                int typeIndex = TypeManager.GetTypeIndex<Prefab>();
                int index = 0;
                while (true)
                {
                    int num1;
                    if (index >= existingArchetype->TypesCount)
                    {
                        existingArchetype->InstantiableArchetype = existingArchetype;
                        if (num > 0)
                        {
                            Archetype* archetypePtr4 = ref this.GetOrCreateArchetypeInternal(types, count - num, groupManager);
                            existingArchetype->InstantiableArchetype = archetypePtr4;
                            archetypePtr4->InstantiableArchetype = archetypePtr4;
                            archetypePtr4->SystemStateResidueArchetype = null;
                        }
                        if (!existingArchetype->SystemStateCleanupNeeded)
                        {
                            archetypePtr3 = existingArchetype;
                        }
                        else
                        {
                            ComponentTypeInArchetype archetype = new ComponentTypeInArchetype(ComponentType.Create<CleanupEntity>());
                            bool flag = false;
                            int num3 = 1;
                            int num5 = 1;
                            while (true)
                            {
                                if (num5 >= existingArchetype->TypesCount)
                                {
                                    if (!flag)
                                    {
                                        num3++;
                                        types[num3] = archetype;
                                    }
                                    Archetype* archetypePtr2 = ref this.GetOrCreateArchetype(types, num3, groupManager);
                                    archetypePtr2->SystemStateResidueArchetype = archetypePtr2;
                                    archetypePtr2->InstantiableArchetype = this.GetEntityOnlyArchetype(types, groupManager);
                                    existingArchetype->SystemStateResidueArchetype = archetypePtr2;
                                    archetypePtr3 = existingArchetype;
                                    break;
                                }
                                ComponentTypeInArchetype archetype3 = existingArchetype->Types[num5];
                                if (archetype3.IsSystemStateComponent || archetype3.IsSystemStateSharedComponent)
                                {
                                    if (!flag && (archetype < existingArchetype->Types[num5]))
                                    {
                                        num3++;
                                        types[num3] = archetype;
                                        flag = true;
                                    }
                                    num3++;
                                    types[num3] = existingArchetype->Types[num5];
                                }
                                num5++;
                            }
                        }
                        break;
                    }
                    ComponentTypeInArchetype archetype2 = existingArchetype->Types[index];
                    if (archetype2.IsSystemStateComponent || archetype2.IsSystemStateSharedComponent)
                    {
                        num1 = 1;
                    }
                    else
                    {
                        num1 = (int) (archetype2.TypeIndex == typeIndex);
                    }
                    if (num1 != 0)
                    {
                        num++;
                    }
                    else
                    {
                        types[index - num] = existingArchetype->Types[index];
                    }
                    index++;
                }
            }
            return archetypePtr3;
        }

        private unsafe Archetype* GetOrCreateArchetypeInternal(ComponentTypeInArchetype* types, int count, EntityGroupManager groupManager)
        {
            Archetype* existingArchetype = this.GetExistingArchetype(types, count);
            return ((existingArchetype != null) ? existingArchetype : this.CreateArchetypeInternal(types, count, groupManager));
        }

        internal SharedComponentDataManager GetSharedComponentDataManager() => 
            this.m_SharedComponentManager;

        public static unsafe void MoveChunks(EntityManager srcEntities, ArchetypeManager dstArchetypeManager, EntityGroupManager dstGroupManager, EntityDataManager* dstEntityDataManager, SharedComponentDataManager dstSharedComponents, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping = new NativeArray<EntityRemapUtility.EntityRemapInfo>(srcEntities.Entities.Capacity, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            MoveChunks(srcEntities, dstArchetypeManager, dstGroupManager, dstEntityDataManager, dstSharedComponents, componentTypeInArchetypeArray, entityRemapping);
            entityRemapping.Dispose();
        }

        public static unsafe void MoveChunks(EntityManager srcEntities, ArchetypeManager dstArchetypeManager, EntityGroupManager dstGroupManager, EntityDataManager* dstEntityDataManager, SharedComponentDataManager dstSharedComponents, ComponentTypeInArchetype* componentTypeInArchetypeArray, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            ArchetypeManager archetypeManager = srcEntities.ArchetypeManager;
            SharedComponentDataManager sharedComponentManager = srcEntities.m_SharedComponentManager;
            MoveAllChunksJob jobData = new MoveAllChunksJob {
                srcEntityDataManager = srcEntities.Entities,
                dstEntityDataManager = dstEntityDataManager,
                entityRemapping = entityRemapping
            };
            JobHandle dependsOn = new JobHandle();
            JobHandle handle = jobData.Schedule<MoveAllChunksJob>(dependsOn);
            JobHandle.ScheduleBatchedJobs();
            CustomSampler sampler = CustomSampler.Create("MoveAllSharedComponents");
            sampler.Begin();
            NativeArray<int> array = dstSharedComponents.MoveAllSharedComponents(sharedComponentManager, Allocator.TempJob);
            sampler.End();
            int length = 0;
            int num2 = 0;
            Archetype* lastArchetype = archetypeManager.m_LastArchetype;
            while (true)
            {
                if (lastArchetype == null)
                {
                    NativeArray<RemapChunk> array2 = new NativeArray<RemapChunk>(length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                    NativeArray<RemapArchetype> array3 = new NativeArray<RemapArchetype>(num2, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                    int index = 0;
                    int arrayLength = 0;
                    lastArchetype = archetypeManager.m_LastArchetype;
                    while (true)
                    {
                        if (lastArchetype == null)
                        {
                            RemapChunksJob job2 = new RemapChunksJob {
                                dstEntityDataManager = dstEntityDataManager,
                                remapChunks = array2,
                                remapShared = array,
                                entityRemapping = entityRemapping
                            };
                            RemapArchetypesJob job3 = new RemapArchetypesJob {
                                remapArchetypes = array3
                            };
                            job3.Schedule<RemapArchetypesJob>(arrayLength, 1, job2.Schedule<RemapChunksJob>(array2.Length, 1, handle)).Complete();
                            array.Dispose();
                            return;
                        }
                        if (lastArchetype->ChunkCount != 0)
                        {
                            if (lastArchetype->NumManagedArrays != 0)
                            {
                                throw new ArgumentException("MoveEntitiesFrom is not supported with managed arrays");
                            }
                            UnsafeUtility.MemCpy((void*) componentTypeInArchetypeArray, (void*) lastArchetype->Types, (long) (UnsafeUtility.SizeOf<ComponentTypeInArchetype>() * lastArchetype->TypesCount));
                            Archetype* archetypePtr2 = ref dstArchetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, lastArchetype->TypesCount, dstGroupManager);
                            RemapArchetype archetype = new RemapArchetype {
                                srcArchetype = lastArchetype,
                                dstArchetype = archetypePtr2
                            };
                            array3.set_Item(arrayLength, archetype);
                            UnsafeLinkedListNode* begin = lastArchetype->ChunkList.Begin;
                            while (true)
                            {
                                if (begin == lastArchetype->ChunkList.End)
                                {
                                    arrayLength++;
                                    dstEntityDataManager.IncrementComponentTypeOrderVersion(archetypePtr2);
                                    break;
                                }
                                RemapChunk chunk = new RemapChunk {
                                    chunk = (Chunk*) begin,
                                    dstArchetype = archetypePtr2
                                };
                                array2.set_Item(index, chunk);
                                index++;
                                begin = begin->Next;
                            }
                        }
                        lastArchetype = lastArchetype->PrevArchetype;
                    }
                }
                num2++;
                length += lastArchetype->ChunkCount;
                lastArchetype = lastArchetype->PrevArchetype;
            }
        }

        public static unsafe void MoveChunks(EntityManager srcEntities, NativeArray<ArchetypeChunk> chunks, ArchetypeManager dstArchetypeManager, EntityGroupManager dstGroupManager, EntityDataManager* dstEntityDataManager, SharedComponentDataManager dstSharedComponents, ComponentTypeInArchetype* componentTypeInArchetypeArray, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            ArchetypeManager archetypeManager = srcEntities.ArchetypeManager;
            SharedComponentDataManager sharedComponentManager = srcEntities.m_SharedComponentManager;
            MoveChunksJob jobData = new MoveChunksJob {
                srcEntityDataManager = srcEntities.Entities,
                dstEntityDataManager = dstEntityDataManager,
                entityRemapping = entityRemapping,
                chunks = chunks
            };
            JobHandle dependsOn = new JobHandle();
            JobHandle handle = jobData.Schedule<MoveChunksJob>(dependsOn);
            JobHandle.ScheduleBatchedJobs();
            CustomSampler sampler = CustomSampler.Create("MoveSharedComponents");
            sampler.Begin();
            NativeArray<int> array = dstSharedComponents.MoveSharedComponents(sharedComponentManager, chunks, Allocator.TempJob);
            sampler.End();
            int length = chunks.Length;
            NativeArray<RemapChunk> array2 = new NativeArray<RemapChunk>(length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            int index = 0;
            while (true)
            {
                if (index >= length)
                {
                    JobHandle handle2 = new RemoveChunksFromArchetypeJob { chunks = chunks }.Schedule<RemoveChunksFromArchetypeJob>(handle);
                    RemapChunksJob job3 = new RemapChunksJob {
                        dstEntityDataManager = dstEntityDataManager,
                        remapChunks = array2,
                        remapShared = array,
                        entityRemapping = entityRemapping
                    };
                    AddChunksToArchetypeJob job4 = new AddChunksToArchetypeJob {
                        chunks = chunks
                    };
                    job4.Schedule<AddChunksToArchetypeJob>(job3.Schedule<RemapChunksJob>(array2.Length, 1, handle2)).Complete();
                    array.Dispose();
                    return;
                }
                Chunk* chunkPtr = chunks[index].m_Chunk;
                Archetype* archetype = chunkPtr->Archetype;
                UnsafeUtility.MemCpy((void*) componentTypeInArchetypeArray, (void*) archetype->Types, (long) (UnsafeUtility.SizeOf<ComponentTypeInArchetype>() * archetype->TypesCount));
                Archetype* archetypePtr2 = ref dstArchetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, archetype->TypesCount, dstGroupManager);
                RemapChunk chunk = new RemapChunk {
                    chunk = chunkPtr,
                    dstArchetype = archetypePtr2
                };
                array2.set_Item(index, chunk);
                index++;
            }
        }

        public unsafe void SetChunkCount(Chunk* chunk, int newCount)
        {
            Unity.Assertions.Assert.AreNotEqual(newCount, chunk.Count);
            int capacity = chunk.Capacity;
            if (newCount != 0)
            {
                if (newCount == capacity)
                {
                    chunk.ChunkListWithEmptySlotsNode.Remove();
                }
                else if (chunk.Count == capacity)
                {
                    Unity.Assertions.Assert.IsTrue(newCount < chunk.Count);
                    if (chunk.Archetype.NumSharedComponents == 0)
                    {
                        chunk.Archetype.ChunkListWithEmptySlots.Add(&chunk.ChunkListWithEmptySlotsNode);
                    }
                    else
                    {
                        chunk.Archetype.FreeChunksBySharedComponents.Add(chunk);
                    }
                }
            }
            else
            {
                if (chunk.Archetype.NumSharedComponents > 0)
                {
                    int* sharedComponentValueArray = chunk.SharedComponentValueArray;
                    int index = 0;
                    while (true)
                    {
                        if (index >= chunk.Archetype.NumSharedComponents)
                        {
                            break;
                        }
                        this.m_SharedComponentManager.RemoveReference(sharedComponentValueArray[index], 1);
                        index++;
                    }
                }
                if (chunk.ManagedArrayIndex != -1)
                {
                    this.DeallocateManagedArrayStorage(chunk.ManagedArrayIndex);
                    chunk.ManagedArrayIndex = -1;
                }
                int* numPtr1 = (int*) ref chunk.Archetype.ChunkCount;
                numPtr1[0]--;
                chunk.Archetype = null;
                chunk.ChunkListNode.Remove();
                chunk.ChunkListWithEmptySlotsNode.Remove();
                this.m_EmptyChunkPool.Add(&chunk.ChunkListNode);
            }
            chunk.Count = newCount;
        }

        public unsafe void SetManagedObject(Chunk* chunk, int type, int index, object val)
        {
            int num = chunk.Archetype.ManagedArrayOffset[type] * chunk.Capacity;
            this.m_ManagedArrays[chunk.ManagedArrayIndex].ManagedArray[index + num] = val;
        }

        public unsafe void SetManagedObject(Chunk* chunk, ComponentType type, int index, object val)
        {
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype, type.TypeIndex);
            if ((indexInTypeArray < 0) || (chunk.Archetype.ManagedArrayOffset[indexInTypeArray] < 0))
            {
                throw new InvalidOperationException("Trying to set managed object for non existing component");
            }
            this.SetManagedObject(chunk, indexInTypeArray, index, val);
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct AddChunksToArchetypeJob : IJob
        {
            [ReadOnly]
            public NativeArray<ArchetypeChunk> chunks;
            public unsafe void Execute()
            {
                int length = this.chunks.Length;
                for (int i = 0; i < length; i++)
                {
                    Chunk* chunk = this.chunks[i].m_Chunk;
                    Archetype* archetype = chunk->Archetype;
                    archetype->ChunkList.Add(&chunk->ChunkListNode);
                    int* numPtr1 = (int*) ref archetype->ChunkCount;
                    numPtr1[0]++;
                    int* numPtr2 = (int*) ref archetype->EntityCount;
                    numPtr2[0] += chunk->Count;
                    if (chunk->Count < chunk->Capacity)
                    {
                        if (archetype->NumSharedComponents == 0)
                        {
                            archetype->ChunkListWithEmptySlots.Add(&chunk->ChunkListWithEmptySlotsNode);
                        }
                        else
                        {
                            archetype->FreeChunksBySharedComponents.Add(chunk);
                        }
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ManagedArrayStorage
        {
            public object[] ManagedArray;
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct MoveAllChunksJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public unsafe EntityDataManager* srcEntityDataManager;
            [NativeDisableUnsafePtrRestriction]
            public unsafe EntityDataManager* dstEntityDataManager;
            public NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping;
            public unsafe void Execute()
            {
                this.dstEntityDataManager.AllocateEntitiesForRemapping(this.srcEntityDataManager, ref this.entityRemapping);
                this.srcEntityDataManager.FreeAllEntities();
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct MoveChunksJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public unsafe EntityDataManager* srcEntityDataManager;
            [NativeDisableUnsafePtrRestriction]
            public unsafe EntityDataManager* dstEntityDataManager;
            public NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping;
            [ReadOnly]
            public NativeArray<ArchetypeChunk> chunks;
            public unsafe void Execute()
            {
                int length = this.chunks.Length;
                for (int i = 0; i < length; i++)
                {
                    Chunk* chunk = this.chunks[i].m_Chunk;
                    this.dstEntityDataManager.AllocateEntitiesForRemapping(this.srcEntityDataManager, chunk, ref this.entityRemapping);
                    this.srcEntityDataManager.FreeEntities(chunk);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RemapArchetype
        {
            public unsafe Archetype* srcArchetype;
            public unsafe Archetype* dstArchetype;
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct RemapArchetypesJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<ArchetypeManager.RemapArchetype> remapArchetypes;
            public unsafe void Execute(int index)
            {
                Archetype* srcArchetype = this.remapArchetypes[index].srcArchetype;
                Archetype* dstArchetype = this.remapArchetypes[index].dstArchetype;
                UnsafeLinkedListNode.InsertListBefore(dstArchetype->ChunkList.End, &srcArchetype->ChunkList);
                if (srcArchetype->NumSharedComponents != 0)
                {
                    this.remapArchetypes[index].dstArchetype.FreeChunksBySharedComponents.AppendFrom(&this.remapArchetypes[index].srcArchetype.FreeChunksBySharedComponents);
                }
                else if (!srcArchetype->ChunkListWithEmptySlots.IsEmpty)
                {
                    UnsafeLinkedListNode.InsertListBefore(dstArchetype->ChunkListWithEmptySlots.End, &srcArchetype->ChunkListWithEmptySlots);
                }
                int* numPtr1 = (int*) ref dstArchetype->EntityCount;
                numPtr1[0] += srcArchetype->EntityCount;
                int* numPtr2 = (int*) ref dstArchetype->ChunkCount;
                numPtr2[0] += srcArchetype->ChunkCount;
                srcArchetype->EntityCount = 0;
                srcArchetype->ChunkCount = 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RemapChunk
        {
            public unsafe Chunk* chunk;
            public unsafe Archetype* dstArchetype;
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct RemapChunksJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping;
            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<ArchetypeManager.RemapChunk> remapChunks;
            [ReadOnly]
            public NativeArray<int> remapShared;
            [NativeDisableUnsafePtrRestriction]
            public unsafe EntityDataManager* dstEntityDataManager;
            public unsafe void Execute(int index)
            {
                Chunk* chunk = this.remapChunks[index].chunk;
                Archetype* dstArchetype = this.remapChunks[index].dstArchetype;
                this.dstEntityDataManager.RemapChunk(dstArchetype, chunk, 0, chunk->Count, ref this.entityRemapping);
                EntityRemapUtility.PatchEntities(dstArchetype->ScalarEntityPatches + 1, dstArchetype->ScalarEntityPatchCount - 1, dstArchetype->BufferEntityPatches, dstArchetype->BufferEntityPatchCount, &chunk->Buffer.FixedElementField, chunk->Count, ref this.entityRemapping);
                chunk->Archetype = dstArchetype;
                for (int i = 0; i < dstArchetype->NumSharedComponents; i++)
                {
                    int num2 = chunk->SharedComponentValueArray[i];
                    chunk->SharedComponentValueArray[i] = this.remapShared[num2];
                }
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct RemoveChunksFromArchetypeJob : IJob
        {
            [ReadOnly]
            public NativeArray<ArchetypeChunk> chunks;
            public unsafe void Execute()
            {
                int length = this.chunks.Length;
                for (int i = 0; i < length; i++)
                {
                    Chunk* chunk = this.chunks[i].m_Chunk;
                    Archetype* archetype = chunk->Archetype;
                    chunk->ChunkListNode.Remove();
                    chunk->ChunkListWithEmptySlotsNode.Remove();
                    int* numPtr1 = (int*) ref archetype->ChunkCount;
                    numPtr1[0]--;
                    int* numPtr2 = (int*) ref archetype->EntityCount;
                    numPtr2[0] -= chunk->Count;
                }
            }
        }
    }
}

