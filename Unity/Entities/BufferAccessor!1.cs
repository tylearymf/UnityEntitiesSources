namespace Unity.Entities
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer]
    public struct BufferAccessor<T> where T: struct, IBufferElementData
    {
        [NativeDisableUnsafePtrRestriction]
        private unsafe byte* m_BasePointer;
        private int m_Length;
        private int m_Stride;
        private bool m_IsReadOnly;
        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;
        public int Length =>
            this.m_Length;
        public unsafe BufferAccessor(byte* basePointer, int length, int stride, bool readOnly, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            this.m_BasePointer = basePointer;
            this.m_Length = length;
            this.m_Stride = stride;
            this.m_Safety0 = safety;
            this.m_ArrayInvalidationSafety = arrayInvalidationSafety;
            this.m_IsReadOnly = readOnly;
            this.m_SafetyReadOnlyCount = readOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = readOnly ? 0 : 2;
        }

        public DynamicBuffer<T> this[int index]
        {
            get
            {
                if (this.m_IsReadOnly)
                {
                    AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
                }
                else
                {
                    AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
                }
                if ((index < 0) || (index >= this.Length))
                {
                    throw new InvalidOperationException($"index {index} out of range in LowLevelBufferAccessor of length {this.Length}");
                }
                return new DynamicBuffer<T>((BufferHeader*) (this.m_BasePointer + (index * this.m_Stride)), this.m_Safety0, this.m_ArrayInvalidationSafety, this.m_IsReadOnly);
            }
        }
    }
}

