namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct Entity : IEquatable<Entity>
    {
        public int Index;
        public int Version;
        public static bool operator ==(Entity lhs, Entity rhs) => 
            ((lhs.Index == rhs.Index) && (lhs.Version == rhs.Version));

        public static bool operator !=(Entity lhs, Entity rhs) => 
            ((lhs.Index != rhs.Index) || (lhs.Version != rhs.Version));

        public override bool Equals(object compare) => 
            (this == ((Entity) compare));

        public override int GetHashCode() => 
            this.Index;

        public static Entity Null =>
            new Entity();
        public bool Equals(Entity entity) => 
            ((entity.Index == this.Index) && (entity.Version == this.Version));

        public override string ToString() => 
            $"Entity Index: {this.Index} Version: {this.Version}";
    }
}

