namespace Unity.Entities
{
    using System;

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Struct)]
    public class DisallowRefReturnCrossingThisAttribute : Attribute
    {
    }
}

