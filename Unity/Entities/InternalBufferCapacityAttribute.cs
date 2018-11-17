namespace Unity.Entities
{
    using System;

    public class InternalBufferCapacityAttribute : Attribute
    {
        public readonly int Capacity;

        public InternalBufferCapacityAttribute(int capacity)
        {
            this.Capacity = capacity;
        }
    }
}

