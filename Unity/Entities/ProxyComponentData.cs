namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProxyComponentData : IComponentData
    {
        private byte m_Internal;
    }
}

