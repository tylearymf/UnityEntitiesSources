namespace Unity.Entities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class UpdateAfterAttribute : Attribute
    {
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Type <SystemType>k__BackingField;

        public UpdateAfterAttribute(Type systemType)
        {
            this.<SystemType>k__BackingField = systemType;
        }

        public Type SystemType =>
            this.<SystemType>k__BackingField;
    }
}

