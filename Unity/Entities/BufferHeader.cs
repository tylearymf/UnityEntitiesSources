namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BufferHeader
    {
        public const int kMinimumCapacity = 8;
        public unsafe byte* Pointer;
        public int Length;
        public int Capacity;
        public static unsafe byte* GetElementPointer(BufferHeader* header)
        {
            byte* pointer;
            if (header.Pointer != null)
            {
                pointer = header.Pointer;
            }
            else
            {
                pointer = (byte*) (header + 1);
            }
            return pointer;
        }

        public static unsafe void EnsureCapacity(BufferHeader* header, int count, int typeSize, int alignment, TrashMode trashMode)
        {
            if (header.Capacity < count)
            {
                int num = Math.Max(Math.Max(2 * header.Capacity, count), 8);
                byte* numPtr = ref GetElementPointer(header);
                byte* numPtr2 = (byte*) ref UnsafeUtility.Malloc((long) (num * typeSize), alignment, Allocator.Persistent);
                if (trashMode == TrashMode.RetainOldData)
                {
                    UnsafeUtility.MemCpy((void*) numPtr2, (void*) numPtr, (long) (header.Capacity * typeSize));
                }
                if (header.Pointer != null)
                {
                    UnsafeUtility.Free((void*) header.Pointer, Allocator.Persistent);
                }
                header.Pointer = numPtr2;
                header.Capacity = num;
            }
        }

        public static unsafe void Assign(BufferHeader* header, byte* source, int count, int typeSize, int alignment)
        {
            EnsureCapacity(header, count, typeSize, alignment, TrashMode.TrashOldData);
            UnsafeUtility.MemCpy((void*) GetElementPointer(header), (void*) source, (long) (typeSize * count));
            header.Length = count;
        }

        public static unsafe void Initialize(BufferHeader* header, int bufferCapacity)
        {
            header.Pointer = null;
            header.Length = 0;
            header.Capacity = bufferCapacity;
        }

        public static unsafe void Destroy(BufferHeader* header)
        {
            if (header.Pointer != null)
            {
                UnsafeUtility.Free((void*) header.Pointer, Allocator.Persistent);
            }
            Initialize(header, 0);
        }
        public enum TrashMode
        {
            TrashOldData,
            RetainOldData
        }
    }
}

