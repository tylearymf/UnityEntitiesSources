namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentType
    {
        public int TypeIndex;
        public AccessMode AccessModeType;
        public int BufferCapacity;
        public bool IsSystemStateComponent =>
            TypeManager.IsSystemStateComponent(this.TypeIndex);
        public bool IsSystemStateSharedComponent =>
            TypeManager.IsSystemStateSharedComponent(this.TypeIndex);
        public bool IsSharedComponent =>
            TypeManager.IsSharedComponent(this.TypeIndex);
        public bool IsZeroSized =>
            TypeManager.GetTypeInfo(this.TypeIndex).IsZeroSized;
        public static ComponentType Create<T>() => 
            FromTypeIndex(TypeManager.GetTypeIndex<T>());

        public static ComponentType FromTypeIndex(int typeIndex)
        {
            ComponentType type;
            type.TypeIndex = typeIndex;
            type.AccessModeType = AccessMode.ReadWrite;
            type.BufferCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            return type;
        }

        public static ComponentType ReadOnly(Type type)
        {
            ComponentType type2 = FromTypeIndex(TypeManager.GetTypeIndex(type));
            type2.AccessModeType = AccessMode.ReadOnly;
            return type2;
        }

        public static ComponentType ReadOnly<T>()
        {
            ComponentType type = Create<T>();
            type.AccessModeType = AccessMode.ReadOnly;
            return type;
        }

        public static ComponentType Subtractive(Type type)
        {
            ComponentType type2 = FromTypeIndex(TypeManager.GetTypeIndex(type));
            type2.AccessModeType = AccessMode.Subtractive;
            return type2;
        }

        public static ComponentType Subtractive<T>()
        {
            ComponentType type = Create<T>();
            type.AccessModeType = AccessMode.Subtractive;
            return type;
        }

        public ComponentType(Type type, AccessMode accessModeType = 0)
        {
            this.TypeIndex = TypeManager.GetTypeIndex(type);
            TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo(this.TypeIndex);
            this.BufferCapacity = typeInfo.BufferCapacity;
            this.AccessModeType = accessModeType;
        }

        internal bool RequiresJobDependency
        {
            get
            {
                bool flag2;
                if (this.AccessModeType == AccessMode.Subtractive)
                {
                    flag2 = false;
                }
                else
                {
                    Type managedType = this.GetManagedType();
                    flag2 = typeof(IComponentData).IsAssignableFrom(managedType) || typeof(IBufferElementData).IsAssignableFrom(managedType);
                }
                return flag2;
            }
        }
        public Type GetManagedType() => 
            TypeManager.GetType(this.TypeIndex);

        public static implicit operator ComponentType(Type type) => 
            new ComponentType(type, AccessMode.ReadWrite);

        public static bool operator <(ComponentType lhs, ComponentType rhs)
        {
            bool flag2;
            if (lhs.TypeIndex == rhs.TypeIndex)
            {
                flag2 = (lhs.BufferCapacity != rhs.BufferCapacity) ? (lhs.BufferCapacity < rhs.BufferCapacity) : (lhs.AccessModeType < rhs.AccessModeType);
            }
            else
            {
                flag2 = lhs.TypeIndex < rhs.TypeIndex;
            }
            return flag2;
        }

        public static bool operator >(ComponentType lhs, ComponentType rhs) => 
            (rhs < lhs);

        public static bool operator ==(ComponentType lhs, ComponentType rhs)
        {
            int num1;
            if ((lhs.TypeIndex != rhs.TypeIndex) || (lhs.BufferCapacity != rhs.BufferCapacity))
            {
                num1 = 0;
            }
            else
            {
                num1 = (int) (lhs.AccessModeType == rhs.AccessModeType);
            }
            return (bool) num1;
        }

        public static bool operator !=(ComponentType lhs, ComponentType rhs)
        {
            int num1;
            if ((lhs.TypeIndex != rhs.TypeIndex) || (lhs.BufferCapacity != rhs.BufferCapacity))
            {
                num1 = 1;
            }
            else
            {
                num1 = (int) (lhs.AccessModeType != rhs.AccessModeType);
            }
            return (bool) num1;
        }

        internal static unsafe bool CompareArray(ComponentType* type1, int typeCount1, ComponentType* type2, int typeCount2)
        {
            bool flag2;
            if (typeCount1 != typeCount2)
            {
                flag2 = false;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index >= typeCount1)
                    {
                        flag2 = true;
                    }
                    else
                    {
                        if (!(type1[index] != type2[index]))
                        {
                            index++;
                            continue;
                        }
                        flag2 = false;
                    }
                    break;
                }
            }
            return flag2;
        }

        public bool IsFixedArray =>
            (this.BufferCapacity != -1);
        public override string ToString()
        {
            string str2;
            string name = this.GetManagedType().Name;
            if (this.IsFixedArray)
            {
                str2 = $"{name}[B {this.BufferCapacity}]";
            }
            else if (this.AccessModeType == AccessMode.Subtractive)
            {
                str2 = $"{name} [S]";
            }
            else if (this.AccessModeType == AccessMode.ReadOnly)
            {
                str2 = $"{name} [RO]";
            }
            else if ((this.TypeIndex == 0) && (this.BufferCapacity == 0))
            {
                str2 = "None";
            }
            else
            {
                str2 = name;
            }
            return str2;
        }

        public override bool Equals(object obj) => 
            ((obj is ComponentType) && (((ComponentType) obj) == this));

        public override int GetHashCode() => 
            ((this.TypeIndex * 0x16b5) ^ this.BufferCapacity);
        public enum AccessMode
        {
            ReadWrite,
            ReadOnly,
            Subtractive
        }
    }
}

