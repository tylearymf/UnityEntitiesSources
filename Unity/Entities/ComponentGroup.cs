namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public class ComponentGroup : IDisposable
    {
        private readonly ComponentJobSafetyManager m_SafetyManager;
        private unsafe readonly EntityGroupData* m_GroupData;
        private unsafe readonly Unity.Entities.EntityDataManager* m_EntityDataManager;
        private ComponentGroupFilter m_Filter;
        internal string DisallowDisposing = null;
        internal IDisposable m_CachedState;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private unsafe readonly Unity.Entities.EntityDataManager* <EntityDataManager>k__BackingField;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Unity.Entities.ArchetypeManager <ArchetypeManager>k__BackingField;

        internal unsafe ComponentGroup(EntityGroupData* groupData, ComponentJobSafetyManager safetyManager, Unity.Entities.ArchetypeManager typeManager, Unity.Entities.EntityDataManager* entityDataManager)
        {
            this.m_GroupData = groupData;
            this.m_EntityDataManager = entityDataManager;
            this.m_Filter = new ComponentGroupFilter();
            this.m_SafetyManager = safetyManager;
            this.<ArchetypeManager>k__BackingField = typeManager;
            this.<EntityDataManager>k__BackingField = entityDataManager;
        }

        public unsafe void AddDependency(JobHandle job)
        {
            this.m_SafetyManager.AddDependency(this.m_GroupData.ReaderTypes, this.m_GroupData.ReaderTypesCount, this.m_GroupData.WriterTypes, this.m_GroupData.WriterTypesCount, job);
        }

        public int CalculateLength()
        {
            int num;
            ComponentChunkIterator iterator;
            this.GetComponentChunkIterator(out num, out iterator);
            return num;
        }

        internal unsafe int CalculateNumberOfChunksWithoutFiltering() => 
            ComponentChunkIterator.CalculateNumberOfChunksWithoutFiltering(this.m_GroupData.FirstMatchingArchetype);

        public unsafe bool CompareComponents(ComponentType[] componentTypes) => 
            EntityGroupManager.CompareComponents(componentTypes, this.m_GroupData);

        public unsafe bool CompareQuery(EntityArchetypeQuery[] query) => 
            EntityGroupManager.CompareQuery(query, this.m_GroupData);

        public unsafe void CompleteDependency()
        {
            this.m_SafetyManager.CompleteDependenciesNoChecks(this.m_GroupData.ReaderTypes, this.m_GroupData.ReaderTypesCount, this.m_GroupData.WriterTypes, this.m_GroupData.WriterTypesCount);
        }

        private unsafe int ComponentTypeIndex(int indexInComponentGroup) => 
            this.m_GroupData.RequiredComponents[indexInComponentGroup].TypeIndex;

        public unsafe NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(Allocator allocator)
        {
            JobHandle handle;
            NativeArray<ArchetypeChunk> array = ComponentChunkIterator.CreateArchetypeChunkArray(this.m_GroupData.FirstMatchingArchetype, allocator, out handle);
            handle.Complete();
            return array;
        }

        public unsafe NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(Allocator allocator, out JobHandle jobhandle) => 
            ComponentChunkIterator.CreateArchetypeChunkArray(this.m_GroupData.FirstMatchingArchetype, allocator, out jobhandle);

        public void Dispose()
        {
            if (this.DisallowDisposing != null)
            {
                throw new ArgumentException(this.DisallowDisposing);
            }
            if (this.m_CachedState != null)
            {
                this.m_CachedState.Dispose();
            }
            this.ResetFilter();
        }

        internal unsafe NativeList<ArchetypeChunk> GetAllMatchingChunks(Allocator allocator)
        {
            NativeList<ArchetypeChunk> list = new NativeList<ArchetypeChunk>(allocator);
            MatchingArchetypes* firstMatchingArchetype = this.m_GroupData.FirstMatchingArchetype;
            while (firstMatchingArchetype != null)
            {
                Archetype* archetype = firstMatchingArchetype->Archetype;
                Chunk* begin = (Chunk*) archetype->ChunkList.Begin;
                while (true)
                {
                    if (begin == archetype->ChunkList.End)
                    {
                        firstMatchingArchetype = firstMatchingArchetype->Next;
                        break;
                    }
                    if (begin.MatchesFilter(firstMatchingArchetype, ref this.m_Filter))
                    {
                        ArchetypeChunk element = new ArchetypeChunk {
                            m_Chunk = begin
                        };
                        list.Add(element);
                    }
                    begin = (Chunk*) begin->ChunkListNode.Next;
                }
            }
            return list;
        }

        internal Unity.Entities.ArchetypeManager GetArchetypeManager() => 
            this.ArchetypeManager;

        public BufferArray<T> GetBufferArray<T>() where T: struct, IBufferElementData
        {
            int num;
            ComponentChunkIterator iterator;
            BufferArray<T> array;
            this.GetComponentChunkIterator(out num, out iterator);
            this.GetBufferArray<T>(ref iterator, this.GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>()), num, out array);
            return array;
        }

        internal void GetBufferArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out BufferArray<T> output) where T: struct, IBufferElementData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;
            output = new BufferArray<T>(iterator, length, this.GetIsReadOnly(indexInComponentGroup), this.GetSafetyHandle(indexInComponentGroup), this.GetBufferSafetyHandle(indexInComponentGroup));
        }

        internal unsafe AtomicSafetyHandle GetBufferSafetyHandle(int indexInComponentGroup)
        {
            ComponentType* typePtr = this.m_GroupData.RequiredComponents + indexInComponentGroup;
            return this.m_SafetyManager.GetBufferSafetyHandle(typePtr->TypeIndex);
        }

        public unsafe int GetCombinedComponentOrderVersion()
        {
            int num = 0;
            for (int i = 0; i < this.m_GroupData.RequiredComponentsCount; i++)
            {
                num += this.m_EntityDataManager.GetComponentTypeOrderVersion(this.m_GroupData.RequiredComponents[i].TypeIndex);
            }
            return num;
        }

        internal unsafe void GetComponentChunkIterator(out ComponentChunkIterator outIterator)
        {
            outIterator = new ComponentChunkIterator(this.m_GroupData.FirstMatchingArchetype, this.m_EntityDataManager.GlobalSystemVersion, ref this.m_Filter);
        }

        internal unsafe void GetComponentChunkIterator(out int outLength, out ComponentChunkIterator outIterator)
        {
            outLength = ComponentChunkIterator.CalculateLength(this.m_GroupData.FirstMatchingArchetype, ref this.m_Filter);
            outIterator = new ComponentChunkIterator(this.m_GroupData.FirstMatchingArchetype, this.m_EntityDataManager.GlobalSystemVersion, ref this.m_Filter);
        }

        public ComponentDataArray<T> GetComponentDataArray<T>() where T: struct, IComponentData
        {
            int num2;
            ComponentChunkIterator iterator;
            ComponentDataArray<T> array;
            int typeIndex = TypeManager.GetTypeIndex<T>();
            if (ComponentType.FromTypeIndex(typeIndex).IsZeroSized)
            {
                throw new ArgumentException($"GetComponentDataArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
            this.GetComponentChunkIterator(out num2, out iterator);
            this.GetComponentDataArray<T>(ref iterator, this.GetIndexInComponentGroup(typeIndex), num2, out array);
            return array;
        }

        internal void GetComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out ComponentDataArray<T> output) where T: struct, IComponentData
        {
            if (ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<T>()).IsZeroSized)
            {
                throw new ArgumentException($"GetComponentDataArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
            iterator.IndexInComponentGroup = indexInComponentGroup;
            output = new ComponentDataArray<T>(iterator, length, this.GetSafetyHandle(indexInComponentGroup));
        }

        public unsafe JobHandle GetDependency() => 
            this.m_SafetyManager.GetDependency(this.m_GroupData.ReaderTypes, this.m_GroupData.ReaderTypesCount, this.m_GroupData.WriterTypes, this.m_GroupData.WriterTypesCount);

        public EntityArray GetEntityArray()
        {
            int num;
            ComponentChunkIterator iterator;
            EntityArray array;
            this.GetComponentChunkIterator(out num, out iterator);
            this.GetEntityArray(ref iterator, num, out array);
            return array;
        }

        internal void GetEntityArray(ref ComponentChunkIterator iterator, int length, out EntityArray output)
        {
            iterator.IndexInComponentGroup = 0;
            output = new EntityArray(iterator, length, this.m_SafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));
        }

        internal unsafe int GetIndexInComponentGroup(int componentType)
        {
            int index = 0;
            while (true)
            {
                if ((index >= this.m_GroupData.RequiredComponentsCount) || (this.m_GroupData.RequiredComponents[index].TypeIndex == componentType))
                {
                    if (index >= this.m_GroupData.RequiredComponentsCount)
                    {
                        throw new InvalidOperationException($"Trying to get iterator for {TypeManager.GetType(componentType)} but the required component type was not declared in the EntityGroup.");
                    }
                    return index;
                }
                index++;
            }
        }

        private unsafe bool GetIsReadOnly(int indexInComponentGroup) => 
            (this.m_GroupData.RequiredComponents[indexInComponentGroup].AccessModeType == ComponentType.AccessMode.ReadOnly);

        internal unsafe AtomicSafetyHandle GetSafetyHandle(int indexInComponentGroup)
        {
            ComponentType* typePtr = this.m_GroupData.RequiredComponents + indexInComponentGroup;
            bool isReadOnly = typePtr->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return this.m_SafetyManager.GetSafetyHandle(typePtr->TypeIndex, isReadOnly);
        }

        public SharedComponentDataArray<T> GetSharedComponentDataArray<T>() where T: struct, ISharedComponentData
        {
            int num;
            ComponentChunkIterator iterator;
            SharedComponentDataArray<T> array;
            this.GetComponentChunkIterator(out num, out iterator);
            this.GetSharedComponentDataArray<T>(ref iterator, this.GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>()), num, out array);
            return array;
        }

        internal void GetSharedComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length, out SharedComponentDataArray<T> output) where T: struct, ISharedComponentData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;
            int type = this.ComponentTypeIndex(indexInComponentGroup);
            output = new SharedComponentDataArray<T>(this.ArchetypeManager.GetSharedComponentDataManager(), indexInComponentGroup, iterator, length, this.m_SafetyManager.GetSafetyHandle(type, true));
        }

        public unsafe void ResetFilter()
        {
            if (this.m_Filter.Type == FilterType.SharedComponent)
            {
                int count = this.m_Filter.Shared.Count;
                SharedComponentDataManager sharedComponentDataManager = this.ArchetypeManager.GetSharedComponentDataManager();
                int* numPtr = &this.m_Filter.Shared.SharedComponentIndex.FixedElementField;
                int index = 0;
                while (true)
                {
                    if (index >= count)
                    {
                        fixed (int** numPtrRef = null)
                        {
                            break;
                        }
                    }
                    sharedComponentDataManager.RemoveReference(numPtr[index], 1);
                    index++;
                }
            }
            this.m_Filter.Type = FilterType.None;
        }

        private void SetFilter(ref ComponentGroupFilter filter)
        {
            filter.AssertValid();
            uint requiredChangeVersion = this.m_Filter.RequiredChangeVersion;
            this.ResetFilter();
            this.m_Filter = filter;
            this.m_Filter.RequiredChangeVersion = requiredChangeVersion;
        }

        public void SetFilter<SharedComponent1>(SharedComponent1 sharedComponent1) where SharedComponent1: struct, ISharedComponentData
        {
            SharedComponentDataManager sharedComponentDataManager = this.ArchetypeManager.GetSharedComponentDataManager();
            ComponentGroupFilter filter = new ComponentGroupFilter {
                Type = FilterType.SharedComponent,
                Shared = { 
                    Count = 1,
                    IndexInComponentGroup = { FixedElementField = this.GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>()) },
                    SharedComponentIndex = { FixedElementField = sharedComponentDataManager.InsertSharedComponent<SharedComponent1>(sharedComponent1) }
                }
            };
            this.SetFilter(ref filter);
        }

        public unsafe void SetFilter<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1, SharedComponent2 sharedComponent2) where SharedComponent1: struct, ISharedComponentData where SharedComponent2: struct, ISharedComponentData
        {
            SharedComponentDataManager sharedComponentDataManager = this.ArchetypeManager.GetSharedComponentDataManager();
            ComponentGroupFilter filter = new ComponentGroupFilter {
                Type = FilterType.SharedComponent,
                Shared = { 
                    Count = 2,
                    IndexInComponentGroup = { FixedElementField = this.GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>()) },
                    SharedComponentIndex = { FixedElementField = sharedComponentDataManager.InsertSharedComponent<SharedComponent1>(sharedComponent1) }
                }
            };
            &filter.Shared.IndexInComponentGroup.FixedElementField[1] = this.GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent2>());
            &filter.Shared.SharedComponentIndex.FixedElementField[1] = sharedComponentDataManager.InsertSharedComponent<SharedComponent2>(sharedComponent2);
            this.SetFilter(ref filter);
        }

        public void SetFilterChanged(ComponentType componentType)
        {
            ComponentGroupFilter filter = new ComponentGroupFilter {
                Type = FilterType.Changed,
                Changed = { 
                    Count = 1,
                    IndexInComponentGroup = { FixedElementField = this.GetIndexInComponentGroup(componentType.TypeIndex) }
                }
            };
            this.SetFilter(ref filter);
        }

        public unsafe void SetFilterChanged(ComponentType[] componentType)
        {
            if (componentType.Length > 2)
            {
                throw new ArgumentException($"ComponentGroup.SetFilterChanged accepts a maximum of {2} component array length");
            }
            if (componentType.Length == 0)
            {
                throw new ArgumentException("ComponentGroup.SetFilterChanged component array length must be larger than 0");
            }
            ComponentGroupFilter filter = new ComponentGroupFilter {
                Type = FilterType.Changed,
                Changed = { Count = componentType.Length }
            };
            int index = 0;
            while (true)
            {
                if (index == componentType.Length)
                {
                    this.SetFilter(ref filter);
                    return;
                }
                &filter.Changed.IndexInComponentGroup.FixedElementField[index] = this.GetIndexInComponentGroup(componentType[index].TypeIndex);
                index++;
            }
        }

        internal void SetFilterChangedRequiredVersion(uint requiredVersion)
        {
            this.m_Filter.RequiredChangeVersion = requiredVersion;
        }

        internal Unity.Entities.EntityDataManager* EntityDataManager =>
            this.<EntityDataManager>k__BackingField;

        public bool IsEmptyIgnoreFilter
        {
            get
            {
                MatchingArchetypes* firstMatchingArchetype = this.m_GroupData.FirstMatchingArchetype;
                while (true)
                {
                    bool flag2;
                    if (firstMatchingArchetype == null)
                    {
                        flag2 = true;
                    }
                    else
                    {
                        if (firstMatchingArchetype->Archetype.EntityCount <= 0)
                        {
                            firstMatchingArchetype = firstMatchingArchetype->Next;
                            continue;
                        }
                        flag2 = false;
                    }
                    return flag2;
                }
            }
        }

        public ComponentType[] Types
        {
            get
            {
                List<ComponentType> list = new List<ComponentType>();
                int index = 0;
                while (true)
                {
                    if (index >= this.m_GroupData.RequiredComponentsCount)
                    {
                        int num2 = 0;
                        while (true)
                        {
                            if (num2 >= this.m_GroupData.ReaderTypesCount)
                            {
                                int num3 = 0;
                                while (true)
                                {
                                    if (num3 >= this.m_GroupData.WriterTypesCount)
                                    {
                                        int num4 = 0;
                                        while (num4 < this.m_GroupData.ArchetypeQueryCount)
                                        {
                                            int num5 = 0;
                                            while (true)
                                            {
                                                if (num5 >= this.m_GroupData.ArchetypeQuery[num4].AnyCount)
                                                {
                                                    int num6 = 0;
                                                    while (true)
                                                    {
                                                        if (num6 >= this.m_GroupData.ArchetypeQuery[num4].AllCount)
                                                        {
                                                            int num7 = 0;
                                                            while (true)
                                                            {
                                                                if (num7 >= this.m_GroupData.ArchetypeQuery[num4].NoneCount)
                                                                {
                                                                    num4++;
                                                                    break;
                                                                }
                                                                list.Add(ComponentType.Subtractive(TypeManager.GetType(this.m_GroupData.ArchetypeQuery[num4].None[num7])));
                                                                num7++;
                                                            }
                                                            break;
                                                        }
                                                        list.Add(TypeManager.GetType(this.m_GroupData.ArchetypeQuery[num4].All[num6]));
                                                        num6++;
                                                    }
                                                    break;
                                                }
                                                list.Add(TypeManager.GetType(this.m_GroupData.ArchetypeQuery[num4].Any[num5]));
                                                num5++;
                                            }
                                        }
                                        return list.ToArray();
                                    }
                                    list.Add(TypeManager.GetType(this.m_GroupData.WriterTypes[num3]));
                                    num3++;
                                }
                            }
                            list.Add(ComponentType.ReadOnly(TypeManager.GetType(this.m_GroupData.ReaderTypes[num2])));
                            num2++;
                        }
                    }
                    list.Add(this.m_GroupData.RequiredComponents[index]);
                    index++;
                }
            }
        }

        internal Unity.Entities.ArchetypeManager ArchetypeManager =>
            this.<ArchetypeManager>k__BackingField;
    }
}

