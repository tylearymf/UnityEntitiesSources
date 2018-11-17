namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ECBChainPriorityQueue : IDisposable
    {
        private unsafe readonly ECBChainHeapElement* m_Heap;
        private int m_Size;
        private readonly Allocator m_Allocator;
        private static readonly int BaseIndex;
        public unsafe ECBChainPriorityQueue(NativeArray<ECBChainPlaybackState> chainStates, Allocator alloc)
        {
            this.m_Size = chainStates.Length;
            this.m_Allocator = alloc;
            this.m_Heap = (ECBChainHeapElement*) UnsafeUtility.Malloc((long) ((this.m_Size + BaseIndex) * sizeof(ECBChainHeapElement)), 0x40, this.m_Allocator);
            int num = this.m_Size - 1;
            while (true)
            {
                if (num < (this.m_Size / 2))
                {
                    for (int i = (this.m_Size / 2) - 1; i >= 0; i--)
                    {
                        this.m_Heap[BaseIndex + i].SortIndex = chainStates[i].NextSortIndex;
                        this.m_Heap[BaseIndex + i].ChainIndex = i;
                        this.Heapify(BaseIndex + i);
                    }
                    return;
                }
                this.m_Heap[BaseIndex + num].SortIndex = chainStates[num].NextSortIndex;
                this.m_Heap[BaseIndex + num].ChainIndex = num;
                num--;
            }
        }

        public unsafe void Dispose()
        {
            UnsafeUtility.Free((void*) this.m_Heap, this.m_Allocator);
        }

        public bool Empty =>
            (this.m_Size <= 0);
        public unsafe ECBChainHeapElement Peek()
        {
            ECBChainHeapElement element2;
            if (!this.Empty)
            {
                element2 = this.m_Heap[BaseIndex];
            }
            else
            {
                element2 = new ECBChainHeapElement {
                    ChainIndex = -1,
                    SortIndex = -1
                };
            }
            return element2;
        }

        public unsafe ECBChainHeapElement Pop()
        {
            ECBChainHeapElement element3;
            if (this.Empty)
            {
                element3 = new ECBChainHeapElement {
                    ChainIndex = -1,
                    SortIndex = -1
                };
            }
            else
            {
                ECBChainHeapElement element = this.Peek();
                int size = this.m_Size;
                this.m_Size = size - 1;
                this.m_Heap[BaseIndex] = this.m_Heap[size];
                if (!this.Empty)
                {
                    this.Heapify(BaseIndex);
                }
                element3 = element;
            }
            return element3;
        }

        public unsafe void ReplaceTop(ECBChainHeapElement value)
        {
            Assert.IsTrue(!this.Empty, "Can't ReplaceTop() an empty heap");
            this.m_Heap[BaseIndex] = value;
            this.Heapify(BaseIndex);
        }

        private unsafe void Heapify(int i)
        {
            object[] objArray1 = new object[] { "heap index ", i, " is out of range with size=", this.m_Size };
            Assert.IsTrue((i >= BaseIndex) && (i <= this.m_Size), string.Concat(objArray1));
            ECBChainHeapElement element = this.m_Heap[i];
            while (true)
            {
                if (i > (this.m_Size / 2))
                {
                    break;
                }
                int index = 2 * i;
                if ((index < this.m_Size) && (this.m_Heap[index + 1].SortIndex < this.m_Heap[index].SortIndex))
                {
                    index++;
                }
                if (element.SortIndex < this.m_Heap[index].SortIndex)
                {
                    break;
                }
                this.m_Heap[i] = this.m_Heap[index];
                i = index;
            }
            this.m_Heap[i] = element;
        }

        static ECBChainPriorityQueue()
        {
            BaseIndex = 1;
        }
    }
}

