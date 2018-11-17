namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Assertions;

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeLinkedListNode
    {
        public unsafe UnsafeLinkedListNode* Prev;
        public unsafe UnsafeLinkedListNode* Next;
        public static unsafe void InitializeList(UnsafeLinkedListNode* list)
        {
            list.Prev = list;
            list.Next = list;
        }

        public bool IsInList =>
            (this.Prev != null);
        public UnsafeLinkedListNode* Begin =>
            this.Next;
        public UnsafeLinkedListNode* Back =>
            this.Prev;
        public bool IsEmpty =>
            (((IntPtr) this) == this.Next);
        public UnsafeLinkedListNode* End =>
            ((UnsafeLinkedListNode*) this);
        public unsafe void Add(UnsafeLinkedListNode* node)
        {
            InsertBefore((UnsafeLinkedListNode*) this, node);
            fixed (UnsafeLinkedListNode* nodeRef = null)
            {
                return;
            }
        }

        public static unsafe void InsertBefore(UnsafeLinkedListNode* pos, UnsafeLinkedListNode* node)
        {
            Assert.IsTrue(node != pos);
            Assert.IsFalse(node.IsInList);
            node.Prev = pos.Prev;
            node.Next = pos;
            node.Prev.Next = node;
            node.Next.Prev = node;
        }

        public static unsafe void InsertListBefore(UnsafeLinkedListNode* pos, UnsafeLinkedListNode* srcList)
        {
            Assert.IsTrue(pos != srcList);
            Assert.IsFalse(srcList.IsEmpty);
            UnsafeLinkedListNode* prev = pos.Prev;
            UnsafeLinkedListNode* nodePtr2 = pos;
            prev->Next = srcList.Next;
            nodePtr2->Prev = srcList.Prev;
            prev->Next.Prev = prev;
            nodePtr2->Prev.Next = nodePtr2;
            srcList.Next = srcList;
            srcList.Prev = srcList;
        }

        public unsafe void Remove()
        {
            if (this.Prev != null)
            {
                this.Prev.Next = this.Next;
                this.Next.Prev = this.Prev;
                this.Prev = null;
                this.Next = null;
            }
        }
    }
}

