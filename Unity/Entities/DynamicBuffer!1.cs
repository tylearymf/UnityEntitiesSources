namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct DynamicBuffer<T> where T: struct
    {
        [NativeDisableUnsafePtrRestriction]
        private unsafe BufferHeader* m_Buffer;
        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_Safety1;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;
        internal bool m_IsReadOnly;
        internal unsafe DynamicBuffer(BufferHeader* header, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly)
        {
            this.m_Buffer = header;
            this.m_Safety0 = safety;
            this.m_Safety1 = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            this.m_IsReadOnly = isReadOnly;
        }

        public int Length =>
            this.m_Buffer.Length;
        public int Capacity =>
            this.m_Buffer.Capacity;
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckBounds(int index)
        {
            if (index >= this.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{this.Length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
        }

        public T this[int index]
        {
            get
            {
                this.CheckReadAccess();
                this.CheckBounds(index);
                return UnsafeUtility.ReadArrayElement<T>((void*) BufferHeader.GetElementPointer(this.m_Buffer), index);
            }
            set
            {
                this.CheckWriteAccess();
                this.CheckBounds(index);
                UnsafeUtility.WriteArrayElement<T>((void*) BufferHeader.GetElementPointer(this.m_Buffer), index, value);
            }
        }
        public unsafe void ResizeUninitialized(int length)
        {
            this.InvalidateArrayAliases();
            BufferHeader.EnsureCapacity(this.m_Buffer, length, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData);
            this.m_Buffer.Length = length;
        }

        private void InvalidateArrayAliases()
        {
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety1);
        }

        public unsafe void Clear()
        {
            this.m_Buffer.Length = 0;
        }

        public unsafe void TrimExcess()
        {
            byte* pointer = this.m_Buffer.Pointer;
            int length = this.m_Buffer.Length;
            if ((length != this.Capacity) && (pointer != null))
            {
                int num2 = UnsafeUtility.SizeOf<T>();
                int alignment = UnsafeUtility.AlignOf<T>();
                byte* numPtr2 = (byte*) ref UnsafeUtility.Malloc((long) (num2 * length), alignment, Allocator.Persistent);
                UnsafeUtility.MemCpy((void*) numPtr2, (void*) pointer, (long) (num2 * length));
                this.m_Buffer.Capacity = length;
                this.m_Buffer.Pointer = numPtr2;
                UnsafeUtility.Free((void*) pointer, Allocator.Persistent);
            }
        }

        public void Add(T elem)
        {
            this.CheckWriteAccess();
            int length = this.Length;
            this.ResizeUninitialized(length + 1);
            this.set_Item(length, elem);
        }

        public unsafe void Insert(int index, T elem)
        {
            this.CheckWriteAccess();
            int length = this.Length;
            this.ResizeUninitialized(length + 1);
            this.CheckBounds(index);
            int num2 = UnsafeUtility.SizeOf<T>();
            byte* elementPointer = BufferHeader.GetElementPointer(this.m_Buffer);
            UnsafeUtility.MemMove((void*) (elementPointer + ((index + 1) * num2)), (void*) (elementPointer + (index * num2)), (long) (num2 * (length - index)));
            this.set_Item(index, elem);
        }

        public unsafe void AddRange(NativeArray<T> newElems)
        {
            this.CheckWriteAccess();
            int num = UnsafeUtility.SizeOf<T>();
            int length = this.Length;
            this.ResizeUninitialized(length + newElems.Length);
            UnsafeUtility.MemCpy((void*) (BufferHeader.GetElementPointer(this.m_Buffer) + (length * num)), newElems.GetUnsafeReadOnlyPtr<T>(), (long) (num * newElems.Length));
        }

        public unsafe void RemoveRange(int index, int count)
        {
            this.CheckWriteAccess();
            this.CheckBounds((index + count) - 1);
            int num = UnsafeUtility.SizeOf<T>();
            byte* elementPointer = BufferHeader.GetElementPointer(this.m_Buffer);
            UnsafeUtility.MemMove((void*) (elementPointer + (index * num)), (void*) (elementPointer + ((index + count) * num)), (long) (num * ((this.Length - count) - index)));
            int* numPtr1 = (int*) ref this.m_Buffer.Length;
            numPtr1[0] -= count;
        }

        public void RemoveAt(int index)
        {
            this.RemoveRange(index, 1);
        }

        public unsafe byte* GetBasePointer()
        {
            this.CheckWriteAccess();
            return ref BufferHeader.GetElementPointer(this.m_Buffer);
        }

        public unsafe DynamicBuffer<U> Reinterpret<U>() where U: struct
        {
            if (UnsafeUtility.SizeOf<U>() != UnsafeUtility.SizeOf<T>())
            {
                throw new InvalidOperationException($"Types {typeof(U)} and {typeof(T)} are of different sizes; cannot reinterpret");
            }
            return new DynamicBuffer<U>(this.m_Buffer, this.m_Safety0, this.m_Safety1, this.m_IsReadOnly);
        }

        public unsafe NativeArray<T> ToNativeArray()
        {
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*) this.GetBasePointer(), this.Length, Allocator.Invalid);
            AtomicSafetyHandle handle = this.m_Safety1;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref array, handle);
            return array;
        }

        public void CopyFrom(NativeArray<T> v)
        {
            this.ResizeUninitialized(v.Length);
            this.ToNativeArray().CopyFrom(v);
        }

        public void CopyFrom(T[] v)
        {
            this.ResizeUninitialized(v.Length);
            this.ToNativeArray().CopyFrom(v);
        }
    }
}

