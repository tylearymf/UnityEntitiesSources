namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityBufferCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;
        public int ComponentSize;
        public BufferHeader TempBuffer;
    }
}

