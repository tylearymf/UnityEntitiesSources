namespace Unity.Entities
{
    using System;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class DisableAutoCreationAttribute : Attribute
    {
    }
}

