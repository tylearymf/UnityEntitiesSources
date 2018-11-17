namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class EntityRemapUtility
    {
        public static void AddEntityRemapping(ref NativeArray<EntityRemapInfo> remapping, Entity source, Entity target)
        {
            EntityRemapInfo info = new EntityRemapInfo {
                SourceVersion = source.Version,
                Target = target
            };
            remapping.set_Item(source.Index, info);
        }

        public static unsafe BufferEntityPatchInfo* AppendBufferEntityPatches(BufferEntityPatchInfo* patches, TypeManager.EntityOffsetInfo[] offsets, int bufferBaseOffset, int bufferStride, int elementStride)
        {
            BufferEntityPatchInfo* infoPtr;
            if (offsets == null)
            {
                infoPtr = patches;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index >= offsets.Length)
                    {
                        infoPtr = patches + offsets.Length;
                        break;
                    }
                    patches[index] = new BufferEntityPatchInfo { 
                        BufferOffset = bufferBaseOffset,
                        BufferStride = bufferStride,
                        ElementOffset = offsets[index].Offset,
                        ElementStride = elementStride
                    };
                    index++;
                }
            }
            return infoPtr;
        }

        public static unsafe EntityPatchInfo* AppendEntityPatches(EntityPatchInfo* patches, TypeManager.EntityOffsetInfo[] offsets, int baseOffset, int stride)
        {
            EntityPatchInfo* infoPtr;
            if (offsets == null)
            {
                infoPtr = patches;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index >= offsets.Length)
                    {
                        infoPtr = patches + offsets.Length;
                        break;
                    }
                    patches[index] = new EntityPatchInfo { 
                        Offset = baseOffset + offsets[index].Offset,
                        Stride = stride
                    };
                    index++;
                }
            }
            return infoPtr;
        }

        public static TypeManager.EntityOffsetInfo[] CalculateEntityOffsets(Type type)
        {
            TypeManager.EntityOffsetInfo[] infoArray;
            List<TypeManager.EntityOffsetInfo> offsets = new List<TypeManager.EntityOffsetInfo>();
            CalculateEntityOffsetsRecurse(ref offsets, type, 0);
            if (offsets.Count > 0)
            {
                infoArray = offsets.ToArray();
            }
            else
            {
                infoArray = null;
            }
            return infoArray;
        }

        private static void CalculateEntityOffsetsRecurse(ref List<TypeManager.EntityOffsetInfo> offsets, Type type, int baseOffset)
        {
            if (type == typeof(Entity))
            {
                TypeManager.EntityOffsetInfo item = new TypeManager.EntityOffsetInfo {
                    Offset = baseOffset
                };
                offsets.Add(item);
            }
            else
            {
                foreach (FieldInfo info2 in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (info2.FieldType.IsValueType && !info2.FieldType.IsPrimitive)
                    {
                        CalculateEntityOffsetsRecurse(ref offsets, info2.FieldType, baseOffset + UnsafeUtility.GetFieldOffset(info2));
                    }
                }
            }
        }

        public static unsafe void PatchEntities(EntityPatchInfo* scalarPatches, int scalarPatchCount, BufferEntityPatchInfo* bufferPatches, int bufferPatchCount, byte* data, int count, ref NativeArray<EntityRemapInfo> remapping)
        {
            int index = 0;
            while (true)
            {
                if (index >= scalarPatchCount)
                {
                    int num3 = 0;
                    while (num3 < bufferPatchCount)
                    {
                        byte* numPtr2 = data + bufferPatches[num3].BufferOffset;
                        int num4 = 0;
                        while (true)
                        {
                            if (num4 == count)
                            {
                                num3++;
                                break;
                            }
                            BufferHeader* header = (BufferHeader*) numPtr2;
                            byte* numPtr3 = BufferHeader.GetElementPointer(header) + bufferPatches[num3].ElementOffset;
                            int length = header->Length;
                            int num6 = 0;
                            while (true)
                            {
                                if (num6 == length)
                                {
                                    numPtr2 += bufferPatches[num3].BufferStride;
                                    num4++;
                                    break;
                                }
                                Entity* entityPtr2 = (Entity*) numPtr3;
                                entityPtr2[0] = RemapEntity(ref remapping, entityPtr2[0]);
                                numPtr3 += bufferPatches[num3].ElementStride;
                                num6++;
                            }
                        }
                    }
                    return;
                }
                byte* numPtr = data + scalarPatches[index].Offset;
                int num2 = 0;
                while (true)
                {
                    if (num2 == count)
                    {
                        index++;
                        break;
                    }
                    Entity* entityPtr = (Entity*) numPtr;
                    entityPtr[0] = RemapEntity(ref remapping, entityPtr[0]);
                    numPtr += scalarPatches[index].Stride;
                    num2++;
                }
            }
        }

        public static Entity RemapEntity(ref NativeArray<EntityRemapInfo> remapping, Entity source)
        {
            Entity target;
            if (source.Version == remapping[source.Index].SourceVersion)
            {
                target = remapping[source.Index].Target;
            }
            else
            {
                target = Entity.Null;
            }
            return target;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BufferEntityPatchInfo
        {
            public int BufferOffset;
            public int BufferStride;
            public int ElementOffset;
            public int ElementStride;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EntityPatchInfo
        {
            public int Offset;
            public int Stride;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EntityRemapInfo
        {
            public int SourceVersion;
            public Entity Target;
        }
    }
}

