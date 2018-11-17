namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public struct EntityArchetype : IEquatable<EntityArchetype>
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe Unity.Entities.Archetype* Archetype;
        public bool Valid =>
            (this.Archetype != null);
        public static unsafe bool operator ==(EntityArchetype lhs, EntityArchetype rhs) => 
            (lhs.Archetype == rhs.Archetype);

        public static unsafe bool operator !=(EntityArchetype lhs, EntityArchetype rhs) => 
            (lhs.Archetype != rhs.Archetype);

        public override bool Equals(object compare) => 
            (this == ((EntityArchetype) compare));

        public unsafe bool Equals(EntityArchetype entityArchetype) => 
            (this.Archetype == entityArchetype.Archetype);

        public override unsafe int GetHashCode() => 
            ((int) this.Archetype);

        public ComponentType[] ComponentTypes
        {
            get
            {
                ComponentType[] typeArray = new ComponentType[this.Archetype.TypesCount];
                for (int i = 0; i < typeArray.Length; i++)
                {
                    typeArray[i] = (this.Archetype.Types + i).ToComponentType();
                }
                return typeArray;
            }
        }
        public int ChunkCount =>
            this.Archetype.ChunkCount;
        public int ChunkCapacity =>
            this.Archetype.ChunkCapacity;
    }
}

