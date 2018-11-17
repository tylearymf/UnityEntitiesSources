namespace Unity.Entities
{
    using System;
    using Unity.Assertions;
    using Unity.Collections.LowLevel.Unsafe;

    internal static class ChunkDataUtility
    {
        public static unsafe void ClearManagedObjects(ArchetypeManager typeMan, Chunk* chunk, int index, int count)
        {
            Archetype* archetype = chunk.Archetype;
            for (int i = 0; i < archetype->TypesCount; i++)
            {
                if (archetype->ManagedArrayOffset[i] >= 0)
                {
                    int num2 = 0;
                    while (true)
                    {
                        if (num2 >= count)
                        {
                            break;
                        }
                        typeMan.SetManagedObject(chunk, i, index + num2, null);
                        num2++;
                    }
                }
            }
        }

        public static unsafe void Convert(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstIndex)
        {
            Archetype* archetype = srcChunk.Archetype;
            Archetype* archetypePtr2 = dstChunk.Archetype;
            int index = 0;
            int num2 = 0;
            while (true)
            {
                if ((index >= archetype->TypesCount) || (num2 >= archetypePtr2->TypesCount))
                {
                    while (true)
                    {
                        if (index >= archetype->TypesCount)
                        {
                            while (num2 < archetypePtr2->TypesCount)
                            {
                                byte* numPtr4 = (&dstChunk.Buffer.FixedElementField + archetypePtr2->Offsets[num2]) + (dstIndex * archetypePtr2->SizeOfs[num2]);
                                if ((archetypePtr2->Types + num2).IsBuffer)
                                {
                                    BufferHeader.Initialize((BufferHeader*) numPtr4, archetypePtr2->Types[num2].BufferCapacity);
                                }
                                else
                                {
                                    UnsafeUtility.MemClear((void*) numPtr4, (long) archetypePtr2->SizeOfs[num2]);
                                }
                                num2++;
                            }
                            return;
                        }
                        byte* numPtr3 = (&srcChunk.Buffer.FixedElementField + archetype->Offsets[index]) + (srcIndex * archetype->SizeOfs[index]);
                        if ((archetype->Types + index).IsBuffer)
                        {
                            BufferHeader.Destroy((BufferHeader*) numPtr3);
                        }
                        index++;
                    }
                }
                byte* numPtr = (&srcChunk.Buffer.FixedElementField + archetype->Offsets[index]) + (srcIndex * archetype->SizeOfs[index]);
                byte* numPtr2 = (&dstChunk.Buffer.FixedElementField + archetypePtr2->Offsets[num2]) + (dstIndex * archetypePtr2->SizeOfs[num2]);
                if (archetype->Types[index] < archetypePtr2->Types[num2])
                {
                    if ((archetype->Types + index).IsBuffer)
                    {
                        BufferHeader.Destroy((BufferHeader*) numPtr);
                    }
                    index++;
                    continue;
                }
                if (archetype->Types[index] <= archetypePtr2->Types[num2])
                {
                    UnsafeUtility.MemCpy((void*) numPtr2, (void*) numPtr, (long) archetype->SizeOfs[index]);
                    if ((archetype->Types + index).IsBuffer)
                    {
                        BufferHeader.Initialize((BufferHeader*) numPtr, archetype->Types[index].BufferCapacity);
                    }
                    index++;
                    num2++;
                    continue;
                }
                if ((archetypePtr2->Types + num2).IsBuffer)
                {
                    BufferHeader.Initialize((BufferHeader*) numPtr2, archetypePtr2->Types[num2].BufferCapacity);
                }
                else
                {
                    UnsafeUtility.MemClear((void*) numPtr2, (long) archetypePtr2->SizeOfs[num2]);
                }
                num2++;
            }
        }

        public static unsafe void Copy(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstIndex, int count)
        {
            Assert.IsTrue(srcChunk.Archetype == dstChunk.Archetype);
            Archetype* archetype = srcChunk.Archetype;
            byte* numPtr = &srcChunk.Buffer.FixedElementField;
            byte* numPtr2 = &dstChunk.Buffer.FixedElementField;
            int* offsets = archetype->Offsets;
            int* sizeOfs = archetype->SizeOfs;
            int typesCount = archetype->TypesCount;
            for (int i = 0; i < typesCount; i++)
            {
                int num3 = offsets[i];
                int num4 = sizeOfs[i];
                byte* numPtr5 = numPtr + (num3 + (num4 * srcIndex));
                byte* numPtr6 = numPtr2 + (num3 + (num4 * dstIndex));
                dstChunk.ChangeVersion[i] = srcChunk.ChangeVersion[i];
                UnsafeUtility.MemCpy((void*) numPtr6, (void*) numPtr5, (long) (num4 * count));
            }
        }

        public static unsafe void CopyManagedObjects(ArchetypeManager typeMan, Chunk* srcChunk, int srcStartIndex, Chunk* dstChunk, int dstStartIndex, int count)
        {
            Archetype* archetype = srcChunk.Archetype;
            Archetype* archetypePtr2 = dstChunk.Archetype;
            int index = 0;
            int num2 = 0;
            while ((index < archetype->TypesCount) && (num2 < archetypePtr2->TypesCount))
            {
                if (archetype->Types[index] < archetypePtr2->Types[num2])
                {
                    index++;
                    continue;
                }
                if (archetype->Types[index] > archetypePtr2->Types[num2])
                {
                    num2++;
                    continue;
                }
                if (archetype->ManagedArrayOffset[index] >= 0)
                {
                    int num3 = 0;
                    while (true)
                    {
                        if (num3 >= count)
                        {
                            break;
                        }
                        object val = typeMan.GetManagedObject(srcChunk, index, srcStartIndex + num3);
                        typeMan.SetManagedObject(dstChunk, num2, dstStartIndex + num3, val);
                        num3++;
                    }
                }
                index++;
                num2++;
            }
        }

        public static unsafe byte* GetComponentDataRO(Chunk* chunk, int index, int indexInTypeArray)
        {
            int num = chunk.Archetype.Offsets[indexInTypeArray];
            return (&chunk.Buffer.FixedElementField + (num + (chunk.Archetype.SizeOfs[indexInTypeArray] * index)));
        }

        public static unsafe byte* GetComponentDataRW(Chunk* chunk, int index, int indexInTypeArray, uint globalSystemVersion)
        {
            int num = chunk.Archetype.Offsets[indexInTypeArray];
            chunk.ChangeVersion[indexInTypeArray] = globalSystemVersion;
            return (&chunk.Buffer.FixedElementField + (num + (chunk.Archetype.SizeOfs[indexInTypeArray] * index)));
        }

        public static unsafe byte* GetComponentDataWithTypeRO(Chunk* chunk, int index, int typeIndex)
        {
            int indexInTypeArray = GetIndexInTypeArray(chunk.Archetype, typeIndex);
            int num2 = chunk.Archetype.Offsets[indexInTypeArray];
            return (&chunk.Buffer.FixedElementField + (num2 + (chunk.Archetype.SizeOfs[indexInTypeArray] * index)));
        }

        public static unsafe byte* GetComponentDataWithTypeRO(Chunk* chunk, int index, int typeIndex, ref int typeLookupCache)
        {
            Archetype* archetype = chunk.Archetype;
            GetIndexInTypeArray(archetype, typeIndex, ref typeLookupCache);
            int num = typeLookupCache;
            int num2 = archetype->Offsets[num];
            return (&chunk.Buffer.FixedElementField + (num2 + (archetype->SizeOfs[num] * index)));
        }

        public static unsafe byte* GetComponentDataWithTypeRW(Chunk* chunk, int index, int typeIndex, uint globalSystemVersion)
        {
            int indexInTypeArray = GetIndexInTypeArray(chunk.Archetype, typeIndex);
            int num2 = chunk.Archetype.Offsets[indexInTypeArray];
            chunk.ChangeVersion[indexInTypeArray] = globalSystemVersion;
            return (&chunk.Buffer.FixedElementField + (num2 + (chunk.Archetype.SizeOfs[indexInTypeArray] * index)));
        }

        public static unsafe byte* GetComponentDataWithTypeRW(Chunk* chunk, int index, int typeIndex, uint globalSystemVersion, ref int typeLookupCache)
        {
            Archetype* archetype = chunk.Archetype;
            GetIndexInTypeArray(archetype, typeIndex, ref typeLookupCache);
            int num = typeLookupCache;
            int num2 = archetype->Offsets[num];
            chunk.ChangeVersion[num] = globalSystemVersion;
            return (&chunk.Buffer.FixedElementField + (num2 + (archetype->SizeOfs[num] * index)));
        }

        public static unsafe int GetIndexInTypeArray(Archetype* archetype, int typeIndex)
        {
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            int index = 0;
            while (true)
            {
                int num3;
                if (index == typesCount)
                {
                    num3 = -1;
                }
                else
                {
                    if (typeIndex != types[index].TypeIndex)
                    {
                        index++;
                        continue;
                    }
                    num3 = index;
                }
                return num3;
            }
        }

        public static unsafe void GetIndexInTypeArray(Archetype* archetype, int typeIndex, ref int typeLookupCache)
        {
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            if ((typeLookupCache >= typesCount) || (types[typeLookupCache].TypeIndex != typeIndex))
            {
                int index = 0;
                while (true)
                {
                    if (index == typesCount)
                    {
                        throw new InvalidOperationException("Shouldn't happen");
                    }
                    if (typeIndex == types[index].TypeIndex)
                    {
                        typeLookupCache = index;
                        break;
                    }
                    index++;
                }
            }
        }

        public static unsafe int GetSizeInChunk(Chunk* chunk, int typeIndex, ref int typeLookupCache)
        {
            Archetype* archetype = chunk.Archetype;
            GetIndexInTypeArray(archetype, typeIndex, ref typeLookupCache);
            int index = typeLookupCache;
            return archetype->SizeOfs[index];
        }

        public static unsafe void InitializeComponents(Chunk* dstChunk, int dstIndex, int count)
        {
            Archetype* archetype = dstChunk.Archetype;
            int* offsets = archetype->Offsets;
            int* sizeOfs = archetype->SizeOfs;
            byte* numPtr3 = &dstChunk.Buffer.FixedElementField;
            int typesCount = archetype->TypesCount;
            ComponentTypeInArchetype* types = archetype->Types;
            for (int i = 1; i != typesCount; i++)
            {
                int num3 = offsets[i];
                int num4 = sizeOfs[i];
                byte* numPtr4 = numPtr3 + (num3 + (num4 * dstIndex));
                if (!(types + i).IsBuffer)
                {
                    UnsafeUtility.MemClear((void*) numPtr4, (long) (num4 * count));
                }
                else
                {
                    int num5 = 0;
                    while (true)
                    {
                        if (num5 >= count)
                        {
                            break;
                        }
                        BufferHeader.Initialize((BufferHeader*) numPtr4, types[i].BufferCapacity);
                        numPtr4 += num4;
                        num5++;
                    }
                }
            }
        }

        public static unsafe void PoisonUnusedChunkData(Chunk* chunk, byte value)
        {
            Archetype* archetype = chunk.Archetype;
            int chunkBufferSize = Chunk.GetChunkBufferSize(archetype->TypesCount, archetype->NumSharedComponents);
            byte* numPtr = &chunk.Buffer.FixedElementField;
            int count = chunk.Count;
            int index = 0;
            while (true)
            {
                if (index >= (archetype->TypesCount - 1))
                {
                    int num3 = archetype->TypeMemoryOrder[archetype->TypesCount - 1];
                    int num4 = archetype->Offsets[num3] + (count * archetype->SizeOfs[num3]);
                    UnsafeUtilityEx.MemSet((void*) (numPtr + num4), value, chunkBufferSize - num4);
                    return;
                }
                int num6 = archetype->TypeMemoryOrder[index];
                int num7 = archetype->TypeMemoryOrder[index + 1];
                int num8 = archetype->Offsets[num6] + (count * archetype->SizeOfs[num6]);
                int num9 = archetype->Offsets[num7];
                UnsafeUtilityEx.MemSet((void*) (numPtr + num8), value, num9 - num8);
                index++;
            }
        }

        public static unsafe void ReplicateComponents(Chunk* srcChunk, int srcIndex, Chunk* dstChunk, int dstBaseIndex, int count)
        {
            Archetype* archetypePtr = srcChunk.Archetype;
            byte* numPtr = &srcChunk.Buffer.FixedElementField;
            byte* numPtr2 = &dstChunk.Buffer.FixedElementField;
            Archetype* archetypePtr2 = dstChunk.Archetype;
            int* offsets = archetypePtr->Offsets;
            int* sizeOfs = archetypePtr->SizeOfs;
            int typesCount = archetypePtr->TypesCount;
            ComponentTypeInArchetype* types = archetypePtr->Types;
            ComponentTypeInArchetype* archetypePtr4 = archetypePtr2->Types;
            int* numPtr5 = archetypePtr2->Offsets;
            int index = 1;
            for (int i = 1; i != typesCount; i++)
            {
                ComponentTypeInArchetype archetype = types[i];
                ComponentTypeInArchetype archetype2 = archetypePtr4[index];
                if (archetype.TypeIndex == archetype2.TypeIndex)
                {
                    int size = sizeOfs[i];
                    byte* numPtr6 = numPtr + (offsets[i] + (size * srcIndex));
                    byte* numPtr7 = numPtr2 + (numPtr5[index] + (size * dstBaseIndex));
                    if (!archetype.IsBuffer)
                    {
                        UnsafeUtility.MemCpyReplicate((void*) numPtr7, (void*) numPtr6, size, count);
                    }
                    else
                    {
                        int alignment = 8;
                        int elementSize = TypeManager.GetTypeInfo(archetype.TypeIndex).ElementSize;
                        int num9 = 0;
                        while (true)
                        {
                            if (num9 >= count)
                            {
                                break;
                            }
                            BufferHeader* header = (BufferHeader*) numPtr6;
                            BufferHeader* headerPtr2 = (BufferHeader*) numPtr7;
                            BufferHeader.Initialize(headerPtr2, archetype.BufferCapacity);
                            BufferHeader.Assign(headerPtr2, BufferHeader.GetElementPointer(header), header->Length, elementSize, alignment);
                            numPtr7 += size;
                            num9++;
                        }
                    }
                    index++;
                }
            }
        }
    }
}

