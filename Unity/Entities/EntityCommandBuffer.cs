namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Profiling;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct EntityCommandBuffer : IDisposable
    {
        private const int kDefaultMinimumChunkSize = 0x1000;
        [NativeDisableUnsafePtrRestriction]
        private unsafe EntityCommandBufferData* m_Data;
        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_BufferSafety;
        private AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;
        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
        internal int SystemID;
        private const int MainThreadJobIndex = 0x7fffffff;
        public int MinimumChunkSize
        {
            get => 
                ((this.m_Data.m_MinimumChunkSize > 0) ? this.m_Data.m_MinimumChunkSize : 0x1000);
            set => 
                (this.m_Data.m_MinimumChunkSize = Math.Max(0, value));
        }
        public bool ShouldPlayback
        {
            get => 
                ((this.m_Data != null) ? this.m_Data.m_ShouldPlayback : false);
            set
            {
                if (this.m_Data != null)
                {
                    this.m_Data.m_ShouldPlayback = value;
                }
            }
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void EnforceSingleThreadOwnership()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
        }

        public unsafe EntityCommandBuffer(Allocator label)
        {
            this.m_Data = (EntityCommandBufferData*) UnsafeUtility.Malloc((long) sizeof(EntityCommandBufferData), UnsafeUtility.AlignOf<EntityCommandBufferData>(), label);
            this.m_Data.m_Allocator = label;
            this.m_Data.m_MinimumChunkSize = 0x1000;
            this.m_Data.m_ShouldPlayback = true;
            this.m_Data.m_MainThreadChain.m_CleanupList = null;
            this.m_Data.m_MainThreadChain.m_Tail = null;
            this.m_Data.m_MainThreadChain.m_Head = null;
            this.m_Data.m_MainThreadChain.m_PrevCreateCommand = null;
            this.m_Data.m_MainThreadChain.m_PrevEntityCommand = null;
            this.m_Data.m_MainThreadChain.m_LastSortIndex = -1;
            this.m_Data.m_MainThreadChain.m_NextChain = null;
            this.m_Data.m_ThreadedChains = null;
            this.m_Data.m_RecordedChainCount = 0;
            DisposeSentinel.Create(out this.m_Safety0, out this.m_DisposeSentinel, 8);
            this.m_BufferSafety = AtomicSafetyHandle.Create();
            this.m_ArrayInvalidationSafety = AtomicSafetyHandle.Create();
            this.m_SafetyReadOnlyCount = 0;
            this.m_SafetyReadWriteCount = 3;
            this.SystemID = 0;
        }

        public unsafe void Dispose()
        {
            DisposeSentinel.Dispose(this.m_Safety0, ref this.m_DisposeSentinel);
            AtomicSafetyHandle.Release(this.m_ArrayInvalidationSafety);
            AtomicSafetyHandle.Release(this.m_BufferSafety);
            if (this.m_Data != null)
            {
                this.FreeChain(&this.m_Data.m_MainThreadChain);
                if (this.m_Data.m_ThreadedChains != null)
                {
                    int num = 0;
                    while (true)
                    {
                        if (num >= 0x80)
                        {
                            this.m_Data.DestroyConcurrentAccess();
                            break;
                        }
                        this.FreeChain(this.m_Data.m_ThreadedChains + num);
                        num++;
                    }
                }
                UnsafeUtility.Free((void*) this.m_Data, this.m_Data.m_Allocator);
                this.m_Data = null;
            }
        }

        private unsafe void FreeChain(EntityCommandBufferChain* chain)
        {
            if (chain != null)
            {
                EntitySharedComponentCommand* cleanupList = chain.m_CleanupList;
                while (true)
                {
                    if (cleanupList == null)
                    {
                        chain.m_CleanupList = null;
                        while (true)
                        {
                            if (chain.m_Tail == null)
                            {
                                chain.m_Head = null;
                                if (chain.m_NextChain != null)
                                {
                                    this.FreeChain(chain.m_NextChain);
                                    UnsafeUtility.Free((void*) chain.m_NextChain, this.m_Data.m_Allocator);
                                    chain.m_NextChain = null;
                                }
                                break;
                            }
                            ECBChunk* prev = chain.m_Tail.Prev;
                            UnsafeUtility.Free((void*) chain.m_Tail, this.m_Data.m_Allocator);
                            chain.m_Tail = prev;
                        }
                        break;
                    }
                    cleanupList->BoxedObject.Free();
                    cleanupList = cleanupList->Prev;
                }
            }
        }

        public unsafe void CreateEntity()
        {
            this.EnforceSingleThreadOwnership();
            EntityArchetype archetype = new EntityArchetype();
            this.m_Data.AddCreateCommand(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.CreateEntity, archetype);
        }

        public unsafe void CreateEntity(EntityArchetype archetype)
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddCreateCommand(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.CreateEntity, archetype);
        }

        public unsafe void Instantiate(Entity e)
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddEntityCommand(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.InstantiateEntity, e);
        }

        public unsafe void DestroyEntity(Entity e)
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddEntityCommand(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.DestroyEntity, e);
        }

        public DynamicBuffer<T> AddBuffer<T>() where T: struct, IBufferElementData => 
            this.AddBuffer<T>(Entity.Null);

        public unsafe DynamicBuffer<T> AddBuffer<T>(Entity e) where T: struct, IBufferElementData
        {
            this.EnforceSingleThreadOwnership();
            return this.m_Data.CreateBufferCommand<T>(ECBCommand.AddBuffer, &this.m_Data.m_MainThreadChain, 0x7fffffff, e, this.m_BufferSafety, this.m_ArrayInvalidationSafety);
        }

        public DynamicBuffer<T> SetBuffer<T>() where T: struct, IBufferElementData => 
            this.SetBuffer<T>(Entity.Null);

        public unsafe DynamicBuffer<T> SetBuffer<T>(Entity e) where T: struct, IBufferElementData
        {
            this.EnforceSingleThreadOwnership();
            return this.m_Data.CreateBufferCommand<T>(ECBCommand.SetBuffer, &this.m_Data.m_MainThreadChain, 0x7fffffff, e, this.m_BufferSafety, this.m_ArrayInvalidationSafety);
        }

        public unsafe void AddComponent<T>(Entity e, T component) where T: struct, IComponentData
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddEntityComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.AddComponent, e, component);
        }

        public void AddComponent<T>(T component) where T: struct, IComponentData
        {
            this.AddComponent<T>(Entity.Null, component);
        }

        public void SetComponent<T>(T component) where T: struct, IComponentData
        {
            this.SetComponent<T>(Entity.Null, component);
        }

        public unsafe void SetComponent<T>(Entity e, T component) where T: struct, IComponentData
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddEntityComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.SetComponent, e, component);
        }

        public void RemoveComponent<T>(Entity e)
        {
            this.RemoveComponent(e, ComponentType.Create<T>());
        }

        public unsafe void RemoveComponent(Entity e, ComponentType componentType)
        {
            this.EnforceSingleThreadOwnership();
            this.m_Data.AddEntityComponentTypeCommand(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.RemoveComponent, e, componentType);
        }

        private static bool IsDefaultObject<T>(ref T component, out int hashCode) where T: struct, ISharedComponentData
        {
            FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(TypeManager.GetTypeIndex<T>()).FastEqualityTypeInfo;
            T lhs = default(T);
            hashCode = FastEquality.GetHashCode<T>(ref component, fastEqualityTypeInfo);
            return FastEquality.Equals<T>(ref lhs, ref component, fastEqualityTypeInfo);
        }

        public void AddSharedComponent<T>(T component) where T: struct, ISharedComponentData
        {
            this.AddSharedComponent<T>(Entity.Null, component);
        }

        public unsafe void AddSharedComponent<T>(Entity e, T component) where T: struct, ISharedComponentData
        {
            int num;
            this.EnforceSingleThreadOwnership();
            if (IsDefaultObject<T>(ref component, out num))
            {
                this.m_Data.AddEntitySharedComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.AddSharedComponentData, e, num, null);
            }
            else
            {
                this.m_Data.AddEntitySharedComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.AddSharedComponentData, e, num, component);
            }
        }

        public void SetSharedComponent<T>(T component) where T: struct, ISharedComponentData
        {
            this.SetSharedComponent<T>(Entity.Null, component);
        }

        public unsafe void SetSharedComponent<T>(Entity e, T component) where T: struct, ISharedComponentData
        {
            int num;
            this.EnforceSingleThreadOwnership();
            if (IsDefaultObject<T>(ref component, out num))
            {
                this.m_Data.AddEntitySharedComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.SetSharedComponentData, e, num, null);
            }
            else
            {
                this.m_Data.AddEntitySharedComponentCommand<T>(&this.m_Data.m_MainThreadChain, 0x7fffffff, ECBCommand.SetSharedComponentData, e, num, component);
            }
        }

        public unsafe void Playback(EntityManager mgr)
        {
            if (mgr == null)
            {
                throw new NullReferenceException($"{"mgr"} cannot be null");
            }
            this.EnforceSingleThreadOwnership();
            if (this.ShouldPlayback && (this.m_Data != null))
            {
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_BufferSafety);
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_ArrayInvalidationSafety);
                Profiler.BeginSample("EntityCommandBuffer.Playback");
                NativeArray<ECBChainPlaybackState> chainStates = new NativeArray<ECBChainPlaybackState>(this.m_Data.m_RecordedChainCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
                using (chainStates)
                {
                    int index = 0;
                    EntityCommandBufferChain* nextChain = &this.m_Data.m_MainThreadChain;
                    while (true)
                    {
                        ECBChainPlaybackState state2;
                        if (nextChain == null)
                        {
                            if (this.m_Data.m_ThreadedChains != null)
                            {
                                int num2 = 0;
                                while (true)
                                {
                                    if (num2 >= 0x80)
                                    {
                                        break;
                                    }
                                    EntityCommandBufferChain* nextChain = this.m_Data.m_ThreadedChains + num2;
                                    while (true)
                                    {
                                        if (nextChain == null)
                                        {
                                            num2++;
                                            break;
                                        }
                                        if (nextChain->m_Head != null)
                                        {
                                            index++;
                                            state2 = new ECBChainPlaybackState {
                                                Chunk = nextChain->m_Head,
                                                Offset = 0,
                                                NextSortIndex = nextChain->m_Head.BaseSortIndex
                                            };
                                            chainStates.set_Item(index, state2);
                                        }
                                        nextChain = nextChain->m_NextChain;
                                    }
                                }
                            }
                            Unity.Assertions.Assert.AreEqual<int>(this.m_Data.m_RecordedChainCount, index);
                            ECBSharedPlaybackState playbackState = new ECBSharedPlaybackState {
                                CreateEntityBatchOffset = 0,
                                InstantiateEntityBatchOffset = 0,
                                CreateEntityBatch = (Entity*) stackalloc byte[(((IntPtr) 0x200) * sizeof(Entity))],
                                InstantiateEntityBatch = (Entity*) stackalloc byte[(((IntPtr) 0x200) * sizeof(Entity))],
                                CurrentEntity = Entity.Null
                            };
                            using (ECBChainPriorityQueue queue = new ECBChainPriorityQueue(chainStates, Allocator.Temp))
                            {
                                ECBChainHeapElement element = queue.Pop();
                                while (true)
                                {
                                    if (element.ChainIndex == -1)
                                    {
                                        break;
                                    }
                                    ECBChainHeapElement element2 = queue.Peek();
                                    PlaybackChain(mgr, ref playbackState, chainStates, element.ChainIndex, element2.ChainIndex);
                                    if (chainStates[element.ChainIndex].Chunk == null)
                                    {
                                        queue.Pop();
                                    }
                                    else
                                    {
                                        ECBChainHeapElement* elementPtr1 = (ECBChainHeapElement*) ref element;
                                        elementPtr1->SortIndex = chainStates[element.ChainIndex].NextSortIndex;
                                        queue.ReplaceTop(element);
                                    }
                                    element = element2;
                                }
                            }
                            break;
                        }
                        if (nextChain->m_Head != null)
                        {
                            index++;
                            state2 = new ECBChainPlaybackState {
                                Chunk = nextChain->m_Head,
                                Offset = 0,
                                NextSortIndex = nextChain->m_Head.BaseSortIndex
                            };
                            chainStates.set_Item(index, state2);
                        }
                        nextChain = nextChain->m_NextChain;
                    }
                }
                Profiler.EndSample();
            }
        }

        private static unsafe void PlaybackChain(EntityManager mgr, ref ECBSharedPlaybackState playbackState, NativeArray<ECBChainPlaybackState> chainStates, int currentChain, int nextChain)
        {
            int num = (nextChain != -1) ? chainStates[nextChain].NextSortIndex : -1;
            ECBChunk* chunk = chainStates[currentChain].Chunk;
            Unity.Assertions.Assert.IsTrue(chunk != null);
            int offset = chainStates[currentChain].Offset;
            Unity.Assertions.Assert.IsTrue((offset >= 0) && (offset < chunk->Used));
            while (true)
            {
                if (chunk != null)
                {
                    byte* numPtr = (byte*) (chunk + 1);
                    while (true)
                    {
                        if (offset < chunk->Used)
                        {
                            BasicCommand* commandPtr = (BasicCommand*) (numPtr + offset);
                            if ((nextChain == -1) || (commandPtr->SortIndex <= num))
                            {
                                int num3;
                                switch (commandPtr->CommandType)
                                {
                                    case 0:
                                    {
                                        EntityCommand* commandPtr4 = (EntityCommand*) commandPtr;
                                        if (commandPtr4->BatchCount > 0)
                                        {
                                            playbackState.InstantiateEntityBatchOffset = 0;
                                            mgr.InstantiateInternal(commandPtr4->Entity, playbackState.InstantiateEntityBatch, commandPtr4->BatchCount);
                                        }
                                        int* numPtr1 = (int*) ref playbackState.InstantiateEntityBatchOffset;
                                        num3 = numPtr1[0];
                                        numPtr1[0] = num3 + 1;
                                        playbackState.CurrentEntity = playbackState.InstantiateEntityBatch[num3];
                                        break;
                                    }
                                    case 1:
                                    {
                                        CreateCommand* commandPtr3 = (CreateCommand*) commandPtr;
                                        if (commandPtr3->BatchCount > 0)
                                        {
                                            playbackState.CreateEntityBatchOffset = 0;
                                            EntityArchetype archetype = commandPtr3->Archetype;
                                            if (!archetype.Valid)
                                            {
                                                archetype = mgr.CreateArchetype(null, 0);
                                            }
                                            mgr.CreateEntityInternal(archetype, playbackState.CreateEntityBatch, commandPtr3->BatchCount);
                                        }
                                        int* numPtr2 = (int*) ref playbackState.CreateEntityBatchOffset;
                                        num3 = numPtr2[0];
                                        numPtr2[0] = num3 + 1;
                                        playbackState.CurrentEntity = playbackState.CreateEntityBatch[num3];
                                        break;
                                    }
                                    case 2:
                                        mgr.DestroyEntity(commandPtr->Entity);
                                        break;

                                    case 3:
                                    {
                                        EntityComponentCommand* commandPtr5 = (EntityComponentCommand*) commandPtr;
                                        ComponentType type = TypeManager.GetType(commandPtr5->ComponentTypeIndex);
                                        Entity entity2 = (commandPtr5->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr5->Header.Entity;
                                        mgr.AddComponent(entity2, type);
                                        if (!type.IsZeroSized)
                                        {
                                            mgr.SetComponentDataRaw(entity2, commandPtr5->ComponentTypeIndex, (void*) (commandPtr5 + 1), commandPtr5->ComponentSize);
                                        }
                                        break;
                                    }
                                    case 4:
                                    {
                                        EntityComponentCommand* commandPtr2 = (EntityComponentCommand*) commandPtr;
                                        Entity entity = (commandPtr2->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr2->Header.Entity;
                                        mgr.RemoveComponent(entity, TypeManager.GetType(commandPtr2->ComponentTypeIndex));
                                        break;
                                    }
                                    case 5:
                                    {
                                        EntityComponentCommand* commandPtr6 = (EntityComponentCommand*) commandPtr;
                                        Entity entity3 = (commandPtr6->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr6->Header.Entity;
                                        mgr.SetComponentDataRaw(entity3, commandPtr6->ComponentTypeIndex, (void*) (commandPtr6 + 1), commandPtr6->ComponentSize);
                                        break;
                                    }
                                    case 6:
                                    {
                                        EntityBufferCommand* commandPtr7 = (EntityBufferCommand*) commandPtr;
                                        Entity entity4 = (commandPtr7->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr7->Header.Entity;
                                        mgr.AddComponent(entity4, ComponentType.FromTypeIndex(commandPtr7->ComponentTypeIndex));
                                        mgr.SetBufferRaw(entity4, commandPtr7->ComponentTypeIndex, &commandPtr7->TempBuffer, commandPtr7->ComponentSize);
                                        break;
                                    }
                                    case 7:
                                    {
                                        EntityBufferCommand* commandPtr8 = (EntityBufferCommand*) commandPtr;
                                        Entity entity5 = (commandPtr8->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr8->Header.Entity;
                                        mgr.SetBufferRaw(entity5, commandPtr8->ComponentTypeIndex, &commandPtr8->TempBuffer, commandPtr8->ComponentSize);
                                        break;
                                    }
                                    case 8:
                                    {
                                        EntitySharedComponentCommand* commandPtr9 = (EntitySharedComponentCommand*) commandPtr;
                                        Entity entity6 = (commandPtr9->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr9->Header.Entity;
                                        mgr.AddSharedComponentDataBoxed(entity6, commandPtr9->ComponentTypeIndex, commandPtr9->HashCode, commandPtr9.GetBoxedObject());
                                        break;
                                    }
                                    case 9:
                                    {
                                        EntitySharedComponentCommand* commandPtr10 = (EntitySharedComponentCommand*) commandPtr;
                                        Entity entity7 = (commandPtr10->Header.Entity == Entity.Null) ? playbackState.CurrentEntity : commandPtr10->Header.Entity;
                                        mgr.SetSharedComponentDataBoxed(entity7, commandPtr10->ComponentTypeIndex, commandPtr10->HashCode, commandPtr10.GetBoxedObject());
                                        break;
                                    }
                                    default:
                                        throw new InvalidOperationException("Invalid command buffer");
                                }
                                offset += commandPtr->TotalSize;
                                continue;
                            }
                            ECBChainPlaybackState state = chainStates[currentChain];
                            state.Chunk = chunk;
                            state.Offset = offset;
                            state.NextSortIndex = commandPtr->SortIndex;
                            chainStates.set_Item(currentChain, state);
                            break;
                        }
                        else
                        {
                            chunk = chunk->Next;
                            offset = 0;
                        }
                        break;
                    }
                    continue;
                }
                else
                {
                    ECBChainPlaybackState state2 = chainStates[currentChain];
                    state2.Chunk = null;
                    state2.Offset = 0;
                    state2.NextSortIndex = -2147483648;
                    chainStates.set_Item(currentChain, state2);
                }
                break;
            }
        }

        public unsafe Concurrent ToConcurrent()
        {
            Concurrent concurrent;
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
            concurrent.m_Safety0 = this.m_Safety0;
            AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety0);
            concurrent.m_BufferSafety = this.m_BufferSafety;
            concurrent.m_ArrayInvalidationSafety = this.m_ArrayInvalidationSafety;
            concurrent.m_SafetyReadOnlyCount = 0;
            concurrent.m_SafetyReadWriteCount = 3;
            if (this.m_Data.m_Allocator == Allocator.Temp)
            {
                throw new InvalidOperationException("EntityCommandBuffer.Concurrent can not use Allocator.Temp; use Allocator.TempJob instead");
            }
            concurrent.m_Data = this.m_Data;
            concurrent.m_ThreadIndex = -1;
            if (concurrent.m_Data != null)
            {
                concurrent.m_Data.InitConcurrentAccess();
            }
            return concurrent;
        }
        [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerIsAtomicWriteOnly]
        public struct Concurrent
        {
            [NativeDisableUnsafePtrRestriction]
            internal unsafe EntityCommandBufferData* m_Data;
            internal AtomicSafetyHandle m_Safety0;
            internal AtomicSafetyHandle m_BufferSafety;
            internal AtomicSafetyHandle m_ArrayInvalidationSafety;
            internal int m_SafetyReadOnlyCount;
            internal int m_SafetyReadWriteCount;
            [NativeSetThreadIndex]
            internal int m_ThreadIndex;
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckWriteAccess()
            {
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
            }

            private EntityCommandBufferChain* ThreadChain
            {
                get
                {
                    if (this.m_ThreadIndex == -1)
                    {
                        throw new InvalidOperationException("EntityCommandBuffer.Concurrent must only be used in a Job");
                    }
                    return (this.m_Data.m_ThreadedChains + this.m_ThreadIndex);
                }
            }
            public unsafe void CreateEntity(int jobIndex)
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                EntityArchetype archetype = new EntityArchetype();
                this.m_Data.AddCreateCommand(threadChain, jobIndex, ECBCommand.CreateEntity, archetype);
                threadChain.TemporaryForceDisableBatching();
            }

            public unsafe void CreateEntity(int jobIndex, EntityArchetype archetype)
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddCreateCommand(threadChain, jobIndex, ECBCommand.CreateEntity, archetype);
                threadChain.TemporaryForceDisableBatching();
            }

            public unsafe void Instantiate(int jobIndex, Entity e)
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddEntityCommand(threadChain, jobIndex, ECBCommand.InstantiateEntity, e);
                threadChain.TemporaryForceDisableBatching();
            }

            public unsafe void DestroyEntity(int jobIndex, Entity e)
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddEntityCommand(threadChain, jobIndex, ECBCommand.DestroyEntity, e);
                threadChain.TemporaryForceDisableBatching();
            }

            public unsafe void AddComponent<T>(int jobIndex, Entity e, T component) where T: struct, IComponentData
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddEntityComponentCommand<T>(threadChain, jobIndex, ECBCommand.AddComponent, e, component);
            }

            public DynamicBuffer<T> AddBuffer<T>(int jobIndex) where T: struct, IBufferElementData => 
                this.AddBuffer<T>(jobIndex, Entity.Null);

            public unsafe DynamicBuffer<T> AddBuffer<T>(int jobIndex, Entity e) where T: struct, IBufferElementData
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                return this.m_Data.CreateBufferCommand<T>(ECBCommand.AddBuffer, threadChain, jobIndex, e, this.m_BufferSafety, this.m_ArrayInvalidationSafety);
            }

            public DynamicBuffer<T> SetBuffer<T>(int jobIndex) where T: struct, IBufferElementData => 
                this.SetBuffer<T>(jobIndex, Entity.Null);

            public unsafe DynamicBuffer<T> SetBuffer<T>(int jobIndex, Entity e) where T: struct, IBufferElementData
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                return this.m_Data.CreateBufferCommand<T>(ECBCommand.SetBuffer, threadChain, jobIndex, e, this.m_BufferSafety, this.m_ArrayInvalidationSafety);
            }

            public void AddComponent<T>(int jobIndex, T component) where T: struct, IComponentData
            {
                this.AddComponent<T>(jobIndex, Entity.Null, component);
            }

            public void SetComponent<T>(int jobIndex, T component) where T: struct, IComponentData
            {
                this.SetComponent<T>(jobIndex, Entity.Null, component);
            }

            public unsafe void SetComponent<T>(int jobIndex, Entity e, T component) where T: struct, IComponentData
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddEntityComponentCommand<T>(threadChain, jobIndex, ECBCommand.SetComponent, e, component);
            }

            public void RemoveComponent<T>(int jobIndex, Entity e)
            {
                this.RemoveComponent(jobIndex, e, ComponentType.Create<T>());
            }

            public unsafe void RemoveComponent(int jobIndex, Entity e, ComponentType componentType)
            {
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                this.m_Data.AddEntityComponentTypeCommand(threadChain, jobIndex, ECBCommand.RemoveComponent, e, componentType);
            }

            public void AddSharedComponent<T>(int jobIndex, T component) where T: struct, ISharedComponentData
            {
                this.AddSharedComponent<T>(jobIndex, Entity.Null, component);
            }

            public unsafe void AddSharedComponent<T>(int jobIndex, Entity e, T component) where T: struct, ISharedComponentData
            {
                int num;
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                if (EntityCommandBuffer.IsDefaultObject<T>(ref component, out num))
                {
                    this.m_Data.AddEntitySharedComponentCommand<T>(threadChain, jobIndex, ECBCommand.AddSharedComponentData, e, num, null);
                }
                else
                {
                    this.m_Data.AddEntitySharedComponentCommand<T>(threadChain, jobIndex, ECBCommand.AddSharedComponentData, e, num, component);
                }
            }

            public void SetSharedComponent<T>(int jobIndex, T component) where T: struct, ISharedComponentData
            {
                this.SetSharedComponent<T>(jobIndex, Entity.Null, component);
            }

            public unsafe void SetSharedComponent<T>(int jobIndex, Entity e, T component) where T: struct, ISharedComponentData
            {
                int num;
                this.CheckWriteAccess();
                EntityCommandBufferChain* threadChain = this.ThreadChain;
                if (EntityCommandBuffer.IsDefaultObject<T>(ref component, out num))
                {
                    this.m_Data.AddEntitySharedComponentCommand<T>(threadChain, jobIndex, ECBCommand.SetSharedComponentData, e, num, null);
                }
                else
                {
                    this.m_Data.AddEntitySharedComponentCommand<T>(threadChain, jobIndex, ECBCommand.SetSharedComponentData, e, num, component);
                }
            }
        }
    }
}

