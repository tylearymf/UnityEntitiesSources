namespace Unity.Entities.Serialization
{
    using System;

    public interface BinaryReader : IDisposable
    {
        unsafe void ReadBytes(void* data, int bytes);
    }
}

