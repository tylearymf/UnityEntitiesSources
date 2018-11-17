namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Unity;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Assertions;

    internal class SharedComponentDataManager
    {
        private NativeMultiHashMap<int, int> m_HashLookup = new NativeMultiHashMap<int, int>(0x80, Allocator.Persistent);
        private List<object> m_SharedComponentData = new List<object>();
        private NativeList<int> m_SharedComponentRefCount = new NativeList<int>(0, Allocator.Persistent);
        private NativeList<int> m_SharedComponentType = new NativeList<int>(0, Allocator.Persistent);
        private NativeList<int> m_SharedComponentVersion = new NativeList<int>(0, Allocator.Persistent);
        private int m_FreeListIndex;

        public SharedComponentDataManager()
        {
            this.m_SharedComponentData.Add(null);
            this.m_SharedComponentRefCount.Add(1);
            this.m_SharedComponentVersion.Add(1);
            this.m_SharedComponentType.Add(-1);
            this.m_FreeListIndex = -1;
        }

        private int Add(int typeIndex, int hashCode, object newData)
        {
            int count;
            if (this.m_FreeListIndex == -1)
            {
                count = this.m_SharedComponentData.Count;
                this.m_HashLookup.Add(hashCode, count);
                this.m_SharedComponentData.Add(newData);
                this.m_SharedComponentRefCount.Add(1);
                this.m_SharedComponentVersion.Add(1);
                this.m_SharedComponentType.Add(typeIndex);
            }
            else
            {
                count = this.m_FreeListIndex;
                this.m_FreeListIndex = this.m_SharedComponentVersion[count];
                Assert.IsTrue(this.m_SharedComponentData[count] == null);
                this.m_HashLookup.Add(hashCode, count);
                this.m_SharedComponentData.set_Item(count, newData);
                this.m_SharedComponentRefCount.set_Item(count, 1);
                this.m_SharedComponentVersion.set_Item(count, 1);
                this.m_SharedComponentType.set_Item(count, typeIndex);
            }
            return count;
        }

        public unsafe void AddReference(int index)
        {
            if (index != 0)
            {
                int num = index;
                NativeList<int>* listPtr1 = (NativeList<int>*) ref this.m_SharedComponentRefCount;
                listPtr1.set_Item(num, listPtr1[num] + 1);
            }
        }

        public unsafe bool AllSharedComponentReferencesAreFromChunks(ArchetypeManager archetypeManager)
        {
            NativeArray<int> nativeArray = new NativeArray<int>(this.m_SharedComponentRefCount.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            Archetype* lastArchetype = archetypeManager.m_LastArchetype;
            while (true)
            {
                if (lastArchetype == null)
                {
                    nativeArray.set_Item(0, 1);
                    int num = UnsafeUtility.MemCmp(this.m_SharedComponentRefCount.GetUnsafePtr<int>(), nativeArray.GetUnsafeReadOnlyPtr<int>(), (long) (4 * nativeArray.Length));
                    nativeArray.Dispose();
                    return (num == 0);
                }
                UnsafeLinkedListNode* begin = lastArchetype->ChunkList.Begin;
                while (true)
                {
                    if (begin == lastArchetype->ChunkList.End)
                    {
                        lastArchetype = lastArchetype->PrevArchetype;
                        break;
                    }
                    Chunk* chunkPtr = (Chunk*) begin;
                    int index = 0;
                    while (true)
                    {
                        if (index >= lastArchetype->NumSharedComponents)
                        {
                            begin = begin->Next;
                            break;
                        }
                        ref NativeArray<int> arrayRef = ref nativeArray;
                        int num3 = chunkPtr->SharedComponentValueArray[index];
                        arrayRef.set_Item(num3, arrayRef[num3] + 1);
                        index++;
                    }
                }
            }
        }

        public void CheckRefcounts()
        {
            int expected = 0;
            int num2 = 0;
            while (true)
            {
                if (num2 >= this.m_SharedComponentData.Count)
                {
                    Assert.AreEqual(expected, this.m_HashLookup.Length);
                    return;
                }
                if (this.m_SharedComponentData[num2] != null)
                {
                    expected++;
                }
                num2++;
            }
        }

        public void Dispose()
        {
            if (!this.IsEmpty())
            {
                Debug.LogWarning("SharedComponentData manager should be empty on shutdown");
            }
            this.m_SharedComponentType.Dispose();
            this.m_SharedComponentRefCount.Dispose();
            this.m_SharedComponentVersion.Dispose();
            this.m_SharedComponentData.Clear();
            this.m_SharedComponentData = null;
            this.m_HashLookup.Dispose();
        }

        private unsafe int FindNonDefaultSharedComponentIndex(int typeIndex, int hashCode, void* newData, FastEquality.TypeInfo typeInfo)
        {
            int num;
            NativeMultiHashMapIterator<int> iterator;
            int num2;
            if (!this.m_HashLookup.TryGetFirstValue(hashCode, out num, out iterator))
            {
                num2 = -1;
            }
            else
            {
                while (true)
                {
                    object target = this.m_SharedComponentData[num];
                    if ((target != null) && (this.m_SharedComponentType[num] == typeIndex))
                    {
                        ulong num3;
                        void* rhsPtr = ref PinGCObjectAndGetAddress(target, out num3);
                        bool flag3 = FastEquality.Equals(newData, rhsPtr, typeInfo);
                        UnsafeUtility.ReleaseGCObject(num3);
                        if (flag3)
                        {
                            num2 = num;
                            break;
                        }
                    }
                    if (!this.m_HashLookup.TryGetNextValue(out num, ref iterator))
                    {
                        num2 = -1;
                        break;
                    }
                }
            }
            return num2;
        }

        private unsafe int FindSharedComponentIndex<T>(int typeIndex, T newData) where T: struct
        {
            int num;
            T lhs = default(T);
            FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
            if (FastEquality.Equals<T>(ref lhs, ref newData, fastEqualityTypeInfo))
            {
                num = 0;
            }
            else
            {
                num = this.FindNonDefaultSharedComponentIndex(typeIndex, FastEquality.GetHashCode<T>(ref newData, fastEqualityTypeInfo), UnsafeUtility.AddressOf<T>(ref newData), fastEqualityTypeInfo);
            }
            return num;
        }

        public void GetAllUniqueSharedComponents<T>(List<T> sharedComponentValues) where T: struct, ISharedComponentData
        {
            T item = default(T);
            sharedComponentValues.Add(item);
            for (int i = 1; i != this.m_SharedComponentData.Count; i++)
            {
                object obj2 = this.m_SharedComponentData[i];
                if ((obj2 != null) && (obj2.GetType() == typeof(T)))
                {
                    sharedComponentValues.Add(this.m_SharedComponentData[i]);
                }
            }
        }

        public void GetAllUniqueSharedComponents<T>(List<T> sharedComponentValues, List<int> sharedComponentIndices) where T: struct, ISharedComponentData
        {
            T item = default(T);
            sharedComponentValues.Add(item);
            sharedComponentIndices.Add(0);
            for (int i = 1; i != this.m_SharedComponentData.Count; i++)
            {
                object obj2 = this.m_SharedComponentData[i];
                if ((obj2 != null) && (obj2.GetType() == typeof(T)))
                {
                    sharedComponentValues.Add(this.m_SharedComponentData[i]);
                    sharedComponentIndices.Add(i);
                }
            }
        }

        private static unsafe int GetHashCodeFast(object target, FastEquality.TypeInfo typeInfo)
        {
            ulong num;
            int hashCode = FastEquality.GetHashCode(PinGCObjectAndGetAddress(target, out num), typeInfo);
            UnsafeUtility.ReleaseGCObject(num);
            return hashCode;
        }

        public int GetSharedComponentCount() => 
            this.m_SharedComponentData.Count;

        public T GetSharedComponentData<T>(int index) where T: struct
        {
            T local2;
            if (index != 0)
            {
                local2 = this.m_SharedComponentData[index];
            }
            else
            {
                local2 = default(T);
            }
            return local2;
        }

        public object GetSharedComponentDataBoxed(int index, int typeIndex)
        {
            object obj2;
            if (index == 0)
            {
                obj2 = Activator.CreateInstance(TypeManager.GetType(typeIndex));
            }
            else
            {
                obj2 = this.m_SharedComponentData[index];
            }
            return obj2;
        }

        public object GetSharedComponentDataNonDefaultBoxed(int index)
        {
            Assert.AreNotEqual(0, index);
            return this.m_SharedComponentData[index];
        }

        public int GetSharedComponentVersion<T>(T sharedData) where T: struct
        {
            int num = this.FindSharedComponentIndex<T>(TypeManager.GetTypeIndex<T>(), sharedData);
            return ((num == -1) ? 0 : this.m_SharedComponentVersion[num]);
        }

        public unsafe void IncrementSharedComponentVersion(int index)
        {
            int num = index;
            NativeList<int>* listPtr1 = (NativeList<int>*) ref this.m_SharedComponentVersion;
            listPtr1.set_Item(num, listPtr1[num] + 1);
        }

        public unsafe int InsertSharedComponent<T>(T newData) where T: struct
        {
            int num4;
            int typeIndex = TypeManager.GetTypeIndex<T>();
            int num2 = this.FindSharedComponentIndex<T>(TypeManager.GetTypeIndex<T>(), newData);
            if (num2 == 0)
            {
                num4 = 0;
            }
            else if (num2 == -1)
            {
                FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                num4 = this.Add(typeIndex, FastEquality.GetHashCode<T>(ref newData, fastEqualityTypeInfo), newData);
            }
            else
            {
                int index = num2;
                NativeList<int>* listPtr1 = (NativeList<int>*) ref this.m_SharedComponentRefCount;
                listPtr1.set_Item(index, listPtr1[index] + 1);
                num4 = num2;
            }
            return num4;
        }

        internal unsafe int InsertSharedComponentAssumeNonDefault(int typeIndex, int hashCode, object newData, FastEquality.TypeInfo typeInfo)
        {
            ulong num;
            void* voidPtr = ref PinGCObjectAndGetAddress(newData, out num);
            int num2 = this.FindNonDefaultSharedComponentIndex(typeIndex, hashCode, voidPtr, typeInfo);
            UnsafeUtility.ReleaseGCObject(num);
            if (-1 == num2)
            {
                num2 = this.Add(typeIndex, hashCode, newData);
            }
            else
            {
                ref NativeList<int> listRef = ref this.m_SharedComponentRefCount;
                int index = num2;
                listRef.set_Item(index, listRef[index] + 1);
            }
            return num2;
        }

        public bool IsEmpty()
        {
            int num = 1;
            while (true)
            {
                bool flag2;
                if (num >= this.m_SharedComponentData.Count)
                {
                    if (this.m_SharedComponentData[0] != null)
                    {
                        flag2 = false;
                    }
                    else if (this.m_HashLookup.Length != 0)
                    {
                        flag2 = false;
                    }
                    else
                    {
                        flag2 = true;
                    }
                }
                else if (this.m_SharedComponentData[num] != null)
                {
                    flag2 = false;
                }
                else if (this.m_SharedComponentType[num] != -1)
                {
                    flag2 = false;
                }
                else
                {
                    if (this.m_SharedComponentRefCount[num] == 0)
                    {
                        num++;
                        continue;
                    }
                    flag2 = false;
                }
                return flag2;
            }
        }

        public NativeArray<int> MoveAllSharedComponents(SharedComponentDataManager srcSharedComponents, Allocator allocator)
        {
            NativeArray<int> array = new NativeArray<int>(srcSharedComponents.GetSharedComponentCount(), allocator, NativeArrayOptions.ClearMemory);
            array.set_Item(0, 0);
            int index = 1;
            while (true)
            {
                if (index >= array.Length)
                {
                    srcSharedComponents.m_HashLookup.Clear();
                    srcSharedComponents.m_SharedComponentVersion.ResizeUninitialized(1);
                    srcSharedComponents.m_SharedComponentRefCount.ResizeUninitialized(1);
                    srcSharedComponents.m_SharedComponentType.ResizeUninitialized(1);
                    srcSharedComponents.m_SharedComponentData.Clear();
                    srcSharedComponents.m_SharedComponentData.Add(null);
                    srcSharedComponents.m_FreeListIndex = -1;
                    return array;
                }
                object newData = srcSharedComponents.m_SharedComponentData[index];
                if (newData != null)
                {
                    int typeIndex = srcSharedComponents.m_SharedComponentType[index];
                    FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                    int num4 = this.InsertSharedComponentAssumeNonDefault(typeIndex, GetHashCodeFast(newData, fastEqualityTypeInfo), newData, fastEqualityTypeInfo);
                    ref NativeList<int> listRef = ref this.m_SharedComponentRefCount;
                    int num5 = num4;
                    listRef.set_Item(num5, listRef[num5] + (srcSharedComponents.m_SharedComponentRefCount[index] - 1));
                    array.set_Item(index, num4);
                }
                index++;
            }
        }

        public unsafe void MoveSharedComponents(SharedComponentDataManager srcSharedComponents, int* sharedComponentIndices, int sharedComponentIndicesCount)
        {
            for (int i = 0; i != sharedComponentIndicesCount; i++)
            {
                int index = sharedComponentIndices[i];
                if (index != 0)
                {
                    object newData = srcSharedComponents.m_SharedComponentData[index];
                    int typeIndex = srcSharedComponents.m_SharedComponentType[index];
                    FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                    int num5 = this.InsertSharedComponentAssumeNonDefault(typeIndex, GetHashCodeFast(newData, fastEqualityTypeInfo), newData, fastEqualityTypeInfo);
                    srcSharedComponents.RemoveReference(index, 1);
                    sharedComponentIndices[i] = num5;
                }
            }
        }

        public unsafe NativeArray<int> MoveSharedComponents(SharedComponentDataManager srcSharedComponents, NativeArray<ArchetypeChunk> chunks, Allocator allocator)
        {
            NativeArray<int> array = new NativeArray<int>(srcSharedComponents.GetSharedComponentCount(), allocator, NativeArrayOptions.ClearMemory);
            int num = 0;
            while (true)
            {
                int num4;
                if (num >= chunks.Length)
                {
                    array.set_Item(0, 0);
                    for (int i = 1; i < array.Length; i++)
                    {
                        if (array[i] != 0)
                        {
                            object newData = srcSharedComponents.m_SharedComponentData[i];
                            int typeIndex = srcSharedComponents.m_SharedComponentType[i];
                            FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                            int num8 = this.InsertSharedComponentAssumeNonDefault(typeIndex, GetHashCodeFast(newData, fastEqualityTypeInfo), newData, fastEqualityTypeInfo);
                            ref NativeList<int> listRef = ref this.m_SharedComponentRefCount;
                            num4 = num8;
                            listRef.set_Item(num4, listRef[num4] + (array[i] - 1));
                            srcSharedComponents.RemoveReference(i, array[i]);
                            array.set_Item(i, num8);
                        }
                    }
                    return array;
                }
                Chunk* chunk = chunks[num].m_Chunk;
                Archetype* archetype = chunk->Archetype;
                int index = 0;
                while (true)
                {
                    if (index >= archetype->NumSharedComponents)
                    {
                        num++;
                        break;
                    }
                    int num3 = chunk->SharedComponentValueArray[index];
                    NativeArray<int>* arrayPtr1 = (NativeArray<int>*) ref array;
                    num4 = arrayPtr1[num3];
                    arrayPtr1.set_Item(num3, num4 + 1);
                    index++;
                }
            }
        }

        private static unsafe void* PinGCObjectAndGetAddress(object target, out ulong handle) => 
            (UnsafeUtility.PinGCObjectAndGetAddress(target, out handle) + TypeManager.ObjectOffset);

        public void PrepareForDeserialize()
        {
            if (!this.IsEmpty())
            {
                throw new ArgumentException("SharedComponentManager must be empty when deserializing a scene");
            }
            this.m_HashLookup.Clear();
            this.m_SharedComponentVersion.ResizeUninitialized(1);
            this.m_SharedComponentRefCount.ResizeUninitialized(1);
            this.m_SharedComponentType.ResizeUninitialized(1);
            this.m_SharedComponentData.Clear();
            this.m_SharedComponentData.Add(null);
            this.m_FreeListIndex = -1;
        }

        public void RemoveReference(int index, int numRefs = 1)
        {
            if (index != 0)
            {
                int num6;
                ref NativeList<int> listRef = ref this.m_SharedComponentRefCount;
                int num5 = index;
                listRef.set_Item(num5, num6 = listRef[num5] - numRefs);
                int num = num6;
                Assert.IsTrue(num >= 0);
                if (num == 0)
                {
                    int num4;
                    NativeMultiHashMapIterator<int> iterator;
                    FastEquality.TypeInfo fastEqualityTypeInfo = TypeManager.GetTypeInfo(this.m_SharedComponentType[index]).FastEqualityTypeInfo;
                    int hashCodeFast = GetHashCodeFast(this.m_SharedComponentData[index], fastEqualityTypeInfo);
                    this.m_SharedComponentData.set_Item(index, null);
                    this.m_SharedComponentType.set_Item(index, -1);
                    this.m_SharedComponentVersion.set_Item(index, this.m_FreeListIndex);
                    this.m_FreeListIndex = index;
                    if (!this.m_HashLookup.TryGetFirstValue(hashCodeFast, out num4, out iterator))
                    {
                        throw new ArgumentException("RemoveReference didn't find element in in hashtable");
                    }
                    while (true)
                    {
                        if (num4 == index)
                        {
                            this.m_HashLookup.Remove(iterator);
                        }
                        else if (this.m_HashLookup.TryGetNextValue(out num4, ref iterator))
                        {
                            continue;
                        }
                        break;
                    }
                }
            }
        }
    }
}

