namespace Unity.Entities.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    internal static class BinaryReaderExtensions
    {
        public static unsafe void ReadArray<T>(this BinaryReader reader, NativeArray<T> elements, int count) where T: struct
        {
            reader.ReadBytes(elements.GetUnsafePtr<T>(), count * UnsafeUtility.SizeOf<T>());
        }

        public static unsafe byte ReadByte(this BinaryReader reader)
        {
            byte num;
            reader.ReadBytes((void*) &num, 1);
            return num;
        }

        public static unsafe void ReadBytes(this BinaryReader writer, NativeArray<byte> elements, int count, int offset = 0)
        {
            byte* numPtr = (byte*) (elements.GetUnsafePtr<byte>() + offset);
            writer.ReadBytes((void*) numPtr, count);
        }

        public static unsafe int ReadInt(this BinaryReader reader)
        {
            int num;
            reader.ReadBytes((void*) &num, 4);
            return num;
        }
    }
}

