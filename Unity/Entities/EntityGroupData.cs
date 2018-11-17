namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityGroupData
    {
        public unsafe ComponentType* RequiredComponents;
        public int RequiredComponentsCount;
        public unsafe int* ReaderTypes;
        public int ReaderTypesCount;
        public unsafe int* WriterTypes;
        public int WriterTypesCount;
        public unsafe Unity.Entities.ArchetypeQuery* ArchetypeQuery;
        public int ArchetypeQueryCount;
        public unsafe MatchingArchetypes* FirstMatchingArchetype;
        public unsafe MatchingArchetypes* LastMatchingArchetype;
        public unsafe EntityGroupData* PrevGroup;
    }
}

