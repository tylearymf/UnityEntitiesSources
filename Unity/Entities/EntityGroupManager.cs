namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Assertions;

    internal class EntityGroupManager : IDisposable
    {
        private readonly ComponentJobSafetyManager m_JobSafetyManager;
        private ChunkAllocator m_GroupDataChunkAllocator;
        private unsafe EntityGroupData* m_LastGroupData;

        public EntityGroupManager(ComponentJobSafetyManager safetyManager)
        {
            this.m_JobSafetyManager = safetyManager;
        }

        public unsafe void AddArchetypeIfMatching(Archetype* type)
        {
            for (EntityGroupData* dataPtr = this.m_LastGroupData; dataPtr != null; dataPtr = dataPtr->PrevGroup)
            {
                this.AddArchetypeIfMatching(type, dataPtr);
            }
        }

        private unsafe void AddArchetypeIfMatching(Archetype* archetype, EntityGroupData* group)
        {
            if (IsMatchingArchetype(archetype, group))
            {
                MatchingArchetypes* archetypesPtr = (MatchingArchetypes*) ref this.m_GroupDataChunkAllocator.Allocate(MatchingArchetypes.GetAllocationSize(group.RequiredComponentsCount), 8);
                archetypesPtr->Archetype = archetype;
                int* numPtr = &archetypesPtr->IndexInArchetype.FixedElementField;
                if (group.LastMatchingArchetype == null)
                {
                    group.LastMatchingArchetype = archetypesPtr;
                }
                archetypesPtr->Next = group.FirstMatchingArchetype;
                group.FirstMatchingArchetype = archetypesPtr;
                int index = 0;
                while (true)
                {
                    if (index >= group.RequiredComponentsCount)
                    {
                        break;
                    }
                    int actual = -1;
                    if (group.RequiredComponents[index].AccessModeType != ComponentType.AccessMode.Subtractive)
                    {
                        actual = ChunkDataUtility.GetIndexInTypeArray(archetype, group.RequiredComponents[index].TypeIndex);
                        Assert.AreNotEqual(-1, actual);
                    }
                    numPtr[index] = actual;
                    index++;
                }
            }
        }

        public static unsafe bool CompareComponents(ComponentType[] componentTypes, EntityGroupData* groupData)
        {
            bool flag2;
            if (groupData.RequiredComponents == null)
            {
                flag2 = false;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index >= componentTypes.Length)
                    {
                        if ((componentTypes.Length + 1) != groupData.RequiredComponentsCount)
                        {
                            flag2 = false;
                        }
                        else
                        {
                            int num2 = 0;
                            while (true)
                            {
                                if (num2 >= componentTypes.Length)
                                {
                                    flag2 = true;
                                }
                                else
                                {
                                    if (!(groupData.RequiredComponents[num2 + 1] != componentTypes[num2]))
                                    {
                                        num2++;
                                        continue;
                                    }
                                    flag2 = false;
                                }
                                break;
                            }
                        }
                        break;
                    }
                    if (componentTypes[index].TypeIndex == TypeManager.GetTypeIndex<Entity>())
                    {
                        throw new ArgumentException("ComponentGroup.CompareComponents may not include typeof(Entity), it is implicit");
                    }
                    index++;
                }
            }
            return flag2;
        }

        public static unsafe bool CompareQuery(EntityArchetypeQuery[] query, EntityGroupData* groupData)
        {
            bool flag2;
            if (groupData.RequiredComponents != null)
            {
                flag2 = false;
            }
            else if (groupData.ArchetypeQueryCount != query.Length)
            {
                flag2 = false;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index == query.Length)
                    {
                        flag2 = true;
                    }
                    else if (!CompareQueryArray(query[index].All, groupData.ArchetypeQuery[index].All, groupData.ArchetypeQuery[index].AllCount))
                    {
                        flag2 = false;
                    }
                    else if (!CompareQueryArray(query[index].None, groupData.ArchetypeQuery[index].None, groupData.ArchetypeQuery[index].NoneCount))
                    {
                        flag2 = false;
                    }
                    else
                    {
                        if (CompareQueryArray(query[index].Any, groupData.ArchetypeQuery[index].Any, groupData.ArchetypeQuery[index].AnyCount))
                        {
                            index++;
                            continue;
                        }
                        flag2 = false;
                    }
                    break;
                }
            }
            return flag2;
        }

        public static unsafe bool CompareQueryArray(ComponentType[] filter, int* typeArray, int typeArrayCount)
        {
            bool flag2;
            int num = (filter != null) ? filter.Length : 0;
            if (typeArrayCount != num)
            {
                flag2 = false;
            }
            else
            {
                int index = 0;
                while (true)
                {
                    if (index >= num)
                    {
                        flag2 = true;
                    }
                    else
                    {
                        if (typeArray[index] == filter[index].TypeIndex)
                        {
                            index++;
                            continue;
                        }
                        flag2 = false;
                    }
                    break;
                }
            }
            return flag2;
        }

        private unsafe void ConstructTypeArray(ComponentType[] types, out int* outTypes, out int outLength)
        {
            if ((types == null) || (types.Length == 0))
            {
                outTypes = (int*) IntPtr.Zero;
                outLength = 0;
            }
            else
            {
                outLength = types.Length;
                outTypes = (int*) this.m_GroupDataChunkAllocator.Allocate(4 * types.Length, UnsafeUtility.AlignOf<int>());
                int index = 0;
                while (true)
                {
                    if (index == types.Length)
                    {
                        break;
                    }
                    outTypes + (((IntPtr) index) * 4) = (int*) types[index].TypeIndex;
                    index++;
                }
            }
        }

        public unsafe ComponentGroup CreateEntityGroup(ArchetypeManager typeMan, EntityDataManager* entityDataManager, ComponentType[] requiredComponents)
        {
            ComponentType* typePtr;
            int num;
            this.CreateRequiredComponents(requiredComponents, out typePtr, out num);
            return this.CreateEntityGroup(typeMan, entityDataManager, this.CreateQuery(requiredComponents), 1, typePtr, num);
        }

        public unsafe ComponentGroup CreateEntityGroup(ArchetypeManager typeMan, EntityDataManager* entityDataManager, EntityArchetypeQuery[] query) => 
            this.CreateEntityGroup(typeMan, entityDataManager, this.CreateQuery(query), query.Length, null, 0);

        public unsafe ComponentGroup CreateEntityGroup(ArchetypeManager typeMan, EntityDataManager* entityDataManager, ArchetypeQuery* archetypeQueries, int archetypeFiltersCount, ComponentType* requiredComponents, int requiredComponentsCount)
        {
            EntityGroupData* grp = (EntityGroupData*) ref this.m_GroupDataChunkAllocator.Allocate(sizeof(EntityGroupData), 8);
            grp->PrevGroup = this.m_LastGroupData;
            this.m_LastGroupData = grp;
            grp->RequiredComponentsCount = requiredComponentsCount;
            grp->RequiredComponents = requiredComponents;
            this.InitializeReaderWriter(grp, requiredComponents, requiredComponentsCount);
            grp->ArchetypeQuery = archetypeQueries;
            grp->ArchetypeQueryCount = archetypeFiltersCount;
            grp->FirstMatchingArchetype = null;
            grp->LastMatchingArchetype = null;
            for (Archetype* archetypePtr = typeMan.m_LastArchetype; archetypePtr != null; archetypePtr = archetypePtr->PrevArchetype)
            {
                this.AddArchetypeIfMatching(archetypePtr, grp);
            }
            return new ComponentGroup(grp, this.m_JobSafetyManager, typeMan, entityDataManager);
        }

        private unsafe ArchetypeQuery* CreateQuery(ComponentType[] requiredTypes)
        {
            ArchetypeQuery* queryPtr = (ArchetypeQuery*) ref this.m_GroupDataChunkAllocator.Allocate(sizeof(ArchetypeQuery), UnsafeUtility.AlignOf<ArchetypeQuery>());
            int index = 0;
            int num2 = 0;
            int num3 = 0;
            while (true)
            {
                if (num3 == requiredTypes.Length)
                {
                    queryPtr->All = (int*) this.m_GroupDataChunkAllocator.Allocate(4 * num2, UnsafeUtility.AlignOf<int>());
                    queryPtr->AllCount = num2;
                    queryPtr->None = (int*) this.m_GroupDataChunkAllocator.Allocate(4 * index, UnsafeUtility.AlignOf<int>());
                    queryPtr->NoneCount = index;
                    queryPtr->Any = null;
                    queryPtr->AnyCount = 0;
                    index = 0;
                    num2 = 0;
                    for (int i = 0; i != requiredTypes.Length; i++)
                    {
                        if (requiredTypes[i].AccessModeType == ComponentType.AccessMode.Subtractive)
                        {
                            index++;
                            queryPtr->None[index] = requiredTypes[i].TypeIndex;
                        }
                        else
                        {
                            num2++;
                            queryPtr->All[num2] = requiredTypes[i].TypeIndex;
                        }
                    }
                    return queryPtr;
                }
                if (requiredTypes[num3].AccessModeType == ComponentType.AccessMode.Subtractive)
                {
                    index++;
                }
                else
                {
                    num2++;
                }
                num3++;
            }
        }

        private unsafe ArchetypeQuery* CreateQuery(EntityArchetypeQuery[] query)
        {
            ArchetypeQuery* queryPtr = (ArchetypeQuery*) this.m_GroupDataChunkAllocator.Allocate(sizeof(ArchetypeQuery) * query.Length, UnsafeUtility.AlignOf<ArchetypeQuery>());
            for (int i = 0; i != query.Length; i++)
            {
                this.ConstructTypeArray(query[i].None, out queryPtr[i].None, out queryPtr[i].NoneCount);
                this.ConstructTypeArray(query[i].All, out queryPtr[i].All, out queryPtr[i].AllCount);
                this.ConstructTypeArray(query[i].Any, out queryPtr[i].Any, out queryPtr[i].AnyCount);
            }
            return queryPtr;
        }

        private unsafe void CreateRequiredComponents(ComponentType[] requiredComponents, out ComponentType* types, out int typesCount)
        {
            types = (ComponentType*) this.m_GroupDataChunkAllocator.Allocate(sizeof(ComponentType) * (requiredComponents.Length + 1), UnsafeUtility.AlignOf<ComponentType>());
            types = (ComponentType*) ComponentType.Create<Entity>();
            int index = 0;
            while (true)
            {
                if (index == requiredComponents.Length)
                {
                    typesCount = requiredComponents.Length + 1;
                    return;
                }
                types + (((IntPtr) (index + 1)) * sizeof(ComponentType)) = (ComponentType*) requiredComponents[index];
                index++;
            }
        }

        public void Dispose()
        {
            this.m_GroupDataChunkAllocator.Dispose();
        }

        private unsafe void InitializeReaderWriter(EntityGroupData* grp, ComponentType* requiredTypes, int requiredCount)
        {
            grp.ReaderTypesCount = 0;
            grp.WriterTypesCount = 0;
            int index = 0;
            while (true)
            {
                if (index == requiredCount)
                {
                    grp.ReaderTypes = (int*) this.m_GroupDataChunkAllocator.Allocate(4 * grp.ReaderTypesCount, 4);
                    grp.WriterTypes = (int*) this.m_GroupDataChunkAllocator.Allocate(4 * grp.WriterTypesCount, 4);
                    int num = 0;
                    int num2 = 0;
                    for (int i = 0; i != requiredCount; i++)
                    {
                        if ((requiredTypes + i).RequiresJobDependency)
                        {
                            if (requiredTypes[i].AccessModeType != ComponentType.AccessMode.ReadOnly)
                            {
                                num2++;
                                grp.WriterTypes[num2] = requiredTypes[i].TypeIndex;
                            }
                            else
                            {
                                num++;
                                grp.ReaderTypes[num] = requiredTypes[i].TypeIndex;
                            }
                        }
                    }
                    return;
                }
                if ((requiredTypes + index).RequiresJobDependency)
                {
                    if (requiredTypes[index].AccessModeType != ComponentType.AccessMode.ReadOnly)
                    {
                        int* numPtr1 = (int*) ref grp.WriterTypesCount;
                        numPtr1[0]++;
                    }
                    else
                    {
                        int* numPtr2 = (int*) ref grp.ReaderTypesCount;
                        numPtr2[0]++;
                    }
                }
                index++;
            }
        }

        private static unsafe bool IsMatchingArchetype(Archetype* archetype, ArchetypeQuery* query)
        {
            bool flag2;
            if (!TestMatchingArchetypeAll(archetype, query.All, query.AllCount))
            {
                flag2 = false;
            }
            else if (!TestMatchingArchetypeNone(archetype, query.None, query.NoneCount))
            {
                flag2 = false;
            }
            else if (!TestMatchingArchetypeAny(archetype, query.Any, query.AnyCount))
            {
                flag2 = false;
            }
            else
            {
                flag2 = true;
            }
            return flag2;
        }

        private static unsafe bool IsMatchingArchetype(Archetype* archetype, EntityGroupData* group)
        {
            int num = 0;
            while (true)
            {
                bool flag2;
                if (num == group.ArchetypeQueryCount)
                {
                    flag2 = false;
                }
                else
                {
                    if (!IsMatchingArchetype(archetype, group.ArchetypeQuery + num))
                    {
                        num++;
                        continue;
                    }
                    flag2 = true;
                }
                return flag2;
            }
        }

        private static unsafe bool TestMatchingArchetypeAll(Archetype* archetype, int* allTypes, int allCount)
        {
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            int num2 = 0;
            int typeIndex = TypeManager.GetTypeIndex<Disabled>();
            int num4 = TypeManager.GetTypeIndex<Prefab>();
            bool flag = false;
            bool flag2 = false;
            int index = 0;
            while (true)
            {
                if (index >= typesCount)
                {
                    bool flag9;
                    if (archetype.Disabled && !flag)
                    {
                        flag9 = false;
                    }
                    else if (archetype.Prefab && !flag2)
                    {
                        flag9 = false;
                    }
                    else
                    {
                        flag9 = num2 == allCount;
                    }
                    return flag9;
                }
                int num6 = types[index].TypeIndex;
                int num7 = 0;
                while (true)
                {
                    if (num7 >= allCount)
                    {
                        index++;
                        break;
                    }
                    int num8 = allTypes[num7];
                    if (num8 == typeIndex)
                    {
                        flag = true;
                    }
                    if (num8 == num4)
                    {
                        flag2 = true;
                    }
                    if (num6 == num8)
                    {
                        num2++;
                    }
                    num7++;
                }
            }
        }

        private static unsafe bool TestMatchingArchetypeAny(Archetype* archetype, int* anyTypes, int anyCount)
        {
            ComponentTypeInArchetype* types;
            int typesCount;
            int num2;
            if (anyCount != 0)
            {
                types = archetype.Types;
                typesCount = archetype.TypesCount;
                num2 = 0;
            }
            else
            {
                return true;
            }
            while (true)
            {
                while (true)
                {
                    bool flag2;
                    if (num2 < typesCount)
                    {
                        int typeIndex = types[num2].TypeIndex;
                        int index = 0;
                        while (true)
                        {
                            if (index < anyCount)
                            {
                                int num5 = anyTypes[index];
                                if (typeIndex != num5)
                                {
                                    index++;
                                    continue;
                                }
                                flag2 = true;
                            }
                            else
                            {
                                num2++;
                                continue;
                            }
                            break;
                        }
                    }
                    else
                    {
                        flag2 = false;
                    }
                    return flag2;
                }
            }
        }

        private static unsafe bool TestMatchingArchetypeNone(Archetype* archetype, int* noneTypes, int noneCount)
        {
            bool flag2;
            ComponentTypeInArchetype* types = archetype.Types;
            int typesCount = archetype.TypesCount;
            int index = 0;
            while (true)
            {
                if (index < typesCount)
                {
                    int typeIndex = types[index].TypeIndex;
                    int num4 = 0;
                    while (true)
                    {
                        if (num4 < noneCount)
                        {
                            int num5 = noneTypes[num4];
                            if (typeIndex != num5)
                            {
                                num4++;
                                continue;
                            }
                            return false;
                        }
                        else
                        {
                            index++;
                        }
                        break;
                    }
                    continue;
                }
                else
                {
                    flag2 = true;
                }
                break;
            }
            return flag2;
        }
    }
}

