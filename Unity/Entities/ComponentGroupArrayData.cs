namespace Unity.Entities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsMinMaxWriteRestriction]
    internal struct ComponentGroupArrayData
    {
        public const int kMaxStream = 6;
        [FixedBuffer(typeof(byte), 0x60)]
        private <m_Caches>e__FixedBuffer m_Caches;
        private readonly int m_ComponentDataCount;
        private readonly int m_ComponentCount;
        public readonly int m_Length;
        public readonly int m_MinIndex;
        public readonly int m_MaxIndex;
        public int CacheBeginIndex;
        public int CacheEndIndex;
        private ComponentChunkIterator m_ChunkIterator;
        [FixedBuffer(typeof(int), 6)]
        private <m_IndexInComponentGroup>e__FixedBuffer m_IndexInComponentGroup;
        [FixedBuffer(typeof(bool), 6)]
        private <m_IsWriting>e__FixedBuffer m_IsWriting;
        private readonly int m_SafetyReadOnlyCount;
        private readonly int m_SafetyReadWriteCount;
        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_Safety1;
        private AtomicSafetyHandle m_Safety2;
        private AtomicSafetyHandle m_Safety3;
        private AtomicSafetyHandle m_Safety4;
        private AtomicSafetyHandle m_Safety5;
        [NativeSetClassTypeToNullOnSchedule]
        private readonly ArchetypeManager m_ArchetypeManager;
        public unsafe ComponentGroupArrayData(ComponentGroupArrayStaticCache staticCache)
        {
            int outLength = 0;
            staticCache.ComponentGroup.GetComponentChunkIterator(out outLength, out this.m_ChunkIterator);
            this.m_ChunkIterator.IndexInComponentGroup = 0;
            this.m_Length = outLength;
            this.m_MinIndex = 0;
            this.m_MaxIndex = outLength - 1;
            this.CacheBeginIndex = 0;
            this.CacheEndIndex = 0;
            this.m_ArchetypeManager = staticCache.ComponentGroup.GetArchetypeManager();
            this.m_ComponentDataCount = staticCache.ComponentDataCount;
            this.m_ComponentCount = staticCache.ComponentCount;
            int* numPtr = &this.m_IndexInComponentGroup.FixedElementField;
            bool* flagPtr = &this.m_IsWriting.FixedElementField;
            ComponentGroupStream* streamPtr = (ComponentGroupStream*) &this.m_Caches.FixedElementField;
            int index = 0;
            while (true)
            {
                if (index >= (staticCache.ComponentDataCount + staticCache.ComponentCount))
                {
                    fixed (bool** flagPtrRef = null)
                    {
                        fixed (byte** numPtrRef2 = null)
                        {
                            fixed (int** numPtrRef = null)
                            {
                                this.m_Safety0 = new AtomicSafetyHandle();
                                this.m_Safety1 = new AtomicSafetyHandle();
                                this.m_Safety2 = new AtomicSafetyHandle();
                                this.m_Safety3 = new AtomicSafetyHandle();
                                this.m_Safety4 = new AtomicSafetyHandle();
                                this.m_Safety5 = new AtomicSafetyHandle();
                                Assert.AreEqual(6, 6);
                                this.m_SafetyReadWriteCount = 0;
                                this.m_SafetyReadOnlyCount = 0;
                                ComponentJobSafetyManager safetyManager = staticCache.SafetyManager;
                                AtomicSafetyHandle* handlePtr = &this.m_Safety0;
                                int num3 = 0;
                                while (true)
                                {
                                    if (num3 == staticCache.ComponentTypes.Length)
                                    {
                                        int num4 = 0;
                                        while (true)
                                        {
                                            if (num4 == staticCache.ComponentTypes.Length)
                                            {
                                                fixed (AtomicSafetyHandle* handleRef = null)
                                                {
                                                    return;
                                                }
                                            }
                                            ComponentType type2 = staticCache.ComponentTypes[num4];
                                            if (type2.AccessModeType == ComponentType.AccessMode.ReadWrite)
                                            {
                                                handlePtr[this.m_SafetyReadOnlyCount + this.m_SafetyReadWriteCount] = safetyManager.GetSafetyHandle(type2.TypeIndex, false);
                                                this.m_SafetyReadWriteCount++;
                                            }
                                            num4++;
                                        }
                                    }
                                    ComponentType type = staticCache.ComponentTypes[num3];
                                    if (type.AccessModeType == ComponentType.AccessMode.ReadOnly)
                                    {
                                        handlePtr[this.m_SafetyReadOnlyCount] = safetyManager.GetSafetyHandle(type.TypeIndex, true);
                                        this.m_SafetyReadOnlyCount++;
                                    }
                                    num3++;
                                }
                            }
                        }
                    }
                }
                numPtr[index] = staticCache.ComponentGroup.GetIndexInComponentGroup(staticCache.ComponentTypes[index].TypeIndex);
                streamPtr[index].FieldOffset = (ushort) staticCache.ComponentFieldOffsets[index];
                *((sbyte*) (flagPtr + index)) = staticCache.ComponentTypes[index].AccessModeType == ComponentType.AccessMode.ReadWrite;
                index++;
            }
        }

        public unsafe void UpdateCache(int index)
        {
            this.m_ChunkIterator.MoveToEntityIndex(index);
            int* numPtr = &this.m_IndexInComponentGroup.FixedElementField;
            bool* flagPtr = &this.m_IsWriting.FixedElementField;
            ComponentGroupStream* streamPtr = (ComponentGroupStream*) &this.m_Caches.FixedElementField;
            int num = this.m_ComponentDataCount + this.m_ComponentCount;
            int num2 = 0;
            while (true)
            {
                ComponentChunkCache cache;
                if (num2 >= num)
                {
                    fixed (bool** flagPtrRef = null)
                    {
                        fixed (byte** numPtrRef2 = null)
                        {
                            fixed (int** numPtrRef = null)
                            {
                                return;
                            }
                        }
                    }
                }
                this.m_ChunkIterator.UpdateCacheToCurrentChunk(out cache, flagPtr[num2], numPtr[num2]);
                this.CacheBeginIndex = cache.CachedBeginIndex;
                this.CacheEndIndex = cache.CachedEndIndex;
                int indexInArchetypeFromCurrentChunk = this.m_ChunkIterator.GetIndexInArchetypeFromCurrentChunk(numPtr[num2]);
                streamPtr[num2].SizeOf = cache.CachedSizeOf;
                streamPtr[num2].CachedPtr = (byte*) cache.CachedPtr;
                if (indexInArchetypeFromCurrentChunk > 0xffff)
                {
                    throw new ArgumentException($"There is a maximum of {(ushort) 0xffff} components on one entity.");
                }
                streamPtr[num2].TypeIndexInArchetype = (ushort) indexInArchetypeFromCurrentChunk;
                num2++;
            }
        }

        public unsafe void CheckAccess()
        {
            AtomicSafetyHandle* handlePtr = &this.m_Safety0;
            int index = 0;
            while (true)
            {
                if (index >= this.m_SafetyReadOnlyCount)
                {
                    int safetyReadOnlyCount = this.m_SafetyReadOnlyCount;
                    while (true)
                    {
                        if (safetyReadOnlyCount >= (this.m_SafetyReadOnlyCount + this.m_SafetyReadWriteCount))
                        {
                            fixed (AtomicSafetyHandle* handleRef = null)
                            {
                                return;
                            }
                        }
                        AtomicSafetyHandle.CheckWriteAndThrow(handlePtr[safetyReadOnlyCount]);
                        safetyReadOnlyCount++;
                    }
                }
                AtomicSafetyHandle.CheckReadAndThrow(handlePtr[index]);
                index++;
            }
        }

        public unsafe void PatchPtrs(int index, byte* valuePtr)
        {
            ComponentGroupStream* streamPtr = (ComponentGroupStream*) &this.m_Caches.FixedElementField;
            int num = 0;
            while (true)
            {
                if (num == this.m_ComponentDataCount)
                {
                    fixed (byte** numPtrRef = null)
                    {
                        return;
                    }
                }
                void* voidPtr = (void*) (streamPtr[num].CachedPtr + (streamPtr[num].SizeOf * index));
                void** voidPtr2 = (void**) (valuePtr + streamPtr[num].FieldOffset);
                *((IntPtr*) voidPtr2) = voidPtr;
                num++;
            }
        }

        [BurstDiscard]
        public unsafe void PatchManagedPtrs(int index, byte* valuePtr)
        {
            ComponentGroupStream* streamPtr = (ComponentGroupStream*) &this.m_Caches.FixedElementField;
            int componentDataCount = this.m_ComponentDataCount;
            while (true)
            {
                if (componentDataCount == (this.m_ComponentDataCount + this.m_ComponentCount))
                {
                    fixed (byte** numPtrRef = null)
                    {
                        return;
                    }
                }
                object target = this.m_ChunkIterator.GetManagedObject(this.m_ArchetypeManager, streamPtr[componentDataCount].TypeIndexInArchetype, this.CacheBeginIndex, index);
                byte* numPtr2 = valuePtr + streamPtr[componentDataCount].FieldOffset;
                UnsafeUtility.CopyObjectAddressToPtr(target, (void*) numPtr2);
                componentDataCount++;
            }
        }

        [BurstDiscard]
        public void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{this.m_Length}' Length.");
        }
        [StructLayout(LayoutKind.Sequential, Size=0x60), CompilerGenerated, UnsafeValueType]
        public struct <m_Caches>e__FixedBuffer
        {
            public byte FixedElementField;
        }

        [StructLayout(LayoutKind.Sequential, Size=0x18), CompilerGenerated, UnsafeValueType]
        public struct <m_IndexInComponentGroup>e__FixedBuffer
        {
            public int FixedElementField;
        }

        [StructLayout(LayoutKind.Sequential, Size=6), CompilerGenerated, UnsafeValueType]
        public struct <m_IsWriting>e__FixedBuffer
        {
            public bool FixedElementField;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ComponentGroupStream
        {
            public unsafe byte* CachedPtr;
            public int SizeOf;
            public ushort FieldOffset;
            public ushort TypeIndexInArchetype;
        }
    }
}

