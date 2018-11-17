namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProxyBufferElementData : IBufferElementData
    {
        private byte m_Internal;
    }
}

