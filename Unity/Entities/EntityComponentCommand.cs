namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityComponentCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;
        public int ComponentSize;
    }
}

