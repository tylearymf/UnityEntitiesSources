namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentTypes
    {
        private ResizableArray64Byte<int> m_sorted;
        public Masks m_masks;
        private unsafe void ComputeMasks()
        {
            for (int i = 0; i < this.m_sorted.Length; i++)
            {
                int typeIndex = this.m_sorted[i];
                TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo(typeIndex);
                ushort num3 = (ushort) (1 << (i & 0x1f));
                if (typeInfo.BufferCapacity >= 0)
                {
                    ushort* numPtr1 = (ushort*) ref this.m_masks.m_BufferMask;
                    numPtr1[0] = (ushort) (numPtr1[0] | num3);
                }
                if (typeInfo.IsSystemStateComponent)
                {
                    ushort* numPtr2 = (ushort*) ref this.m_masks.m_SystemStateComponentMask;
                    numPtr2[0] = (ushort) (numPtr2[0] | num3);
                }
                if (typeInfo.IsSystemStateSharedComponent)
                {
                    ushort* numPtr3 = (ushort*) ref this.m_masks.m_SystemStateSharedComponentMask;
                    numPtr3[0] = (ushort) (numPtr3[0] | num3);
                }
                if (TypeManager.TypeCategory.ISharedComponentData == typeInfo.Category)
                {
                    ushort* numPtr4 = (ushort*) ref this.m_masks.m_SharedComponentMask;
                    numPtr4[0] = (ushort) (numPtr4[0] | num3);
                }
                if (typeInfo.IsZeroSized)
                {
                    ushort* numPtr5 = (ushort*) ref this.m_masks.m_ZeroSizedMask;
                    numPtr5[0] = (ushort) (numPtr5[0] | num3);
                }
            }
        }

        public int Length =>
            this.m_sorted.Length;
        public int GetTypeIndex(int index) => 
            this.m_sorted[index];

        public ComponentType GetComponentType(int index) => 
            TypeManager.GetType(this.m_sorted[index]);

        public unsafe ComponentTypes(ComponentType a)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = 1;
            SortingUtilities.InsertSorted((int*) this.m_sorted.GetUnsafePointer(), 0, a.TypeIndex);
            this.ComputeMasks();
        }

        public unsafe ComponentTypes(ComponentType a, ComponentType b)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = 2;
            int* data = (int*) ref this.m_sorted.GetUnsafePointer();
            SortingUtilities.InsertSorted(data, 0, a.TypeIndex);
            SortingUtilities.InsertSorted(data, 1, b.TypeIndex);
            this.ComputeMasks();
        }

        public unsafe ComponentTypes(ComponentType a, ComponentType b, ComponentType c)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = 3;
            int* data = (int*) ref this.m_sorted.GetUnsafePointer();
            SortingUtilities.InsertSorted(data, 0, a.TypeIndex);
            SortingUtilities.InsertSorted(data, 1, b.TypeIndex);
            SortingUtilities.InsertSorted(data, 2, c.TypeIndex);
            this.ComputeMasks();
        }

        public unsafe ComponentTypes(ComponentType a, ComponentType b, ComponentType c, ComponentType d)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = 4;
            int* data = (int*) ref this.m_sorted.GetUnsafePointer();
            SortingUtilities.InsertSorted(data, 0, a.TypeIndex);
            SortingUtilities.InsertSorted(data, 1, b.TypeIndex);
            SortingUtilities.InsertSorted(data, 2, c.TypeIndex);
            SortingUtilities.InsertSorted(data, 3, d.TypeIndex);
            this.ComputeMasks();
        }

        public unsafe ComponentTypes(ComponentType a, ComponentType b, ComponentType c, ComponentType d, ComponentType e)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = 5;
            int* data = (int*) ref this.m_sorted.GetUnsafePointer();
            SortingUtilities.InsertSorted(data, 0, a.TypeIndex);
            SortingUtilities.InsertSorted(data, 1, b.TypeIndex);
            SortingUtilities.InsertSorted(data, 2, c.TypeIndex);
            SortingUtilities.InsertSorted(data, 3, d.TypeIndex);
            SortingUtilities.InsertSorted(data, 4, e.TypeIndex);
            this.ComputeMasks();
        }

        public unsafe ComponentTypes(ComponentType[] componentType)
        {
            this.m_sorted = new ResizableArray64Byte<int>();
            this.m_masks = new Masks();
            this.m_sorted.Length = componentType.Length;
            int* data = (int*) ref this.m_sorted.GetUnsafePointer();
            int length = 0;
            while (true)
            {
                if (length >= componentType.Length)
                {
                    this.ComputeMasks();
                    return;
                }
                SortingUtilities.InsertSorted(data, length, componentType[length].TypeIndex);
                length++;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Masks
        {
            public ushort m_BufferMask;
            public ushort m_SystemStateComponentMask;
            public ushort m_SystemStateSharedComponentMask;
            public ushort m_SharedComponentMask;
            public ushort m_ZeroSizedMask;
            public bool IsSharedComponent(int index) => 
                ((this.m_SharedComponentMask & (1 << (index & 0x1f))) != 0);

            public bool IsZeroSized(int index) => 
                ((this.m_ZeroSizedMask & (1 << (index & 0x1f))) != 0);

            public int Buffers =>
                math.countbits((uint) this.m_BufferMask);
            public int SystemStateComponents =>
                math.countbits((uint) this.m_SystemStateComponentMask);
            public int SystemStateSharedComponents =>
                math.countbits((uint) this.m_SystemStateSharedComponentMask);
            public int SharedComponents =>
                math.countbits((uint) this.m_SharedComponentMask);
            public int ZeroSizeds =>
                math.countbits((uint) this.m_ZeroSizedMask);
        }
    }
}

