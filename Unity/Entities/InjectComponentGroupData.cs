namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    internal class InjectComponentGroupData
    {
        private readonly InjectionData[] m_ComponentDataInjections;
        private readonly int m_EntityArrayOffset;
        private readonly InjectionData[] m_BufferArrayInjections;
        private readonly int m_GroupFieldOffset;
        private readonly InjectionContext m_InjectionContext;
        private readonly int m_LengthOffset;
        private readonly InjectionData[] m_SharedComponentInjections;
        private readonly ComponentGroup m_EntityGroup;
        private readonly int m_ComponentGroupIndex;

        private unsafe InjectComponentGroupData(ComponentSystemBase system, FieldInfo groupField, InjectionData[] componentDataInjections, InjectionData[] bufferArrayInjections, InjectionData[] sharedComponentInjections, FieldInfo entityArrayInjection, FieldInfo indexFromEntityInjection, InjectionContext injectionContext, FieldInfo lengthInjection, FieldInfo componentGroupIndexField, ComponentType[] componentRequirements)
        {
            this.m_EntityGroup = system.GetComponentGroupInternal(componentRequirements);
            this.m_ComponentGroupIndex = Array.IndexOf<ComponentGroup>(system.ComponentGroups, this.m_EntityGroup);
            this.m_ComponentDataInjections = componentDataInjections;
            this.m_BufferArrayInjections = bufferArrayInjections;
            this.m_SharedComponentInjections = sharedComponentInjections;
            this.m_InjectionContext = injectionContext;
            this.PatchGetIndexInComponentGroup(this.m_ComponentDataInjections);
            this.PatchGetIndexInComponentGroup(this.m_BufferArrayInjections);
            this.PatchGetIndexInComponentGroup(this.m_SharedComponentInjections);
            injectionContext.PrepareEntries(this.m_EntityGroup);
            if (entityArrayInjection != null)
            {
                this.m_EntityArrayOffset = UnsafeUtility.GetFieldOffset(entityArrayInjection);
            }
            else
            {
                this.m_EntityArrayOffset = -1;
            }
            if (lengthInjection != null)
            {
                this.m_LengthOffset = UnsafeUtility.GetFieldOffset(lengthInjection);
            }
            else
            {
                this.m_LengthOffset = -1;
            }
            this.m_GroupFieldOffset = UnsafeUtility.GetFieldOffset(groupField);
            if (componentGroupIndexField != null)
            {
                ulong num;
                UnsafeUtility.CopyStructureToPtr<int>(ref this.m_ComponentGroupIndex, (UnsafeUtility.PinGCObjectAndGetAddress(system, out num) + this.m_GroupFieldOffset) + UnsafeUtility.GetFieldOffset(componentGroupIndexField));
                UnsafeUtility.ReleaseGCObject(num);
            }
        }

        private static string CollectInjectedGroup(ComponentSystemBase system, FieldInfo groupField, Type injectedGroupType, out FieldInfo entityArrayField, out FieldInfo indexFromEntityField, InjectionContext injectionContext, out FieldInfo lengthField, out FieldInfo componentGroupIndexField, ISet<ComponentType> componentRequirements, ICollection<InjectionData> componentDataInjections, ICollection<InjectionData> bufferDataInjections, ICollection<InjectionData> sharedComponentInjections)
        {
            string str;
            FieldInfo[] fields = injectedGroupType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            entityArrayField = null;
            indexFromEntityField = null;
            lengthField = null;
            componentGroupIndexField = null;
            FieldInfo[] infoArray2 = fields;
            int index = 0;
            while (true)
            {
                if (index >= infoArray2.Length)
                {
                    if (injectionContext.HasComponentRequirements)
                    {
                        foreach (ComponentType type in injectionContext.ComponentRequirements)
                        {
                            componentRequirements.Add(type);
                        }
                    }
                    str = null;
                    break;
                }
                FieldInfo field = infoArray2[index];
                bool isReadOnly = field.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0;
                if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataArray<>)))
                {
                    InjectionData item = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                    componentDataInjections.Add(item);
                    componentRequirements.Add(item.ComponentType);
                }
                else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(SubtractiveComponent<>)))
                {
                    componentRequirements.Add(ComponentType.Subtractive(field.FieldType.GetGenericArguments()[0]));
                }
                else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(BufferArray<>)))
                {
                    InjectionData item = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                    bufferDataInjections.Add(item);
                    componentRequirements.Add(item.ComponentType);
                }
                else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(SharedComponentDataArray<>)))
                {
                    if (!isReadOnly)
                    {
                        str = $"{system.GetType().Name}:{groupField.Name} SharedComponentDataArray<> must always be injected as [ReadOnly]";
                        break;
                    }
                    InjectionData item = new InjectionData(field, field.FieldType.GetGenericArguments()[0], true);
                    sharedComponentInjections.Add(item);
                    componentRequirements.Add(item.ComponentType);
                }
                else if (field.FieldType == typeof(EntityArray))
                {
                    if (entityArrayField != null)
                    {
                        str = $"{system.GetType().Name}:{groupField.Name} An [Inject] struct, may only contain a single EntityArray";
                        break;
                    }
                    entityArrayField = field;
                }
                else if (!(field.FieldType == typeof(int)))
                {
                    InjectionHook hook = InjectionHookSupport.HookFor(field);
                    if (hook == null)
                    {
                        str = $"{system.GetType().Name}:{groupField.Name} [Inject] may only be used on ComponentDataArray<>, ComponentArray<>, TransformAccessArray, EntityArray, {string.Join(",", (IEnumerable<string>) (from h in InjectionHookSupport.Hooks select h.FieldTypeOfInterest.Name))} and int Length.";
                        break;
                    }
                    string str2 = hook.ValidateField(field, isReadOnly, injectionContext);
                    if (str2 != null)
                    {
                        str = str2;
                        break;
                    }
                    injectionContext.AddEntry(hook.CreateInjectionInfoFor(field, isReadOnly));
                }
                else
                {
                    if ((field.Name != "Length") && (field.Name != "GroupIndex"))
                    {
                        str = $"{system.GetType().Name}:{groupField.Name} Int in an [Inject] struct should be named "Length" (group length) or "GroupIndex" (index in ComponentGroup[])";
                        break;
                    }
                    if (!field.IsInitOnly)
                    {
                        str = $"{system.GetType().Name}:{groupField.Name} {field.Name} must use the "readonly" keyword";
                        break;
                    }
                    if (field.Name == "Length")
                    {
                        lengthField = field;
                    }
                    if (field.Name == "GroupIndex")
                    {
                        componentGroupIndexField = field;
                    }
                }
                index++;
            }
            return str;
        }

        public static InjectComponentGroupData CreateInjection(Type injectedGroupType, FieldInfo groupField, ComponentSystemBase system)
        {
            FieldInfo info;
            FieldInfo info2;
            FieldInfo info3;
            FieldInfo info4;
            InjectionContext injectionContext = new InjectionContext();
            List<InjectionData> componentDataInjections = new List<InjectionData>();
            List<InjectionData> bufferDataInjections = new List<InjectionData>();
            List<InjectionData> sharedComponentInjections = new List<InjectionData>();
            HashSet<ComponentType> componentRequirements = new HashSet<ComponentType>();
            string message = CollectInjectedGroup(system, groupField, injectedGroupType, out info, out info2, injectionContext, out info3, out info4, componentRequirements, componentDataInjections, bufferDataInjections, sharedComponentInjections);
            if (message != null)
            {
                throw new ArgumentException(message);
            }
            return new InjectComponentGroupData(system, groupField, componentDataInjections.ToArray(), bufferDataInjections.ToArray(), sharedComponentInjections.ToArray(), info, info2, injectionContext, info3, info4, componentRequirements.ToArray<ComponentType>());
        }

        private void PatchGetIndexInComponentGroup(InjectionData[] componentInjections)
        {
            for (int i = 0; i != componentInjections.Length; i++)
            {
                componentInjections[i].IndexInComponentGroup = this.m_EntityGroup.GetIndexInComponentGroup(componentInjections[i].ComponentType.TypeIndex);
            }
        }

        public unsafe void UpdateInjection(byte* systemPtr)
        {
            int num;
            ComponentChunkIterator iterator;
            byte* groupStructPtr = systemPtr + this.m_GroupFieldOffset;
            this.m_EntityGroup.GetComponentChunkIterator(out num, out iterator);
            int index = 0;
            while (true)
            {
                ComponentDataArray<ProxyComponentData> array;
                if (index == this.m_ComponentDataInjections.Length)
                {
                    int num3 = 0;
                    while (true)
                    {
                        SharedComponentDataArray<ProxySharedComponentData> array2;
                        if (num3 == this.m_SharedComponentInjections.Length)
                        {
                            int num4 = 0;
                            while (true)
                            {
                                BufferArray<ProxyBufferElementData> array3;
                                if (num4 == this.m_BufferArrayInjections.Length)
                                {
                                    if (this.m_EntityArrayOffset != -1)
                                    {
                                        EntityArray array4;
                                        this.m_EntityGroup.GetEntityArray(ref iterator, num, out array4);
                                        UnsafeUtility.CopyStructureToPtr<EntityArray>(ref array4, (void*) (groupStructPtr + this.m_EntityArrayOffset));
                                    }
                                    if (this.m_InjectionContext.HasEntries)
                                    {
                                        this.m_InjectionContext.UpdateEntries(this.m_EntityGroup, ref iterator, num, groupStructPtr);
                                    }
                                    if (this.m_LengthOffset != -1)
                                    {
                                        UnsafeUtility.CopyStructureToPtr<int>(ref num, (void*) (groupStructPtr + this.m_LengthOffset));
                                    }
                                    return;
                                }
                                this.m_EntityGroup.GetBufferArray<ProxyBufferElementData>(ref iterator, this.m_BufferArrayInjections[num4].IndexInComponentGroup, num, out array3);
                                UnsafeUtility.CopyStructureToPtr<BufferArray<ProxyBufferElementData>>(ref array3, (void*) (groupStructPtr + this.m_BufferArrayInjections[num4].FieldOffset));
                                num4++;
                            }
                        }
                        this.m_EntityGroup.GetSharedComponentDataArray<ProxySharedComponentData>(ref iterator, this.m_SharedComponentInjections[num3].IndexInComponentGroup, num, out array2);
                        UnsafeUtility.CopyStructureToPtr<SharedComponentDataArray<ProxySharedComponentData>>(ref array2, (void*) (groupStructPtr + this.m_SharedComponentInjections[num3].FieldOffset));
                        num3++;
                    }
                }
                this.m_EntityGroup.GetComponentDataArray<ProxyComponentData>(ref iterator, this.m_ComponentDataInjections[index].IndexInComponentGroup, num, out array);
                UnsafeUtility.CopyStructureToPtr<ComponentDataArray<ProxyComponentData>>(ref array, (void*) (groupStructPtr + this.m_ComponentDataInjections[index].FieldOffset));
                index++;
            }
        }

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly InjectComponentGroupData.<>c <>9 = new InjectComponentGroupData.<>c();
            public static Func<InjectionHook, string> <>9__13_0;

            internal string <CollectInjectedGroup>b__13_0(InjectionHook h) => 
                h.FieldTypeOfInterest.Name;
        }
    }
}

