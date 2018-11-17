namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProxySharedComponentData : ISharedComponentData
    {
        private byte m_Internal;
    }
}

