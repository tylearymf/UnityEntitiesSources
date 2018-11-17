namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntitySharedComponentCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;
        public int HashCode;
        public GCHandle BoxedObject;
        public unsafe EntitySharedComponentCommand* Prev;
        internal object GetBoxedObject()
        {
            object target;
            if (this.BoxedObject.IsAllocated)
            {
                target = this.BoxedObject.Target;
            }
            else
            {
                target = null;
            }
            return target;
        }
    }
}

