namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Type <GroupType>k__BackingField;

        public UpdateInGroupAttribute(Type groupType)
        {
            this.<GroupType>k__BackingField = groupType;
        }

        public Type GroupType =>
            this.<GroupType>k__BackingField;
    }
}

