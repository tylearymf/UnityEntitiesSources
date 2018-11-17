namespace Unity.Entities.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    internal static class BinaryWriterExtensions
    {
        public static unsafe void Write(this BinaryWriter writer, byte value)
        {
            writer.WriteBytes((void*) &value, 1);
        }

        public static unsafe void Write(this BinaryWriter writer, int value)
        {
            writer.WriteBytes((void*) &value, 4);
        }

        public static unsafe void Write(this BinaryWriter writer, byte[] bytes)
        {
            byte* numPtr;
            byte[] pinned buffer;
            if (((buffer = bytes) == null) || (buffer.Length == 0))
            {
                numPtr = null;
            }
            else
            {
                numPtr = buffer;
            }
            writer.WriteBytes((void*) numPtr, bytes.Length);
            buffer = null;
        }

        public static unsafe void WriteArray<T>(this BinaryWriter writer, NativeArray<T> data) where T: struct
        {
            writer.WriteBytes(data.GetUnsafeReadOnlyPtr<T>(), data.Length * UnsafeUtility.SizeOf<T>());
        }

        public static unsafe void WriteList<T>(this BinaryWriter writer, NativeList<T> data) where T: struct
        {
            writer.WriteBytes(data.GetUnsafePtr<T>(), data.Length * UnsafeUtility.SizeOf<T>());
        }
    }
}

