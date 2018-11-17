namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicCommand
    {
        public int CommandType;
        public int TotalSize;
        public int SortIndex;
    }
}

