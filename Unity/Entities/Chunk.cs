namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Chunk
    {
        public UnsafeLinkedListNode ChunkListNode;
        public UnsafeLinkedListNode ChunkListWithEmptySlotsNode;
        public unsafe Unity.Entities.Archetype* Archetype;
        public unsafe int* SharedComponentValueArray;
        public int Count;
        public int Capacity;
        public int ManagedArrayIndex;
        public int Padding0;
        public unsafe uint* ChangeVersion;
        public unsafe void* Padding2;
        [FixedBuffer(typeof(byte), 4)]
        public <Buffer>e__FixedBuffer Buffer;
        public const int kChunkSize = 0x3f00;
        public const int kMaximumEntitiesPerChunk = 0x7e0;
        public static int GetChunkBufferSize(int numComponents, int numSharedComponents) => 
            (0x3f00 - (((sizeof(Chunk) - 4) + (numSharedComponents * 4)) + (numComponents * 4)));

        public static int GetSharedComponentOffset(int numSharedComponents) => 
            (0x3f00 - (numSharedComponents * 4));

        public static int GetChangedComponentOffset(int numComponents, int numSharedComponents) => 
            (GetSharedComponentOffset(numSharedComponents) - (numComponents * 4));

        public unsafe bool MatchesFilter(MatchingArchetypes* match, ref ComponentGroupFilter filter)
        {
            bool flag3;
            if ((filter.Type & FilterType.SharedComponent) != FilterType.None)
            {
                int* sharedComponentValueArray = this.SharedComponentValueArray;
                int count = filter.Shared.Count;
                int* numPtr2 = &filter.Shared.IndexInComponentGroup.FixedElementField;
                int* numPtr3 = &filter.Shared.SharedComponentIndex.FixedElementField;
                int index = 0;
                while (true)
                {
                    if (index >= count)
                    {
                        fixed (int** numPtrRef = null)
                        {
                            fixed (int** numPtrRef2 = null)
                            {
                                flag3 = true;
                            }
                        }
                    }
                    else
                    {
                        int num3 = numPtr2[index];
                        int num4 = numPtr3[index];
                        int num5 = &match.IndexInArchetype.FixedElementField[num3];
                        int num6 = match.Archetype.SharedComponentOffset[num5];
                        if (sharedComponentValueArray[num6] == num4)
                        {
                            index++;
                            continue;
                        }
                        flag3 = false;
                    }
                    break;
                }
            }
            else if ((filter.Type & FilterType.Changed) == FilterType.None)
            {
                flag3 = true;
            }
            else
            {
                int count = filter.Changed.Count;
                uint requiredChangeVersion = filter.RequiredChangeVersion;
                int* numPtr4 = &filter.Changed.IndexInComponentGroup.FixedElementField;
                int index = 0;
                while (true)
                {
                    if (index >= count)
                    {
                        fixed (int** numPtrRef3 = null)
                        {
                            flag3 = false;
                        }
                    }
                    else
                    {
                        int num10 = &match.IndexInArchetype.FixedElementField[numPtr4[index]];
                        uint changeVersion = this.ChangeVersion[num10];
                        if (!ChangeVersionUtility.DidChange(changeVersion, requiredChangeVersion))
                        {
                            index++;
                            continue;
                        }
                        flag3 = true;
                    }
                    break;
                }
            }
            return flag3;
        }

        public unsafe int GetSharedComponentIndex(MatchingArchetypes* match, int indexInComponentGroup)
        {
            int index = &match.IndexInArchetype.FixedElementField[indexInComponentGroup];
            return this.SharedComponentValueArray[match.Archetype.SharedComponentOffset[index]];
        }
        [StructLayout(LayoutKind.Sequential, Size=4), CompilerGenerated, UnsafeValueType]
        public struct <Buffer>e__FixedBuffer
        {
            public byte FixedElementField;
        }
    }
}

