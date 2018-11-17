namespace Unity.Entities
{
    using System;

    [Flags]
    internal enum FilterType
    {
        None,
        SharedComponent,
        Changed
    }
}

