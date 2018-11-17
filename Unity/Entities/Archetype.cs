namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Archetype
    {
        public UnsafeLinkedListNode ChunkList;
        public UnsafeLinkedListNode ChunkListWithEmptySlots;
        public ChunkListMap FreeChunksBySharedComponents;
        public int EntityCount;
        public int ChunkCapacity;
        public int ChunkCount;
        public unsafe ComponentTypeInArchetype* Types;
        public int TypesCount;
        public unsafe int* Offsets;
        public unsafe int* SizeOfs;
        public unsafe int* TypeMemoryOrder;
        public unsafe int* ManagedArrayOffset;
        public int NumManagedArrays;
        public unsafe int* SharedComponentOffset;
        public int NumSharedComponents;
        public unsafe Archetype* PrevArchetype;
        public unsafe Archetype* InstantiableArchetype;
        public unsafe Archetype* SystemStateResidueArchetype;
        public unsafe EntityRemapUtility.EntityPatchInfo* ScalarEntityPatches;
        public int ScalarEntityPatchCount;
        public unsafe EntityRemapUtility.BufferEntityPatchInfo* BufferEntityPatches;
        public int BufferEntityPatchCount;
        public bool SystemStateCleanupComplete;
        public bool SystemStateCleanupNeeded;
        public bool Disabled;
        public bool Prefab;
    }
}

