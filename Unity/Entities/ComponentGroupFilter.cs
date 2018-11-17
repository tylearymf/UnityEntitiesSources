namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Assertions;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentGroupFilter
    {
        public FilterType Type;
        public uint RequiredChangeVersion;
        public SharedComponentData Shared;
        public ChangedFilter Changed;
        public bool RequiresMatchesFilter =>
            (this.Type != FilterType.None);
        public void AssertValid()
        {
            if ((this.Type & FilterType.SharedComponent) != FilterType.None)
            {
                Assert.IsTrue((this.Shared.Count <= 2) && (this.Shared.Count > 0));
            }
            else if ((this.Type & FilterType.Changed) != FilterType.None)
            {
                Assert.IsTrue((this.Changed.Count <= 2) && (this.Changed.Count > 0));
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ChangedFilter
        {
            public const int Capacity = 2;
            public int Count;
            [FixedBuffer(typeof(int), 2)]
            public <IndexInComponentGroup>e__FixedBuffer IndexInComponentGroup;
            [StructLayout(LayoutKind.Sequential, Size=8), CompilerGenerated, UnsafeValueType]
            public struct <IndexInComponentGroup>e__FixedBuffer
            {
                public int FixedElementField;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SharedComponentData
        {
            public int Count;
            [FixedBuffer(typeof(int), 2)]
            public <IndexInComponentGroup>e__FixedBuffer IndexInComponentGroup;
            [FixedBuffer(typeof(int), 2)]
            public <SharedComponentIndex>e__FixedBuffer SharedComponentIndex;
            [StructLayout(LayoutKind.Sequential, Size=8), CompilerGenerated, UnsafeValueType]
            public struct <IndexInComponentGroup>e__FixedBuffer
            {
                public int FixedElementField;
            }

            [StructLayout(LayoutKind.Sequential, Size=8), CompilerGenerated, UnsafeValueType]
            public struct <SharedComponentIndex>e__FixedBuffer
            {
                public int FixedElementField;
            }
        }
    }
}

