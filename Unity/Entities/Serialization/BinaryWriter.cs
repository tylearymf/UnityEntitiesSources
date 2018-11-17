namespace Unity.Entities.Serialization
{
    using System;

    public interface BinaryWriter : IDisposable
    {
        unsafe void WriteBytes(void* data, int bytes);
    }
}

