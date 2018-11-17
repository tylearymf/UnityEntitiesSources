namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct JobProcessComponentDataCache
    {
        public IntPtr JobReflectionData;
        public IntPtr JobReflectionDataParallelFor;
        public ComponentType[] Types;
        public ComponentType[] FilterChanged;
        public int ProcessTypesCount;
        public Unity.Entities.ComponentGroup ComponentGroup;
        public ComponentSystemBase ComponentSystem;
    }
}

