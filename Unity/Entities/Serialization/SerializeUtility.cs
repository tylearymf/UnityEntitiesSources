namespace Unity.Entities.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class SerializeUtility
    {
        public static int CurrentFileFormatVersion = 7;

        private static unsafe void ClearUnusedChunkData(Chunk* chunk)
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
                    UnsafeUtility.MemClear((void*) (numPtr + num4), (long) (chunkBufferSize - num4));
                    return;
                }
                int num6 = archetype->TypeMemoryOrder[index];
                int num7 = archetype->TypeMemoryOrder[index + 1];
                int num8 = archetype->Offsets[num6] + (count * archetype->SizeOfs[num6]);
                int num9 = archetype->Offsets[num7];
                UnsafeUtility.MemClear((void*) (numPtr + num8), (long) (num9 - num8));
                index++;
            }
        }

        public static unsafe void DeserializeWorld(ExclusiveEntityTransaction manager, BinaryReader reader, int numSharedComponents)
        {
            int num2;
            if (manager.ArchetypeManager.CountEntities() != 0)
            {
                throw new ArgumentException("DeserializeWorld can only be used on completely empty EntityManager. Please create a new empty World and use EntityManager.MoveEntitiesFrom to move the loaded entities into the destination world instead.");
            }
            int num = reader.ReadInt();
            if (num != CurrentFileFormatVersion)
            {
                throw new ArgumentException($"Attempting to read a entity scene stored in an old file format version (stored version : {num}, current version : {CurrentFileFormatVersion})");
            }
            NativeArray<int> types = ReadTypeArray(reader);
            NativeArray<EntityArchetype> array2 = ReadArchetypes(reader, types, manager, out num2);
            manager.AllocateConsecutiveEntitiesForLoading(num2);
            int num3 = reader.ReadInt();
            int num4 = 0;
            while (true)
            {
                if (num4 >= num3)
                {
                    array2.Dispose();
                    return;
                }
                Chunk* chunk = (Chunk*) UnsafeUtility.Malloc(0x3f00L, 0x40, Allocator.Persistent);
                reader.ReadBytes((void*) chunk, 0x3f00);
                chunk->Archetype = array2[(int) chunk->Archetype].Archetype;
                chunk->SharedComponentValueArray = (int*) (chunk + Chunk.GetSharedComponentOffset(chunk->Archetype.NumSharedComponents));
                int num5 = chunk->Archetype.NumSharedComponents;
                int index = 0;
                while (true)
                {
                    if (index >= num5)
                    {
                        chunk->ChangeVersion = (uint*) (chunk + Chunk.GetChangedComponentOffset(chunk->Archetype.TypesCount, chunk->Archetype.NumSharedComponents));
                        int length = reader.ReadInt();
                        if (length > 0)
                        {
                            NativeArray<BufferPatchRecord> elements = new NativeArray<BufferPatchRecord>(length, Allocator.Temp, NativeArrayOptions.ClearMemory);
                            reader.ReadArray<BufferPatchRecord>(elements, elements.Length);
                            int num8 = 0;
                            while (true)
                            {
                                if (num8 >= length)
                                {
                                    elements.Dispose();
                                    break;
                                }
                                BufferHeader* headerPtr = (BufferHeader*) ref OffsetFromPointer((void*) &chunk->Buffer.FixedElementField, elements[num8].ChunkOffset);
                                headerPtr->Pointer = (byte*) UnsafeUtility.Malloc((long) elements[num8].AllocSizeBytes, 8, Allocator.Persistent);
                                reader.ReadBytes((void*) headerPtr->Pointer, elements[num8].AllocSizeBytes);
                                num8++;
                            }
                        }
                        manager.AddExistingChunk(chunk);
                        num4++;
                        break;
                    }
                    if (chunk->SharedComponentValueArray[index] > numSharedComponents)
                    {
                        throw new ArgumentException($"Archetype uses shared component at index {chunk->SharedComponentValueArray[index]} but only {numSharedComponents} are available, check if the shared scene has been properly loaded.");
                    }
                    index++;
                }
            }
        }

        private static unsafe int GenerateRemapInfo(EntityManager entityManager, EntityArchetype[] archetypeArray, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            int num = 1;
            int num2 = 0;
            int index = 0;
            while (index < archetypeArray.Length)
            {
                Archetype* archetype = archetypeArray[index].Archetype;
                Chunk* begin = (Chunk*) archetype->ChunkList.Begin;
                while (true)
                {
                    if (begin == archetype->ChunkList.End)
                    {
                        index++;
                        break;
                    }
                    int num4 = 0;
                    while (true)
                    {
                        if (num4 >= begin->Count)
                        {
                            num2++;
                            begin = (Chunk*) begin->ChunkListNode.Next;
                            break;
                        }
                        Entity source = *((Entity*) ChunkDataUtility.GetComponentDataRO(begin, num4, 0));
                        Entity target = new Entity {
                            Version = 0,
                            Index = num
                        };
                        EntityRemapUtility.AddEntityRemapping(ref entityRemapInfos, source, target);
                        num++;
                        num4++;
                    }
                }
            }
            return num2;
        }

        internal static unsafe void GetAllArchetypes(ArchetypeManager archetypeManager, out Dictionary<EntityArchetype, int> archetypeToIndex, out EntityArchetype[] archetypeArray)
        {
            List<EntityArchetype> list = new List<EntityArchetype>();
            Archetype* lastArchetype = archetypeManager.m_LastArchetype;
            while (true)
            {
                if (lastArchetype == null)
                {
                    archetypeToIndex = new Dictionary<EntityArchetype, int>();
                    int num = 0;
                    while (true)
                    {
                        if (num >= list.Count)
                        {
                            archetypeArray = list.ToArray();
                            return;
                        }
                        archetypeToIndex.Add(list[num], num);
                        num++;
                    }
                }
                if (lastArchetype->EntityCount >= 0)
                {
                    EntityArchetype item = new EntityArchetype {
                        Archetype = lastArchetype
                    };
                    list.Add(item);
                }
                lastArchetype = lastArchetype->PrevArchetype;
            }
        }

        private static unsafe byte* OffsetFromPointer(void* ptr, int offset) => 
            ((byte*) (ptr + offset));

        private static unsafe NativeArray<EntityArchetype> ReadArchetypes(BinaryReader reader, NativeArray<int> types, ExclusiveEntityTransaction entityManager, out int totalEntityCount)
        {
            int length = reader.ReadInt();
            NativeArray<EntityArchetype> array = new NativeArray<EntityArchetype>(length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            NativeArray<int> array2 = new NativeArray<int>(length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            totalEntityCount = 0;
            NativeList<ComponentType> nativeList = new NativeList<ComponentType>(Allocator.Temp);
            int index = 0;
            while (true)
            {
                int num4;
                if (index >= length)
                {
                    nativeList.Dispose();
                    types.Dispose();
                    array2.Dispose();
                    return array;
                }
                array2.set_Item(index, num4 = reader.ReadInt());
                totalEntityCount += num4;
                int num3 = reader.ReadInt();
                nativeList.Clear();
                int num5 = 0;
                while (true)
                {
                    if (num5 >= num3)
                    {
                        array.set_Item(index, entityManager.CreateArchetype((ComponentType*) nativeList.GetUnsafePtr<ComponentType>(), nativeList.Length));
                        index++;
                        break;
                    }
                    int typeIndex = types[reader.ReadInt()];
                    nativeList.Add(ComponentType.FromTypeIndex(typeIndex));
                    num5++;
                }
            }
        }

        private static NativeArray<int> ReadTypeArray(BinaryReader reader)
        {
            int length = reader.ReadInt();
            NativeArray<int> elements = new NativeArray<int>(length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            reader.ReadArray<int>(elements, length);
            int num2 = reader.ReadInt();
            NativeArray<byte> array2 = new NativeArray<byte>(num2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            reader.ReadBytes(array2, num2, 0);
            NativeArray<int> array3 = new NativeArray<int>(length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            int offset = 0;
            int index = 0;
            while (true)
            {
                if (index >= length)
                {
                    array2.Dispose();
                    elements.Dispose();
                    return array3;
                }
                string typeName = StringFromNativeBytes(array2, offset);
                Type type = Type.GetType(typeName);
                array3.set_Item(index, TypeManager.GetTypeIndex(type));
                if (elements[index] != TypeManager.GetTypeInfo(array3[index]).FastEqualityTypeInfo.Hash)
                {
                    throw new ArgumentException($"Type layout has changed: '{type.Name}'");
                }
                if (array3[index] == 0)
                {
                    throw new ArgumentException("Unknown type '" + typeName + "'");
                }
                offset += typeName.Length + 1;
                index++;
            }
        }

        public static void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out int[] sharedComponentsToSerialize)
        {
            NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(entityManager.EntityCapacity, Allocator.Temp, NativeArrayOptions.ClearMemory);
            SerializeWorld(entityManager, writer, out sharedComponentsToSerialize, entityRemapInfos);
            entityRemapInfos.Dispose();
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out int[] sharedComponentsToSerialize, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            Dictionary<EntityArchetype, int> dictionary;
            EntityArchetype[] archetypeArray;
            writer.Write(CurrentFileFormatVersion);
            GetAllArchetypes(entityManager.ArchetypeManager, out dictionary, out archetypeArray);
            HashSet<int> source = new HashSet<int>();
            EntityArchetype[] archetypeArray2 = archetypeArray;
            int index = 0;
            while (index < archetypeArray2.Length)
            {
                EntityArchetype archetype = archetypeArray2[index];
                int num4 = 0;
                while (true)
                {
                    if (num4 >= archetype.Archetype.TypesCount)
                    {
                        index++;
                        break;
                    }
                    source.Add(archetype.Archetype.Types[num4].TypeIndex);
                    num4++;
                }
            }
            var typeArray = (from t in source.Select(delegate (int index) {
                Type type = TypeManager.GetType(index);
                string assemblyQualifiedName = TypeManager.GetType(index).AssemblyQualifiedName;
                return new { 
                    index = index,
                    type = type,
                    name = assemblyQualifiedName,
                    hash = TypeManager.GetTypeInfo(index).FastEqualityTypeInfo.Hash,
                    asciiName = Encoding.ASCII.GetBytes(assemblyQualifiedName)
                };
            })
                orderby t.name
                select t).ToArray();
            int num = typeArray.Sum(t => t.asciiName.Length + 1);
            writer.Write(typeArray.Length);
            foreach (var type in typeArray)
            {
                writer.Write(type.hash);
            }
            writer.Write(num);
            foreach (var type2 in typeArray)
            {
                writer.Write(type2.asciiName);
                writer.Write((byte) 0);
            }
            Dictionary<int, int> typeIndexMap = new Dictionary<int, int>();
            int num7 = 0;
            while (true)
            {
                if (num7 >= typeArray.Length)
                {
                    WriteArchetypes(writer, archetypeArray, typeIndexMap);
                    NativeList<BufferPatchRecord> data = new NativeList<BufferPatchRecord>(0x80, Allocator.Temp);
                    int num2 = GenerateRemapInfo(entityManager, archetypeArray, entityRemapInfos);
                    writer.Write(num2);
                    Chunk* chunk = (Chunk*) UnsafeUtility.Malloc(0x3f00L, 0x10, Allocator.Temp);
                    Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
                    int num8 = 0;
                    while (true)
                    {
                        if (num8 >= archetypeArray.Length)
                        {
                            data.Dispose();
                            UnsafeUtility.Free((void*) chunk, Allocator.Temp);
                            sharedComponentsToSerialize = new int[dictionary3.Count];
                            foreach (KeyValuePair<int, int> pair in dictionary3)
                            {
                                sharedComponentsToSerialize[pair.Value - 1] = pair.Key;
                            }
                            return;
                        }
                        Archetype* archetype = archetypeArray[num8].Archetype;
                        Chunk* begin = (Chunk*) archetype->ChunkList.Begin;
                        while (true)
                        {
                            if (begin == archetype->ChunkList.End)
                            {
                                num8++;
                                break;
                            }
                            data.Clear();
                            UnsafeUtility.MemCpy((void*) chunk, (void*) begin, 0x3f00L);
                            chunk->SharedComponentValueArray = (int*) (chunk + Chunk.GetSharedComponentOffset(archetype->NumSharedComponents));
                            byte* numPtr = &chunk->Buffer.FixedElementField;
                            EntityRemapUtility.PatchEntities(archetype->ScalarEntityPatches, archetype->ScalarEntityPatchCount, archetype->BufferEntityPatches, archetype->BufferEntityPatchCount, numPtr, chunk->Count, ref entityRemapInfos);
                            int num9 = 0;
                            while (true)
                            {
                                if (num9 >= archetype->TypesCount)
                                {
                                    ClearUnusedChunkData(chunk);
                                    chunk->ChunkListNode.Next = null;
                                    chunk->ChunkListNode.Prev = null;
                                    chunk->ChunkListWithEmptySlotsNode.Next = null;
                                    chunk->ChunkListWithEmptySlotsNode.Prev = null;
                                    chunk->Archetype = (Archetype*) num8;
                                    if (archetype->NumManagedArrays != 0)
                                    {
                                        throw new ArgumentException("Serialization of GameObject components is not supported for pure entity scenes");
                                    }
                                    int num15 = 0;
                                    while (true)
                                    {
                                        if (num15 == archetype->NumSharedComponents)
                                        {
                                            writer.WriteBytes((void*) chunk, 0x3f00);
                                            writer.Write(data.Length);
                                            if (data.Length > 0)
                                            {
                                                writer.WriteList<BufferPatchRecord>(data);
                                                int num18 = 0;
                                                while (true)
                                                {
                                                    if (num18 >= data.Length)
                                                    {
                                                        break;
                                                    }
                                                    BufferPatchRecord record2 = data[num18];
                                                    BufferHeader* headerPtr2 = (BufferHeader*) ref OffsetFromPointer((void*) &begin->Buffer.FixedElementField, record2.ChunkOffset);
                                                    writer.WriteBytes((void*) headerPtr2->Pointer, record2.AllocSizeBytes);
                                                    num18++;
                                                }
                                            }
                                            begin = (Chunk*) begin->ChunkListNode.Next;
                                            break;
                                        }
                                        int key = chunk->SharedComponentValueArray[num15];
                                        if (chunk->SharedComponentValueArray[num15] != 0)
                                        {
                                            int num17;
                                            if (dictionary3.TryGetValue(key, out num17))
                                            {
                                                chunk->SharedComponentValueArray[num15] = num17;
                                            }
                                            else
                                            {
                                                num17 = dictionary3.Count + 1;
                                                dictionary3.set_Item(key, num17);
                                                chunk->SharedComponentValueArray[num15] = num17;
                                            }
                                        }
                                        num15++;
                                    }
                                    break;
                                }
                                int num10 = archetype->TypeMemoryOrder[num9];
                                if ((archetype->Types + num10).IsBuffer)
                                {
                                    BufferHeader* headerPtr = (BufferHeader*) OffsetFromPointer((void*) numPtr, archetype->Offsets[num10]);
                                    int offset = archetype->SizeOfs[num10];
                                    int count = begin->Count;
                                    TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo(archetype->Types[num10].TypeIndex);
                                    int num14 = 0;
                                    while (true)
                                    {
                                        if (num14 >= count)
                                        {
                                            break;
                                        }
                                        if (headerPtr->Pointer != null)
                                        {
                                            headerPtr->Pointer = null;
                                            BufferPatchRecord element = new BufferPatchRecord {
                                                ChunkOffset = (int) ((long) ((headerPtr - numPtr) / 1)),
                                                AllocSizeBytes = typeInfo.ElementSize * headerPtr->Capacity
                                            };
                                            data.Add(element);
                                        }
                                        headerPtr = (BufferHeader*) OffsetFromPointer((void*) headerPtr, offset);
                                        num14++;
                                    }
                                }
                                num9++;
                            }
                        }
                    }
                }
                typeIndexMap.set_Item(typeArray[num7].index, num7);
                num7++;
            }
        }

        private static unsafe string StringFromNativeBytes(NativeArray<byte> bytes, int offset = 0) => 
            new string((sbyte*) (bytes.GetUnsafePtr<byte>() + offset));

        private static unsafe void WriteArchetypes(BinaryWriter writer, EntityArchetype[] archetypeArray, Dictionary<int, int> typeIndexMap)
        {
            writer.Write(archetypeArray.Length);
            EntityArchetype[] archetypeArray2 = archetypeArray;
            int index = 0;
            while (index < archetypeArray2.Length)
            {
                EntityArchetype archetype = archetypeArray2[index];
                writer.Write(archetype.Archetype.EntityCount);
                writer.Write((int) (archetype.Archetype.TypesCount - 1));
                int num2 = 1;
                while (true)
                {
                    if (num2 >= archetype.Archetype.TypesCount)
                    {
                        index++;
                        break;
                    }
                    ComponentTypeInArchetype archetype2 = archetype.Archetype.Types[num2];
                    writer.Write(typeIndexMap[archetype2.TypeIndex]);
                    num2++;
                }
            }
        }

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly SerializeUtility.<>c <>9 = new SerializeUtility.<>c();
            public static Func<int, <>f__AnonymousType0<int, Type, string, int, byte[]>> <>9__8_0;
            public static Func<<>f__AnonymousType0<int, Type, string, int, byte[]>, string> <>9__8_1;
            public static Func<<>f__AnonymousType0<int, Type, string, int, byte[]>, int> <>9__8_2;

            internal <>f__AnonymousType0<int, Type, string, int, byte[]> <SerializeWorld>b__8_0(int index)
            {
                Type type = TypeManager.GetType(index);
                string assemblyQualifiedName = TypeManager.GetType(index).AssemblyQualifiedName;
                return new { 
                    index = index,
                    type = type,
                    name = assemblyQualifiedName,
                    hash = TypeManager.GetTypeInfo(index).FastEqualityTypeInfo.Hash,
                    asciiName = Encoding.ASCII.GetBytes(assemblyQualifiedName)
                };
            }

            internal string <SerializeWorld>b__8_1(<>f__AnonymousType0<int, Type, string, int, byte[]> t) => 
                t.name;

            internal int <SerializeWorld>b__8_2(<>f__AnonymousType0<int, Type, string, int, byte[]> t) => 
                (t.asciiName.Length + 1);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BufferPatchRecord
        {
            public int ChunkOffset;
            public int AllocSizeBytes;
        }
    }
}

