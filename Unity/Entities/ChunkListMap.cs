namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ChunkListMap : IDisposable
    {
        private unsafe Node* buckets;
        private uint hashMask;
        private uint emptyNodes;
        public unsafe void Init(int count)
        {
            Assert.IsTrue((count & (count - 1)) == 0);
            this.buckets = (Node*) UnsafeUtility.Malloc((long) (count * sizeof(Node)), 8, Allocator.Persistent);
            this.hashMask = (uint) (count - 1);
            UnsafeUtility.MemClear((void*) this.buckets, (long) (count * sizeof(Node)));
            this.emptyNodes = (uint) count;
        }

        public unsafe void AppendFrom(ChunkListMap* src)
        {
            if (src.buckets != null)
            {
                Node* buckets = src.buckets;
                int num = ((int) src.hashMask) + 1;
                int num2 = 0;
                while (true)
                {
                    if (num2 >= num)
                    {
                        break;
                    }
                    Node* nodePtr2 = buckets + num2;
                    if (!nodePtr2.IsDeleted() && !nodePtr2.IsFree())
                    {
                        this.AddMultiple(&nodePtr2->list);
                    }
                    num2++;
                }
            }
        }

        private unsafe uint GetHashCode(int* sharedComponentDataIndices, int numSharedComponents)
        {
            ulong num = 0x165667b19e3779f9L;
            for (int i = 0; i < numSharedComponents; i++)
            {
                num += (ulong) (sharedComponentDataIndices[i] * -4417276706812531889L);
                num = ((num >> 0x1f) | (num << 0x21)) * 11_400_714_785_074_694_791L;
            }
            return (uint) (num ^ (num >> 0x20));
        }

        public unsafe Chunk* GetChunkWithEmptySlots(int* sharedComponentDataIndices, int numSharedComponents)
        {
            uint hashCode = this.GetHashCode(sharedComponentDataIndices, numSharedComponents);
            Node* buckets = this.buckets + ((hashCode & this.hashMask) * sizeof(Node));
            Node* nodePtr2 = this.buckets + (this.hashMask * sizeof(Node));
            while (true)
            {
                Chunk* chunkFromEmptySlotNode;
                if (buckets.IsFree())
                {
                    chunkFromEmptySlotNode = null;
                }
                else
                {
                    if (!(!buckets.IsDeleted() && buckets.CheckEqual(hashCode, sharedComponentDataIndices, numSharedComponents)))
                    {
                        buckets++;
                        if (buckets <= nodePtr2)
                        {
                            continue;
                        }
                        buckets = this.buckets;
                        continue;
                    }
                    chunkFromEmptySlotNode = ArchetypeManager.GetChunkFromEmptySlotNode(buckets->list.Begin);
                }
                return chunkFromEmptySlotNode;
            }
        }

        public unsafe void Add(Chunk* chunk)
        {
            int* sharedComponentValueArray = chunk.SharedComponentValueArray;
            int numSharedComponents = chunk.Archetype.NumSharedComponents;
            uint hashCode = this.GetHashCode(sharedComponentValueArray, numSharedComponents);
            Node* buckets = this.buckets + ((hashCode & this.hashMask) * sizeof(Node));
            Node* nodePtr2 = this.buckets + (this.hashMask * sizeof(Node));
            Node* nodePtr3 = null;
            while (true)
            {
                if (buckets.IsFree())
                {
                    if (nodePtr3 == null)
                    {
                        nodePtr3 = buckets;
                        this.emptyNodes--;
                    }
                    nodePtr3->hash = hashCode;
                    UnsafeLinkedListNode.InitializeList(&nodePtr3->list);
                    nodePtr3->list.Add(&chunk.ChunkListWithEmptySlotsNode);
                    if (this.ShouldGrow(this.emptyNodes))
                    {
                        this.Grow();
                    }
                    break;
                }
                if (!buckets.IsDeleted())
                {
                    if (buckets.CheckEqual(hashCode, sharedComponentValueArray, numSharedComponents))
                    {
                        buckets->list.Add(&chunk.ChunkListWithEmptySlotsNode);
                        break;
                    }
                }
                else if (nodePtr3 == null)
                {
                    nodePtr3 = buckets;
                }
                buckets++;
                if (buckets > nodePtr2)
                {
                    buckets = this.buckets;
                }
            }
        }

        private unsafe void AddMultiple(UnsafeLinkedListNode* list)
        {
            Chunk* chunkPtr = ref ArchetypeManager.GetChunkFromEmptySlotNode(list.Begin);
            uint hashCode = this.GetHashCode(chunkPtr->SharedComponentValueArray, chunkPtr->Archetype.NumSharedComponents);
            int* sharedComponentValueArray = chunkPtr->SharedComponentValueArray;
            int numSharedComponents = chunkPtr->Archetype.NumSharedComponents;
            Node* buckets = this.buckets + ((hashCode & this.hashMask) * sizeof(Node));
            Node* nodePtr2 = this.buckets + (this.hashMask * sizeof(Node));
            Node* nodePtr3 = null;
            while (true)
            {
                if (buckets.IsFree())
                {
                    if (nodePtr3 == null)
                    {
                        nodePtr3 = buckets;
                        this.emptyNodes--;
                    }
                    nodePtr3->hash = hashCode;
                    UnsafeLinkedListNode.InitializeList(&nodePtr3->list);
                    UnsafeLinkedListNode.InsertListBefore(nodePtr3->list.End, list);
                    if (this.ShouldGrow(this.emptyNodes))
                    {
                        this.Grow();
                    }
                    break;
                }
                if (!buckets.IsDeleted())
                {
                    if (buckets.CheckEqual(hashCode, sharedComponentValueArray, numSharedComponents))
                    {
                        UnsafeLinkedListNode.InsertListBefore(buckets->list.End, list);
                        break;
                    }
                }
                else if (nodePtr3 == null)
                {
                    nodePtr3 = buckets;
                }
                buckets++;
                if (buckets > nodePtr2)
                {
                    buckets = this.buckets;
                }
            }
        }

        private bool ShouldGrow(uint unoccupiedNodes) => 
            ((unoccupiedNodes * 3) < this.hashMask);

        private unsafe void Grow()
        {
            uint unoccupiedNodes = 0;
            int num4 = 0;
            while (true)
            {
                if (num4 > this.hashMask)
                {
                    int count = (int) ((this.hashMask + 1) * (this.ShouldGrow(unoccupiedNodes) ? 2 : 1));
                    Node* buckets = this.buckets;
                    int num3 = ((int) this.hashMask) + 1;
                    this.Init(count);
                    Node* nodePtr2 = this.buckets + (this.hashMask * sizeof(Node));
                    int num5 = 0;
                    while (true)
                    {
                        if (num5 >= num3)
                        {
                            UnsafeUtility.Free((void*) buckets, Allocator.Persistent);
                            return;
                        }
                        Node* nodePtr3 = buckets + num5;
                        if (!nodePtr3.IsDeleted() && !nodePtr3.IsFree())
                        {
                            uint hash = nodePtr3->hash;
                            Node* nodePtr4 = this.buckets + ((hash & this.hashMask) * sizeof(Node));
                            while (true)
                            {
                                if (nodePtr4.IsFree())
                                {
                                    nodePtr4[0] = nodePtr3[0];
                                    nodePtr4->list.Next.Prev = &nodePtr4->list;
                                    nodePtr4->list.Prev.Next = &nodePtr4->list;
                                    this.emptyNodes--;
                                    break;
                                }
                                nodePtr4++;
                                if (nodePtr4 > nodePtr2)
                                {
                                    nodePtr4 = this.buckets;
                                }
                            }
                        }
                        num5++;
                    }
                }
                if (((this.buckets + num4)).IsFree() || ((this.buckets + num4)).IsDeleted())
                {
                    unoccupiedNodes++;
                }
                num4++;
            }
        }

        public unsafe void Dispose()
        {
            if (this.buckets != null)
            {
                UnsafeUtility.Free((void*) this.buckets, Allocator.Persistent);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Node
        {
            public UnsafeLinkedListNode list;
            public uint hash;
            public unsafe bool IsFree() => 
                (this.list.Next == null);

            public bool IsDeleted() => 
                this.list.IsEmpty;

            public unsafe bool CheckEqual(uint hash, int* sharedComponentDataIndices, int numSharedComponents) => 
                ((this.hash == hash) && (UnsafeUtility.MemCmp((void*) sharedComponentDataIndices, (void*) ArchetypeManager.GetChunkFromEmptySlotNode(this.list.Begin).SharedComponentValueArray, (long) (numSharedComponents * 4)) == 0));
        }
    }
}

