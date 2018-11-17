namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct InjectionData
    {
        public Unity.Entities.ComponentType ComponentType;
        public int IndexInComponentGroup;
        public readonly bool IsReadOnly;
        public readonly int FieldOffset;
        public InjectionData(FieldInfo field, Type genericType, bool isReadOnly)
        {
            this.IndexInComponentGroup = -1;
            this.FieldOffset = UnsafeUtility.GetFieldOffset(field);
            Unity.Entities.ComponentType.AccessMode accessModeType = isReadOnly ? Unity.Entities.ComponentType.AccessMode.ReadOnly : Unity.Entities.ComponentType.AccessMode.ReadWrite;
            this.ComponentType = new Unity.Entities.ComponentType(genericType, accessModeType);
            this.IsReadOnly = isReadOnly;
        }
    }
}

