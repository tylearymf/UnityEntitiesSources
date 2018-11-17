namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    public static class FastEquality
    {
        private const int FNV_32_PRIME = 0x1000193;

        private static void CombineHash(ref int hash, params int[] values)
        {
            foreach (int num2 in values)
            {
                hash *= 0x1000193;
                hash ^= num2;
            }
        }

        private static void CreateLayoutRecurse(Type type, int baseOffset, List<Layout> layouts, ref int begin, ref int end, ref int typeHash)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            FieldData[] array = new FieldData[fields.Length];
            int index = 0;
            while (true)
            {
                if (index == fields.Length)
                {
                    Array.Sort<FieldData>(array, (a, b) => a.Offset - b.Offset);
                    foreach (FieldData data in array)
                    {
                        FieldInfo field = data.Field;
                        FixedBufferAttribute fixedBufferAttribute = GetFixedBufferAttribute(field);
                        int num3 = baseOffset + data.Offset;
                        if (fixedBufferAttribute != null)
                        {
                            int num4 = UnsafeUtility.SizeOf(fixedBufferAttribute.ElementType);
                            int num5 = 0;
                            while (true)
                            {
                                if (num5 >= fixedBufferAttribute.Length)
                                {
                                    break;
                                }
                                CreateLayoutRecurse(fixedBufferAttribute.ElementType, num3 + (num5 * num4), layouts, ref begin, ref end, ref typeHash);
                                num5++;
                            }
                        }
                        else
                        {
                            int isEnum;
                            if ((field.FieldType.IsPrimitive || field.FieldType.IsPointer) || field.FieldType.IsClass)
                            {
                                isEnum = 1;
                            }
                            else
                            {
                                isEnum = (int) field.FieldType.IsEnum;
                            }
                            if (isEnum == 0)
                            {
                                CreateLayoutRecurse(field.FieldType, num3, layouts, ref begin, ref end, ref typeHash);
                            }
                            else
                            {
                                int[] values = new int[] { num3, (int) Type.GetTypeCode(field.FieldType) };
                                CombineHash(ref typeHash, values);
                                int num6 = -1;
                                if (field.FieldType.IsPointer || field.FieldType.IsClass)
                                {
                                    num6 = UnsafeUtility.SizeOf<PointerSize>();
                                }
                                else if (field.FieldType.IsEnum)
                                {
                                    num6 = UnsafeUtility.SizeOf(field.FieldType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)[0].FieldType);
                                }
                                else
                                {
                                    num6 = UnsafeUtility.SizeOf(field.FieldType);
                                }
                                if (end == num3)
                                {
                                    end += num6;
                                }
                                else
                                {
                                    Layout item = new Layout {
                                        offset = begin,
                                        count = end - begin,
                                        Aligned4 = false
                                    };
                                    layouts.Add(item);
                                    begin = num3;
                                    end = num3 + num6;
                                }
                            }
                        }
                    }
                    return;
                }
                array[index].Offset = UnsafeUtility.GetFieldOffset(fields[index]);
                array[index].Field = fields[index];
                index++;
            }
        }

        internal static unsafe TypeInfo CreateTypeInfo(Type type)
        {
            int begin = 0;
            int end = 0;
            int typeHash = 0;
            List<Layout> layouts = new List<Layout>();
            CreateLayoutRecurse(type, 0, layouts, ref begin, ref end, ref typeHash);
            if (begin != end)
            {
                Layout item = new Layout {
                    offset = begin,
                    count = end - begin,
                    Aligned4 = false
                };
                layouts.Add(item);
            }
            Layout[] layoutArray = layouts.ToArray();
            for (int i = 0; i != layoutArray.Length; i++)
            {
                if (((layoutArray[i].count % 4) == 0) && ((layoutArray[i].offset % 4) == 0))
                {
                    int* numPtr1 = (int*) ref layoutArray[i].count;
                    numPtr1[0] /= 4;
                    layoutArray[i].Aligned4 = true;
                }
            }
            return new TypeInfo { 
                Layouts = layoutArray,
                Hash = typeHash
            };
        }

        public static unsafe bool Equals(void* lhsPtr, void* rhsPtr, TypeInfo typeInfo)
        {
            Layout[] layouts = typeInfo.Layouts;
            byte* numPtr = (byte*) lhsPtr;
            byte* numPtr2 = (byte*) rhsPtr;
            bool flag = true;
            for (int i = 0; i != layouts.Length; i++)
            {
                if (layouts[i].Aligned4)
                {
                    int offset = layouts[i].offset;
                    uint* numPtr3 = (uint*) (numPtr + offset);
                    uint* numPtr4 = (uint*) (numPtr2 + offset);
                    int count = layouts[i].count;
                    int index = 0;
                    while (true)
                    {
                        if (index == count)
                        {
                            break;
                        }
                        flag &= numPtr3[index] == numPtr4[index];
                        index++;
                    }
                }
                else
                {
                    int offset = layouts[i].offset;
                    byte* numPtr5 = numPtr + offset;
                    byte* numPtr6 = numPtr2 + offset;
                    int count = layouts[i].count;
                    int index = 0;
                    while (true)
                    {
                        if (index == count)
                        {
                            break;
                        }
                        flag &= numPtr5[index] == numPtr6[index];
                        index++;
                    }
                }
            }
            return flag;
        }

        public static unsafe bool Equals<T>(T lhs, T rhs, TypeInfo typeInfo) where T: struct => 
            Equals(UnsafeUtility.AddressOf<T>(ref lhs), UnsafeUtility.AddressOf<T>(ref rhs), typeInfo);

        public static unsafe bool Equals<T>(ref T lhs, ref T rhs, TypeInfo typeInfo) where T: struct => 
            Equals(UnsafeUtility.AddressOf<T>(ref lhs), UnsafeUtility.AddressOf<T>(ref rhs), typeInfo);

        private static FixedBufferAttribute GetFixedBufferAttribute(FieldInfo field)
        {
            using (IEnumerator<Attribute> enumerator = field.GetCustomAttributes(typeof(FixedBufferAttribute)).GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return (FixedBufferAttribute) enumerator.Current;
                }
            }
            return null;
        }

        public static unsafe int GetHashCode(void* dataPtr, TypeInfo typeInfo)
        {
            Layout[] layouts = typeInfo.Layouts;
            byte* numPtr = (byte*) dataPtr;
            uint num = 0;
            for (int i = 0; i != layouts.Length; i++)
            {
                if (layouts[i].Aligned4)
                {
                    uint* numPtr2 = (uint*) (numPtr + layouts[i].offset);
                    int count = layouts[i].count;
                    int index = 0;
                    while (true)
                    {
                        if (index == count)
                        {
                            break;
                        }
                        num = (num * 0x1000193) ^ numPtr2[index];
                        index++;
                    }
                }
                else
                {
                    byte* numPtr3 = numPtr + layouts[i].offset;
                    int count = layouts[i].count;
                    int index = 0;
                    while (true)
                    {
                        if (index == count)
                        {
                            break;
                        }
                        num = (num * 0x1000193) ^ numPtr3[index];
                        index++;
                    }
                }
            }
            return (int) num;
        }

        public static unsafe int GetHashCode<T>(T lhs, TypeInfo typeInfo) where T: struct => 
            GetHashCode(UnsafeUtility.AddressOf<T>(ref lhs), typeInfo);

        public static unsafe int GetHashCode<T>(ref T lhs, TypeInfo typeInfo) where T: struct => 
            GetHashCode(UnsafeUtility.AddressOf<T>(ref lhs), typeInfo);

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly FastEquality.<>c <>9 = new FastEquality.<>c();
            public static Comparison<FastEquality.FieldData> <>9__7_0;

            internal int <CreateLayoutRecurse>b__7_0(FastEquality.FieldData a, FastEquality.FieldData b) => 
                (a.Offset - b.Offset);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FieldData
        {
            public int Offset;
            public FieldInfo Field;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Layout
        {
            public int offset;
            public int count;
            public bool Aligned4;
            public override string ToString() => 
                $"offset: {this.offset} count: {this.count} Aligned4: {this.Aligned4}";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PointerSize
        {
            private unsafe void* pter;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TypeInfo
        {
            public FastEquality.Layout[] Layouts;
            public int Hash;
            public static FastEquality.TypeInfo Null =>
                new FastEquality.TypeInfo();
        }
    }
}

