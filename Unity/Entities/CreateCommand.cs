namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateCommand
    {
        public BasicCommand Header;
        public EntityArchetype Archetype;
        public int BatchCount;
    }
}

