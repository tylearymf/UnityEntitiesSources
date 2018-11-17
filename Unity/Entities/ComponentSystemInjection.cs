namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;

    internal static class ComponentSystemInjection
    {
        internal static T[] GetAllInjectedManagers<T>(ScriptBehaviourManager host, World world) where T: ScriptBehaviourManager
        {
            List<T> list = new List<T>();
            foreach (FieldInfo info in host.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                object[] customAttributes = info.GetCustomAttributes(typeof(InjectAttribute), true);
                bool flag = customAttributes.Length == 0;
                if ((!flag && info.FieldType.IsClass) && info.FieldType.IsSubclassOf(typeof(T)))
                {
                    list.Add((T) world.GetOrCreateManager(info.FieldType));
                }
            }
            return list.ToArray();
        }

        public static string GetFieldString(FieldInfo info) => 
            $"{info.DeclaringType.Name}.{info.Name}";

        public static void Inject(ComponentSystemBase componentSystem, World world, EntityManager entityManager, out InjectComponentGroupData[] outInjectGroups, out InjectFromEntityData outInjectFromEntityData)
        {
            ValidateNoStaticInjectDependencies(componentSystem.GetType());
            InjectFields(componentSystem, world, entityManager, out outInjectGroups, out outInjectFromEntityData);
        }

        private static void InjectConstructorDependencies(ScriptBehaviourManager manager, World world, FieldInfo field)
        {
            if (field.FieldType.IsSubclassOf(typeof(ScriptBehaviourManager)))
            {
                field.SetValue(manager, world.GetOrCreateManager(field.FieldType));
            }
            else
            {
                ThrowUnsupportedInjectException(field);
            }
        }

        private static void InjectFields(ComponentSystemBase componentSystem, World world, EntityManager entityManager, out InjectComponentGroupData[] outInjectGroups, out InjectFromEntityData outInjectFromEntityData)
        {
            List<InjectComponentGroupData> list = new List<InjectComponentGroupData>();
            List<InjectionData> componentDataFromEntity = new List<InjectionData>();
            List<InjectionData> bufferFromEntity = new List<InjectionData>();
            foreach (FieldInfo info in componentSystem.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                object[] customAttributes = info.GetCustomAttributes(typeof(InjectAttribute), true);
                if (customAttributes.Length != 0)
                {
                    if (info.FieldType.IsClass)
                    {
                        InjectConstructorDependencies(componentSystem, world, info);
                    }
                    else if (InjectFromEntityData.SupportsInjections(info))
                    {
                        InjectFromEntityData.CreateInjection(info, entityManager, componentDataFromEntity, bufferFromEntity);
                    }
                    else
                    {
                        list.Add(InjectComponentGroupData.CreateInjection(info.FieldType, info, componentSystem));
                    }
                }
            }
            outInjectGroups = list.ToArray();
            outInjectFromEntityData = new InjectFromEntityData(componentDataFromEntity.ToArray(), bufferFromEntity.ToArray());
        }

        public static void ThrowUnsupportedInjectException(FieldInfo field)
        {
            throw new ArgumentException($"[Inject] is not supported for type '{field.FieldType}'. At: {GetFieldString(field)}");
        }

        private static void ValidateNoStaticInjectDependencies(Type type)
        {
            foreach (FieldInfo info in type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (info.GetCustomAttributes(typeof(InjectAttribute), true).Length != 0)
                {
                    throw new ArgumentException($"[Inject] may not be used on static variables: {GetFieldString(info)}");
                }
            }
        }
    }
}

