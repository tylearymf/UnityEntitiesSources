namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    internal class ComponentGroupArrayStaticCache
    {
        public readonly Type CachedType;
        internal readonly int ComponentCount;
        internal readonly int ComponentDataCount;
        internal readonly int[] ComponentFieldOffsets;
        internal readonly Unity.Entities.ComponentGroup ComponentGroup;
        internal readonly ComponentType[] ComponentTypes;
        internal readonly ComponentJobSafetyManager SafetyManager;

        public ComponentGroupArrayStaticCache(Type type, EntityManager entityManager, ComponentSystemBase system)
        {
            List<int> collection = new List<int>();
            List<ComponentType> list2 = new List<ComponentType>();
            List<int> list3 = new List<int>();
            List<ComponentType> list4 = new List<ComponentType>();
            List<ComponentType> list5 = new List<ComponentType>();
            foreach (FieldInfo info in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                Type fieldType = info.FieldType;
                int fieldOffset = UnsafeUtility.GetFieldOffset(info);
                if (fieldType.IsPointer)
                {
                    ComponentType.AccessMode accessModeType = (info.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0) ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite;
                    Type elementType = fieldType.GetElementType();
                    if (!typeof(IComponentData).IsAssignableFrom(elementType) && (elementType != typeof(Entity)))
                    {
                        throw new ArgumentException($"{type}.{info.Name} is a pointer type but not a IComponentData. Only IComponentData or Entity may be a pointer type for enumeration.");
                    }
                    list3.Add(fieldOffset);
                    list4.Add(new ComponentType(elementType, accessModeType));
                }
                else
                {
                    bool flag1;
                    if (TypeManager.UnityEngineComponentType != null)
                    {
                        flag1 = TypeManager.UnityEngineComponentType.IsAssignableFrom(fieldType);
                    }
                    else
                    {
                        object unityEngineComponentType = TypeManager.UnityEngineComponentType;
                        flag1 = false;
                    }
                    if (flag1)
                    {
                        collection.Add(fieldOffset);
                        list2.Add(fieldType);
                    }
                    else
                    {
                        if (!(fieldType.IsGenericType && (fieldType.GetGenericTypeDefinition() == typeof(SubtractiveComponent<>))))
                        {
                            if (!typeof(IComponentData).IsAssignableFrom(fieldType))
                            {
                                throw new ArgumentException($"{type}.{info.Name} can not be used in a component enumerator");
                            }
                            throw new ArgumentException($"{type}.{info.Name} must be an unsafe pointer to the {fieldType}. Like this: {fieldType}* {info.Name};");
                        }
                        list5.Add(ComponentType.Subtractive(fieldType.GetGenericArguments()[0]));
                    }
                }
            }
            if ((list2.Count + list4.Count) > 6)
            {
                throw new ArgumentException($"{type} has too many component references. A ComponentGroup Array can have up to {6}.");
            }
            this.ComponentDataCount = list4.Count;
            this.ComponentCount = list2.Count;
            list4.AddRange(list2);
            list4.AddRange(list5);
            this.ComponentTypes = list4.ToArray();
            list3.AddRange(collection);
            this.ComponentFieldOffsets = list3.ToArray();
            this.ComponentGroup = system.GetComponentGroupInternal(this.ComponentTypes);
            this.SafetyManager = entityManager.ComponentJobSafetyManager;
            this.CachedType = type;
        }
    }
}

