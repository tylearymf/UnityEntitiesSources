namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    internal static class IJobProcessComponentDataUtility
    {
        public static ComponentType[] GetComponentTypes(Type jobType)
        {
            ComponentType[] typeArray2;
            Type iJobProcessComponentDataInterface = GetIJobProcessComponentDataInterface(jobType);
            if (iJobProcessComponentDataInterface != null)
            {
                int num;
                ComponentType[] typeArray;
                typeArray2 = GetComponentTypes(jobType, iJobProcessComponentDataInterface, out num, out typeArray);
            }
            else
            {
                typeArray2 = null;
            }
            return typeArray2;
        }

        private static unsafe ComponentType[] GetComponentTypes(Type jobType, Type interfaceType, out int processCount, out ComponentType[] changedFilter)
        {
            Type[] genericArguments = interfaceType.GetGenericArguments();
            ParameterInfo[] parameters = jobType.GetMethod("Execute").GetParameters();
            List<ComponentType> list = new List<ComponentType>();
            List<ComponentType> list2 = new List<ComponentType>();
            int num = (genericArguments.Length != parameters.Length) ? 2 : 0;
            int index = 0;
            while (true)
            {
                ComponentType type;
                if (index >= genericArguments.Length)
                {
                    RequireSubtractiveComponentAttribute customAttribute = jobType.GetCustomAttribute<RequireSubtractiveComponentAttribute>();
                    if (customAttribute != null)
                    {
                        foreach (Type type2 in customAttribute.SubtractiveComponents)
                        {
                            list.Add(ComponentType.Subtractive(type2));
                        }
                    }
                    RequireComponentTagAttribute attribute2 = jobType.GetCustomAttribute<RequireComponentTagAttribute>();
                    if (attribute2 != null)
                    {
                        foreach (Type type3 in attribute2.TagComponents)
                        {
                            list.Add(ComponentType.ReadOnly(type3));
                        }
                    }
                    processCount = genericArguments.Length;
                    changedFilter = list2.ToArray();
                    return list.ToArray();
                }
                bool flag = parameters[index + num].GetCustomAttribute(typeof(ReadOnlyAttribute)) != null;
                ComponentType* typePtr1 = (ComponentType*) new ComponentType(genericArguments[index], flag ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite);
                typePtr1 = (ComponentType*) ref type;
                list.Add(type);
                if (parameters[index + num].GetCustomAttribute(typeof(ChangedFilterAttribute)) != null)
                {
                    list2.Add(type);
                }
                index++;
            }
        }

        private static Type GetIJobProcessComponentDataInterface(Type jobType)
        {
            Type[] interfaces = jobType.GetInterfaces();
            int index = 0;
            while (true)
            {
                Type type2;
                if (index >= interfaces.Length)
                {
                    type2 = null;
                }
                else
                {
                    Type type = interfaces[index];
                    if (!((type.Assembly == typeof(IBaseJobProcessComponentData).Assembly) && type.Name.StartsWith("IJobProcessComponentData")))
                    {
                        index++;
                        continue;
                    }
                    type2 = type;
                }
                return type2;
            }
        }

        private static IntPtr GetJobReflection(Type jobType, Type wrapperJobType, Type interfaceType, bool isIJobParallelFor)
        {
            Assert.AreNotEqual<Type>(null, wrapperJobType);
            Assert.AreNotEqual<Type>(null, interfaceType);
            List<Type> list = new List<Type> {
                jobType
            };
            list.AddRange(interfaceType.GetGenericArguments());
            Type type = wrapperJobType.MakeGenericType(list.ToArray());
            object[] parameters = new object[] { isIJobParallelFor ? JobType.ParallelFor : JobType.Single };
            return (IntPtr) type.GetMethod("Initialize").Invoke(null, parameters);
        }

        internal static unsafe void Initialize(ComponentSystemBase system, Type jobType, Type wrapperJobType, bool isParallelFor, ref JobProcessComponentDataCache cache, out ProcessIterationData iterator)
        {
            int num;
            int num1;
            if (!isParallelFor || !(cache.JobReflectionDataParallelFor == IntPtr.Zero))
            {
                num1 = isParallelFor ? 0 : ((int) (cache.JobReflectionData == IntPtr.Zero));
            }
            else
            {
                num1 = 1;
            }
            if (num1 != 0)
            {
                Type iJobProcessComponentDataInterface = GetIJobProcessComponentDataInterface(jobType);
                if (cache.Types == null)
                {
                    cache.Types = GetComponentTypes(jobType, iJobProcessComponentDataInterface, out cache.ProcessTypesCount, out cache.FilterChanged);
                }
                IntPtr ptr = GetJobReflection(jobType, wrapperJobType, iJobProcessComponentDataInterface, isParallelFor);
                if (isParallelFor)
                {
                    cache.JobReflectionDataParallelFor = ptr;
                }
                else
                {
                    cache.JobReflectionData = ptr;
                }
            }
            if (cache.ComponentSystem != system)
            {
                cache.ComponentGroup = system.GetComponentGroupInternal(cache.Types);
                if (cache.FilterChanged.Length != 0)
                {
                    cache.ComponentGroup.SetFilterChanged(cache.FilterChanged);
                }
                else
                {
                    cache.ComponentGroup.ResetFilter();
                }
                cache.ComponentSystem = system;
            }
            ComponentGroup componentGroup = cache.ComponentGroup;
            iterator.IsReadOnly3 = num = 0;
            iterator.IsReadOnly2 = num = num;
            iterator.IsReadOnly1 = num = num;
            iterator.IsReadOnly0 = num;
            int* numPtr = &iterator.IsReadOnly0;
            int index = 0;
            while (true)
            {
                if (index == cache.ProcessTypesCount)
                {
                    fixed (int* numRef = null)
                    {
                        componentGroup.GetComponentChunkIterator(out iterator.Iterator);
                        iterator.IndexInGroup3 = num = -1;
                        iterator.IndexInGroup2 = num = num;
                        iterator.IndexInGroup0 = iterator.IndexInGroup1 = num;
                        int* numPtr2 = &iterator.IndexInGroup0;
                        int num3 = 0;
                        while (true)
                        {
                            if (num3 == cache.ProcessTypesCount)
                            {
                                fixed (int* numRef2 = null)
                                {
                                    iterator.m_IsParallelFor = isParallelFor;
                                    iterator.m_Length = componentGroup.CalculateNumberOfChunksWithoutFiltering();
                                    iterator.m_MaxIndex = iterator.m_Length - 1;
                                    iterator.m_MinIndex = 0;
                                    AtomicSafetyHandle handle = new AtomicSafetyHandle();
                                    iterator.m_Safety3 = handle = handle;
                                    iterator.m_Safety2 = handle = handle;
                                    iterator.m_Safety0 = iterator.m_Safety1 = handle;
                                    iterator.m_SafetyReadOnlyCount = 0;
                                    AtomicSafetyHandle* handlePtr = &iterator.m_Safety0;
                                    int num4 = 0;
                                    while (true)
                                    {
                                        if (num4 == cache.ProcessTypesCount)
                                        {
                                            fixed (AtomicSafetyHandle* handleRef = null)
                                            {
                                                iterator.m_SafetyReadWriteCount = 0;
                                                AtomicSafetyHandle* handlePtr2 = &iterator.m_Safety0;
                                                int num5 = 0;
                                                while (true)
                                                {
                                                    if (num5 == cache.ProcessTypesCount)
                                                    {
                                                        fixed (AtomicSafetyHandle* handleRef2 = null)
                                                        {
                                                            Assert.AreEqual(cache.ProcessTypesCount, iterator.m_SafetyReadWriteCount + iterator.m_SafetyReadOnlyCount);
                                                            return;
                                                        }
                                                    }
                                                    if (cache.Types[num5].AccessModeType == ComponentType.AccessMode.ReadWrite)
                                                    {
                                                        handlePtr2[iterator.m_SafetyReadOnlyCount + iterator.m_SafetyReadWriteCount] = componentGroup.GetSafetyHandle(componentGroup.GetIndexInComponentGroup(cache.Types[num5].TypeIndex));
                                                        int* numPtr1 = (int*) ref iterator.m_SafetyReadWriteCount;
                                                        numPtr1[0]++;
                                                    }
                                                    num5++;
                                                }
                                            }
                                        }
                                        if (cache.Types[num4].AccessModeType == ComponentType.AccessMode.ReadOnly)
                                        {
                                            handlePtr[iterator.m_SafetyReadOnlyCount] = componentGroup.GetSafetyHandle(componentGroup.GetIndexInComponentGroup(cache.Types[num4].TypeIndex));
                                            int* numPtr3 = (int*) ref iterator.m_SafetyReadOnlyCount;
                                            numPtr3[0]++;
                                        }
                                        num4++;
                                    }
                                }
                            }
                            numPtr2[num3] = componentGroup.GetIndexInComponentGroup(cache.Types[num3].TypeIndex);
                            num3++;
                        }
                    }
                }
                numPtr[index] = (cache.Types[index].AccessModeType == ComponentType.AccessMode.ReadOnly) ? 1 : 0;
                index++;
            }
        }

        internal static void PrepareComponentGroup(ComponentSystemBase system, Type jobType)
        {
            ComponentType[] typeArray;
            int num;
            Type iJobProcessComponentDataInterface = GetIJobProcessComponentDataInterface(jobType);
            ComponentType[] componentTypes = GetComponentTypes(jobType, iJobProcessComponentDataInterface, out num, out typeArray);
            system.GetComponentGroupInternal(componentTypes);
        }
    }
}

