namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public struct ArchetypeChunk : IEquatable<ArchetypeChunk>
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe Chunk* m_Chunk;
        public int Count =>
            this.m_Chunk.Count;
        public static unsafe bool operator ==(ArchetypeChunk lhs, ArchetypeChunk rhs) => 
            (lhs.m_Chunk == rhs.m_Chunk);

        public static unsafe bool operator !=(ArchetypeChunk lhs, ArchetypeChunk rhs) => 
            (lhs.m_Chunk != rhs.m_Chunk);

        public override bool Equals(object compare) => 
            (this == ((ArchetypeChunk) compare));

        public override unsafe int GetHashCode() => 
            ((int) (((ulong) ((UIntPtr) this.m_Chunk)) >> 15));

        public EntityArchetype Archetype =>
            new EntityArchetype { Archetype=this.m_Chunk.Archetype };
        public static ArchetypeChunk Null =>
            new ArchetypeChunk();
        public unsafe bool Equals(ArchetypeChunk archetypeChunk) => 
            (this.m_Chunk == archetypeChunk.m_Chunk);

        public unsafe int NumSharedComponents() => 
            this.m_Chunk.Archetype.NumSharedComponents;

        public unsafe NativeArray<Entity> GetNativeArray(ArchetypeChunkEntityType archetypeChunkEntityType)
        {
            AtomicSafetyHandle.CheckReadAndThrow(archetypeChunkEntityType.m_Safety);
            int count = this.m_Chunk.Count;
            NativeArray<Entity> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>((void*) (&this.m_Chunk.Buffer.FixedElementField + this.m_Chunk.Archetype.Offsets[0]), count, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<Entity>(ref array, archetypeChunkEntityType.m_Safety);
            return array;
        }

        public bool DidAddOrChange<T>(ArchetypeChunkComponentType<T> chunkComponentType, uint version) where T: struct, IComponentData => 
            ChangeVersionUtility.DidAddOrChange(this.GetComponentVersion<T>(chunkComponentType), version);

        public bool DidChange<T>(ArchetypeChunkComponentType<T> chunkComponentType) where T: struct, IComponentData => 
            ChangeVersionUtility.DidChange(this.GetComponentVersion<T>(chunkComponentType), chunkComponentType.GlobalSystemVersion);

        public unsafe uint GetComponentVersion<T>(ArchetypeChunkComponentType<T> chunkComponentType) where T: struct, IComponentData
        {
            uint num2;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(this.m_Chunk.Archetype, chunkComponentType.m_TypeIndex);
            if (indexInTypeArray == -1)
            {
                num2 = 0;
            }
            else
            {
                num2 = this.m_Chunk.ChangeVersion[indexInTypeArray];
            }
            return num2;
        }

        public unsafe int GetSharedComponentIndex<T>(ArchetypeChunkSharedComponentType<T> chunkSharedComponentData) where T: struct, ISharedComponentData
        {
            int num4;
            Unity.Entities.Archetype* archetype = this.m_Chunk.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, chunkSharedComponentData.m_TypeIndex);
            if (indexInTypeArray == -1)
            {
                num4 = -1;
            }
            else
            {
                int index = archetype->SharedComponentOffset[indexInTypeArray];
                num4 = this.m_Chunk.SharedComponentValueArray[index];
            }
            return num4;
        }

        public unsafe bool Has<T>(ArchetypeChunkComponentType<T> chunkComponentType) where T: struct, IComponentData => 
            (ChunkDataUtility.GetIndexInTypeArray(this.m_Chunk.Archetype, chunkComponentType.m_TypeIndex) != -1);

        public unsafe NativeArray<T> GetNativeArray<T>(ArchetypeChunkComponentType<T> chunkComponentType) where T: struct, IComponentData
        {
            NativeArray<T> array3;
            if (chunkComponentType.m_IsZeroSized)
            {
                throw new ArgumentException($"ArchetypeChunk.GetNativeArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
            AtomicSafetyHandle.CheckReadAndThrow(chunkComponentType.m_Safety);
            Unity.Entities.Archetype* archetype = this.m_Chunk.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(this.m_Chunk.Archetype, chunkComponentType.m_TypeIndex);
            if (indexInTypeArray == -1)
            {
                NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(null, 0, Allocator.Invalid);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref array, chunkComponentType.m_Safety);
                array3 = array;
            }
            else
            {
                int count = this.m_Chunk.Count;
                NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*) (&this.m_Chunk.Buffer.FixedElementField + archetype->Offsets[indexInTypeArray]), count, Allocator.None);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref array, chunkComponentType.m_Safety);
                if (!chunkComponentType.IsReadOnly)
                {
                    this.m_Chunk.ChangeVersion[indexInTypeArray] = chunkComponentType.GlobalSystemVersion;
                }
                array3 = array;
            }
            return array3;
        }

        public unsafe BufferAccessor<T> GetBufferAccessor<T>(ArchetypeChunkBufferType<T> bufferComponentType) where T: struct, IBufferElementData
        {
            BufferAccessor<T> accessor;
            AtomicSafetyHandle.CheckReadAndThrow(bufferComponentType.m_Safety);
            Unity.Entities.Archetype* archetype = this.m_Chunk.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, bufferComponentType.m_TypeIndex);
            if (indexInTypeArray == -1)
            {
                accessor = new BufferAccessor<T>(null, 0, 0, true, bufferComponentType.m_Safety, bufferComponentType.m_ArrayInvalidationSafety);
            }
            else
            {
                if (!bufferComponentType.IsReadOnly)
                {
                    this.m_Chunk.ChangeVersion[indexInTypeArray] = bufferComponentType.GlobalSystemVersion;
                }
                int count = this.m_Chunk.Count;
                accessor = new BufferAccessor<T>(&this.m_Chunk.Buffer.FixedElementField + archetype->Offsets[indexInTypeArray], count, archetype->SizeOfs[indexInTypeArray], bufferComponentType.IsReadOnly, bufferComponentType.m_Safety, bufferComponentType.m_ArrayInvalidationSafety);
            }
            return accessor;
        }
    }
}

