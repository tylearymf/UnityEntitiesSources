namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using Unity.Collections.LowLevel.Unsafe;

    public static class TypeManager
    {
        public const int MaximumTypesCount = 0x2800;
        private static TypeInfo[] s_Types;
        private static volatile int s_Count;
        private static System.Threading.SpinLock s_CreateTypeLock;
        public static int ObjectOffset;
        internal static Type UnityEngineComponentType;
        private static readonly Type[] s_SingularInterfaces = new Type[] { typeof(IComponentData), typeof(IBufferElementData), typeof(ISharedComponentData) };

        public static IEnumerable<TypeInfo> AllTypes() => 
            s_Types.Take<TypeInfo>(s_Count);

        public static TypeInfo BuildComponentType(Type type)
        {
            TypeCategory componentData;
            int size = 0;
            FastEquality.TypeInfo @null = FastEquality.TypeInfo.Null;
            EntityOffsetInfo[] entityOffsets = null;
            int bufferCapacity = -1;
            ulong memoryOrdering = CalculateMemoryOrdering(type);
            int num4 = 0;
            if (type.IsInterface)
            {
                throw new ArgumentException($"{type} is an interface. It must be a concrete type.");
            }
            if (typeof(IComponentData).IsAssignableFrom(type))
            {
                if (!type.IsValueType)
                {
                    throw new ArgumentException($"{type} is an IComponentData, and thus must be a struct.");
                }
                if (!UnsafeUtility.IsBlittable(type))
                {
                    throw new ArgumentException($"{type} is an IComponentData, and thus must be blittable (No managed object is allowed on the struct).");
                }
                componentData = TypeCategory.ComponentData;
                if (IsZeroSizeStruct(type))
                {
                    size = 0;
                }
                else
                {
                    size = UnsafeUtility.SizeOf(type);
                }
                @null = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);
            }
            else if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
                if (!type.IsValueType)
                {
                    throw new ArgumentException($"{type} is an IBufferElementData, and thus must be a struct.");
                }
                if (!UnsafeUtility.IsBlittable(type))
                {
                    throw new ArgumentException($"{type} is an IBufferElementData, and thus must be blittable (No managed object is allowed on the struct).");
                }
                componentData = TypeCategory.BufferData;
                num4 = UnsafeUtility.SizeOf(type);
                InternalBufferCapacityAttribute customAttribute = (InternalBufferCapacityAttribute) type.GetCustomAttribute(typeof(InternalBufferCapacityAttribute));
                if (customAttribute != null)
                {
                    bufferCapacity = customAttribute.Capacity;
                }
                else
                {
                    bufferCapacity = 0x80 / num4;
                }
                size = sizeof(BufferHeader) + (bufferCapacity * num4);
                @null = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);
            }
            else if (typeof(ISharedComponentData).IsAssignableFrom(type))
            {
                if (!type.IsValueType)
                {
                    throw new ArgumentException($"{type} is an ISharedComponentData, and thus must be a struct.");
                }
                componentData = TypeCategory.ISharedComponentData;
                @null = FastEquality.CreateTypeInfo(type);
            }
            else
            {
                if (!type.IsClass)
                {
                    throw new ArgumentException($"{type} is not a valid component.");
                }
                componentData = TypeCategory.Class;
                if (type.FullName == "Unity.Entities.GameObjectEntity")
                {
                    throw new ArgumentException("GameObjectEntity cannot be used from EntityManager. The component is ignored when creating entities for a GameObject.");
                }
                if (UnityEngineComponentType == null)
                {
                    throw new ArgumentException($"{type} cannot be used from EntityManager. If it inherits UnityEngine.Component, you must first register {typeof(TypeManager)}.{"UnityEngineComponentType"} or include the Unity.Entities.Hybrid assembly in your build.");
                }
                if (!UnityEngineComponentType.IsAssignableFrom(type))
                {
                    throw new ArgumentException($"{type} must inherit {UnityEngineComponentType}.");
                }
            }
            int num5 = 0;
            foreach (Type type2 in s_SingularInterfaces)
            {
                if (type2.IsAssignableFrom(type))
                {
                    num5++;
                }
            }
            if (num5 > 1)
            {
                throw new ArgumentException($"Component {type} can only implement one of IComponentData, ISharedComponentData and IBufferElementData");
            }
            return new TypeInfo(type, size, componentData, @null, entityOffsets, memoryOrdering, bufferCapacity, (num4 > 0) ? num4 : size);
        }

        private static ulong CalculateMemoryOrdering(Type type)
        {
            ulong num2;
            if (type == typeof(Entity))
            {
                num2 = 0L;
            }
            else
            {
                byte[] destinationArray = new byte[8];
                Array.Copy(new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(type.AssemblyQualifiedName)), 0, destinationArray, 0, 8);
                ulong num = 0L;
                int index = 0;
                while (true)
                {
                    if (index >= 8)
                    {
                        num2 = (num != 0) ? num : ((ulong) 1L);
                        break;
                    }
                    num = (num * ((ulong) 0x100L)) + destinationArray[index];
                    index++;
                }
            }
            return num2;
        }

        private static int CreateTypeIndexThreadSafe(Type type)
        {
            int num2;
            bool lockTaken = false;
            try
            {
                s_CreateTypeLock.Enter(ref lockTaken);
                int index = FindTypeIndex(type, s_Count);
                if (index != -1)
                {
                    num2 = index;
                }
                else
                {
                    s_Count++;
                    index = s_Count;
                    s_Types[index] = BuildComponentType(type);
                    num2 = index;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    s_CreateTypeLock.Exit(true);
                }
            }
            return num2;
        }

        private static int FindTypeIndex(Type type, int count)
        {
            int index = 0;
            while (true)
            {
                int num2;
                if (index == count)
                {
                    num2 = -1;
                }
                else
                {
                    TypeInfo info = s_Types[index];
                    if (!(info.Type == type))
                    {
                        index++;
                        continue;
                    }
                    num2 = index;
                }
                return num2;
            }
        }

        public static Type GetType(int typeIndex) => 
            s_Types[typeIndex].Type;

        public static int GetTypeCount() => 
            s_Count;

        public static int GetTypeIndex<T>()
        {
            int num2;
            int typeIndex = StaticTypeLookup<T>.typeIndex;
            if (typeIndex != 0)
            {
                num2 = typeIndex;
            }
            else
            {
                typeIndex = GetTypeIndex(typeof(T));
                StaticTypeLookup<T>.typeIndex = typeIndex;
                num2 = typeIndex;
            }
            return num2;
        }

        public static int GetTypeIndex(Type type)
        {
            int num = FindTypeIndex(type, s_Count);
            return ((num != -1) ? num : CreateTypeIndexThreadSafe(type));
        }

        public static TypeInfo GetTypeInfo<T>() => 
            s_Types[GetTypeIndex<T>()];

        public static TypeInfo GetTypeInfo(int typeIndex) => 
            s_Types[typeIndex];

        public static void Initialize()
        {
            if (s_Types == null)
            {
                ObjectOffset = UnsafeUtility.SizeOf<ObjectOffsetType>();
                s_CreateTypeLock = new System.Threading.SpinLock();
                s_Types = new TypeInfo[0x2800];
                s_Count = 0;
                s_Count++;
                s_Types[s_Count] = new TypeInfo(null, 0, TypeCategory.ComponentData, FastEquality.TypeInfo.Null, null, 0L, -1, 0);
                s_Count++;
                s_Types[s_Count] = new TypeInfo(typeof(Entity), sizeof(Entity), TypeCategory.EntityData, FastEquality.CreateTypeInfo(typeof(Entity)), EntityRemapUtility.CalculateEntityOffsets(typeof(Entity)), 0L, -1, sizeof(Entity));
            }
        }

        public static bool IsSharedComponent(int typeIndex) => 
            (TypeCategory.ISharedComponentData == GetTypeInfo(typeIndex).Category);

        public static bool IsSystemStateComponent(int typeIndex) => 
            GetTypeInfo(typeIndex).IsSystemStateComponent;

        public static bool IsSystemStateSharedComponent(int typeIndex) => 
            GetTypeInfo(typeIndex).IsSystemStateSharedComponent;

        private static bool IsZeroSizeStruct(Type t)
        {
            int num1;
            if (!t.IsValueType || t.IsPrimitive)
            {
                num1 = 0;
            }
            else
            {
                num1 = (int) t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).All<FieldInfo>(fi => IsZeroSizeStruct(fi.FieldType));
            }
            return (bool) num1;
        }

        public static void RegisterUnityEngineComponentType(Type type)
        {
            int num1;
            if (((type == null) || !type.IsClass) || type.IsInterface)
            {
                num1 = 1;
            }
            else
            {
                num1 = (int) (type.FullName != "UnityEngine.Component");
            }
            if (num1 != 0)
            {
                throw new ArgumentException($"{type} must be typeof(UnityEngine.Component).");
            }
            UnityEngineComponentType = type;
        }

        public static int TypesCount =>
            s_Count;

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly TypeManager.<>c <>9 = new TypeManager.<>c();
            public static Func<FieldInfo, bool> <>9__10_0;

            internal bool <IsZeroSizeStruct>b__10_0(FieldInfo fi) => 
                TypeManager.IsZeroSizeStruct(fi.FieldType);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EntityOffsetInfo
        {
            public int Offset;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ObjectOffsetType
        {
            private unsafe void* v0;
            private unsafe void* v1;
        }

        [StructLayout(LayoutKind.Sequential, Size=1)]
        private struct StaticTypeLookup<T>
        {
            public static int typeIndex;
        }

        public enum TypeCategory
        {
            ComponentData,
            BufferData,
            ISharedComponentData,
            EntityData,
            Class
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TypeInfo
        {
            public readonly System.Type Type;
            public readonly int SizeInChunk;
            public readonly int ElementSize;
            public readonly int BufferCapacity;
            public readonly FastEquality.TypeInfo FastEqualityTypeInfo;
            public readonly TypeManager.TypeCategory Category;
            public readonly TypeManager.EntityOffsetInfo[] EntityOffsets;
            public readonly ulong MemoryOrdering;
            public readonly bool IsSystemStateSharedComponent;
            public readonly bool IsSystemStateComponent;
            public TypeInfo(System.Type type, int size, TypeManager.TypeCategory category, FastEquality.TypeInfo typeInfo, TypeManager.EntityOffsetInfo[] entityOffsets, ulong memoryOrdering, int bufferCapacity, int elementSize)
            {
                this.Type = type;
                this.SizeInChunk = size;
                this.Category = category;
                this.FastEqualityTypeInfo = typeInfo;
                this.EntityOffsets = entityOffsets;
                this.MemoryOrdering = memoryOrdering;
                this.BufferCapacity = bufferCapacity;
                this.ElementSize = elementSize;
                this.IsSystemStateSharedComponent = typeof(ISystemStateSharedComponentData).IsAssignableFrom(type);
                this.IsSystemStateComponent = typeof(ISystemStateComponentData).IsAssignableFrom(type);
            }

            public bool IsZeroSized =>
                (this.SizeInChunk == 0);
        }
    }
}

