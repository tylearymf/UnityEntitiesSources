namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityCommandBufferData
    {
        public EntityCommandBufferChain m_MainThreadChain;
        public unsafe EntityCommandBufferChain* m_ThreadedChains;
        public int m_RecordedChainCount;
        public int m_MinimumChunkSize;
        public Allocator m_Allocator;
        public bool m_ShouldPlayback;
        public const int kMaxBatchCount = 0x200;
        internal unsafe void InitConcurrentAccess()
        {
            if (this.m_ThreadedChains == null)
            {
                int num = sizeof(EntityCommandBufferChain) * 0x80;
                this.m_ThreadedChains = (EntityCommandBufferChain*) UnsafeUtility.Malloc((long) num, 0x40, this.m_Allocator);
                UnsafeUtility.MemClear((void*) this.m_ThreadedChains, (long) num);
            }
        }

        internal unsafe void DestroyConcurrentAccess()
        {
            if (this.m_ThreadedChains != null)
            {
                UnsafeUtility.Free((void*) this.m_ThreadedChains, this.m_Allocator);
                this.m_ThreadedChains = null;
            }
        }

        internal unsafe void AddCreateCommand(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, EntityArchetype archetype)
        {
            int num1;
            if ((chain.m_PrevCreateCommand == null) || !(chain.m_PrevCreateCommand.Archetype == archetype))
            {
                num1 = 0;
            }
            else
            {
                num1 = (int) (chain.m_PrevCreateCommand.BatchCount < 0x200);
            }
            if (num1 == 0)
            {
                CreateCommand* commandPtr2 = (CreateCommand*) ref this.Reserve(chain, jobIndex, sizeof(CreateCommand));
                commandPtr2->Header.CommandType = (int) op;
                commandPtr2->Header.TotalSize = sizeof(CreateCommand);
                commandPtr2->Header.SortIndex = chain.m_LastSortIndex;
                commandPtr2->Archetype = archetype;
                commandPtr2->BatchCount = 1;
                chain.m_PrevCreateCommand = commandPtr2;
            }
            else
            {
                int* numPtr1 = (int*) ref chain.m_PrevCreateCommand.BatchCount;
                numPtr1[0]++;
                CreateCommand* commandPtr = (CreateCommand*) ref this.Reserve(chain, jobIndex, sizeof(CreateCommand));
                commandPtr->Header.CommandType = (int) op;
                commandPtr->Header.TotalSize = sizeof(CreateCommand);
                commandPtr->Header.SortIndex = chain.m_LastSortIndex;
                commandPtr->Archetype = archetype;
                commandPtr->BatchCount = 0;
            }
        }

        internal unsafe void AddEntityCommand(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, Entity e)
        {
            int num1;
            if ((chain.m_PrevEntityCommand == null) || !(chain.m_PrevEntityCommand.Entity == e))
            {
                num1 = 0;
            }
            else
            {
                num1 = (int) (chain.m_PrevEntityCommand.BatchCount < 0x200);
            }
            if (num1 == 0)
            {
                EntityCommand* commandPtr2 = (EntityCommand*) ref this.Reserve(chain, jobIndex, sizeof(EntityCommand));
                commandPtr2->Header.CommandType = (int) op;
                commandPtr2->Header.TotalSize = sizeof(EntityCommand);
                commandPtr2->Header.SortIndex = chain.m_LastSortIndex;
                commandPtr2->Entity = e;
                commandPtr2->BatchCount = 1;
                chain.m_PrevEntityCommand = commandPtr2;
            }
            else
            {
                int* numPtr1 = (int*) ref chain.m_PrevEntityCommand.BatchCount;
                numPtr1[0]++;
                EntityCommand* commandPtr = (EntityCommand*) ref this.Reserve(chain, jobIndex, sizeof(EntityCommand));
                commandPtr->Header.CommandType = (int) op;
                commandPtr->Header.TotalSize = sizeof(EntityCommand);
                commandPtr->Header.SortIndex = chain.m_LastSortIndex;
                commandPtr->Entity = e;
                commandPtr->BatchCount = 0;
            }
        }

        internal unsafe void AddEntityComponentCommand<T>(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, Entity e, T component) where T: struct
        {
            int num2 = UnsafeUtility.SizeOf<T>();
            int size = Align(sizeof(EntityComponentCommand) + num2, 8);
            EntityComponentCommand* commandPtr = (EntityComponentCommand*) this.Reserve(chain, jobIndex, size);
            commandPtr->Header.Header.CommandType = (int) op;
            commandPtr->Header.Header.TotalSize = size;
            commandPtr->Header.Header.SortIndex = chain.m_LastSortIndex;
            commandPtr->Header.Entity = e;
            commandPtr->ComponentTypeIndex = TypeManager.GetTypeIndex<T>();
            commandPtr->ComponentSize = num2;
            UnsafeUtility.CopyStructureToPtr<T>(ref component, (void*) (commandPtr + 1));
        }

        internal unsafe BufferHeader* AddEntityBufferCommand<T>(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, Entity e) where T: struct, IBufferElementData
        {
            TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo<T>();
            int size = Align(sizeof(EntityBufferCommand) + typeInfo.SizeInChunk, 8);
            EntityBufferCommand* commandPtr = (EntityBufferCommand*) ref this.Reserve(chain, jobIndex, size);
            commandPtr->Header.Header.CommandType = (int) op;
            commandPtr->Header.Header.TotalSize = size;
            commandPtr->Header.Header.SortIndex = chain.m_LastSortIndex;
            commandPtr->Header.Entity = e;
            commandPtr->ComponentTypeIndex = TypeManager.GetTypeIndex<T>();
            commandPtr->ComponentSize = typeInfo.SizeInChunk;
            BufferHeader.Initialize(&commandPtr->TempBuffer, typeInfo.BufferCapacity);
            return &commandPtr->TempBuffer;
        }

        internal static int Align(int size, int alignmentPowerOfTwo) => 
            (((size + alignmentPowerOfTwo) - 1) & ~(alignmentPowerOfTwo - 1));

        internal unsafe void AddEntityComponentTypeCommand(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, Entity e, ComponentType t)
        {
            int size = Align(sizeof(EntityComponentCommand), 8);
            EntityComponentCommand* commandPtr = (EntityComponentCommand*) ref this.Reserve(chain, jobIndex, size);
            commandPtr->Header.Header.CommandType = (int) op;
            commandPtr->Header.Header.TotalSize = size;
            commandPtr->Header.Header.SortIndex = chain.m_LastSortIndex;
            commandPtr->Header.Entity = e;
            commandPtr->ComponentTypeIndex = t.TypeIndex;
        }

        internal unsafe void AddEntitySharedComponentCommand<T>(EntityCommandBufferChain* chain, int jobIndex, ECBCommand op, Entity e, int hashCode, object boxedObject) where T: struct
        {
            int size = Align(sizeof(EntitySharedComponentCommand), 8);
            EntitySharedComponentCommand* commandPtr = (EntitySharedComponentCommand*) ref this.Reserve(chain, jobIndex, size);
            commandPtr->Header.Header.CommandType = (int) op;
            commandPtr->Header.Header.TotalSize = size;
            commandPtr->Header.Header.SortIndex = chain.m_LastSortIndex;
            commandPtr->Header.Entity = e;
            commandPtr->ComponentTypeIndex = TypeManager.GetTypeIndex<T>();
            commandPtr->HashCode = hashCode;
            if (boxedObject == null)
            {
                commandPtr->BoxedObject = new GCHandle();
            }
            else
            {
                commandPtr->BoxedObject = GCHandle.Alloc(boxedObject);
                commandPtr->Prev = chain.m_CleanupList;
                chain.m_CleanupList = commandPtr;
            }
        }

        internal unsafe byte* Reserve(EntityCommandBufferChain* chain, int jobIndex, int size)
        {
            int num = jobIndex;
            if (num < chain.m_LastSortIndex)
            {
                EntityCommandBufferChain* chainPtr = (EntityCommandBufferChain*) ref UnsafeUtility.Malloc((long) sizeof(EntityCommandBufferChain), 8, this.m_Allocator);
                chainPtr[0] = chain[0];
                UnsafeUtility.MemClear((void*) chain, (long) sizeof(EntityCommandBufferChain));
                chain.m_NextChain = chainPtr;
            }
            chain.m_LastSortIndex = num;
            if ((chain.m_Tail == null) || (chain.m_Tail.Capacity < size))
            {
                int num3 = math.max(this.m_MinimumChunkSize, size);
                ECBChunk* chunkPtr = (ECBChunk*) ref UnsafeUtility.Malloc((long) (sizeof(ECBChunk) + num3), 0x10, this.m_Allocator);
                ECBChunk* tail = chain.m_Tail;
                chunkPtr->Next = null;
                chunkPtr->Prev = tail;
                chunkPtr->Used = 0;
                chunkPtr->Size = num3;
                if (tail != null)
                {
                    tail->Next = chunkPtr;
                }
                if (chain.m_Head == null)
                {
                    chain.m_Head = chunkPtr;
                    Interlocked.Increment(ref this.m_RecordedChainCount);
                }
                chain.m_Tail = chunkPtr;
            }
            int num2 = chain.m_Tail.Bump(size);
            return (byte*) ((chain.m_Tail + 1) + num2);
        }

        public unsafe DynamicBuffer<T> CreateBufferCommand<T>(ECBCommand commandType, EntityCommandBufferChain* chain, int jobIndex, Entity e, AtomicSafetyHandle bufferSafety, AtomicSafetyHandle arrayInvalidationSafety) where T: struct, IBufferElementData
        {
            AtomicSafetyHandle handle = bufferSafety;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            return new DynamicBuffer<T>(this.AddEntityBufferCommand<T>(chain, jobIndex, commandType, e), handle, arrayInvalidationSafety, false);
        }
    }
}

