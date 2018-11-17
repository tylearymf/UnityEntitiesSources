namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArchetypeQuery
    {
        public unsafe int* Any;
        public int AnyCount;
        public unsafe int* All;
        public int AllCount;
        public unsafe int* None;
        public int NoneCount;
    }
}

