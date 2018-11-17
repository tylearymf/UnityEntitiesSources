namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MatchingArchetypes
    {
        public unsafe Unity.Entities.Archetype* Archetype;
        public unsafe MatchingArchetypes* Next;
        [FixedBuffer(typeof(int), 1)]
        public <IndexInArchetype>e__FixedBuffer IndexInArchetype;
        public static int GetAllocationSize(int requiredComponentsCount) => 
            (sizeof(MatchingArchetypes) + (4 * (requiredComponentsCount - 1)));
        [StructLayout(LayoutKind.Sequential, Size=4), CompilerGenerated, UnsafeValueType]
        public struct <IndexInArchetype>e__FixedBuffer
        {
            public int FixedElementField;
        }
    }
}

