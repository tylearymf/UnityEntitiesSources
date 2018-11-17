namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityCommand
    {
        public BasicCommand Header;
        public Unity.Entities.Entity Entity;
        public int BatchCount;
    }
}

