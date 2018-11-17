namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentTypeInArchetype
    {
        public readonly int TypeIndex;
        public readonly int BufferCapacity;
        public bool IsBuffer =>
            (this.BufferCapacity >= 0);
        public bool IsSystemStateComponent =>
            TypeManager.IsSystemStateComponent(this.TypeIndex);
        public bool IsSystemStateSharedComponent =>
            TypeManager.IsSystemStateSharedComponent(this.TypeIndex);
        public bool IsSharedComponent =>
            TypeManager.IsSharedComponent(this.TypeIndex);
        public bool IsZeroSized =>
            TypeManager.GetTypeInfo(this.TypeIndex).IsZeroSized;
        public ComponentTypeInArchetype(ComponentType type)
        {
            this.TypeIndex = type.TypeIndex;
            this.BufferCapacity = type.BufferCapacity;
        }

        public static bool operator ==(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            ((lhs.TypeIndex == rhs.TypeIndex) && (lhs.BufferCapacity == rhs.BufferCapacity));

        public static bool operator !=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            ((lhs.TypeIndex != rhs.TypeIndex) || (lhs.BufferCapacity != rhs.BufferCapacity));

        public static bool operator <(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            ((lhs.TypeIndex != rhs.TypeIndex) ? (lhs.TypeIndex < rhs.TypeIndex) : (lhs.BufferCapacity < rhs.BufferCapacity));

        public static bool operator >(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            ((lhs.TypeIndex != rhs.TypeIndex) ? (lhs.TypeIndex > rhs.TypeIndex) : (lhs.BufferCapacity > rhs.BufferCapacity));

        public static bool operator <=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            (lhs <= rhs);

        public static bool operator >=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs) => 
            (lhs >= rhs);

        public static unsafe bool CompareArray(ComponentTypeInArchetype* type1, int typeCount1, ComponentTypeInArchetype* type2, int typeCount2)
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

        public ComponentType ToComponentType()
        {
            ComponentType type;
            type.BufferCapacity = this.BufferCapacity;
            type.TypeIndex = this.TypeIndex;
            type.AccessModeType = ComponentType.AccessMode.ReadWrite;
            return type;
        }

        public override string ToString() => 
            this.ToComponentType().ToString();

        public override bool Equals(object obj)
        {
            bool flag2;
            if (obj is ComponentTypeInArchetype)
            {
                flag2 = ((ComponentTypeInArchetype) obj) == this;
            }
            else
            {
                flag2 = false;
            }
            return flag2;
        }

        public override int GetHashCode() => 
            ((this.TypeIndex * 0x16bb) ^ this.BufferCapacity);
    }
}

