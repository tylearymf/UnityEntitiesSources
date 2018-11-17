namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct InjectFromEntityData
    {
        private readonly InjectionData[] m_InjectComponentDataFromEntity;
        private readonly InjectionData[] m_InjectBufferFromEntity;
        public InjectFromEntityData(InjectionData[] componentDataFromEntity, InjectionData[] bufferFromEntity)
        {
            this.m_InjectComponentDataFromEntity = componentDataFromEntity;
            this.m_InjectBufferFromEntity = bufferFromEntity;
        }

        public static bool SupportsInjections(FieldInfo field)
        {
            bool flag2;
            if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataFromEntity<>)))
            {
                flag2 = true;
            }
            else if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(BufferFromEntity<>)))
            {
                flag2 = true;
            }
            else
            {
                flag2 = false;
            }
            return flag2;
        }

        public static void CreateInjection(FieldInfo field, EntityManager entityManager, List<InjectionData> componentDataFromEntity, List<InjectionData> bufferFromEntity)
        {
            bool isReadOnly = field.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0;
            if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataFromEntity<>)))
            {
                InjectionData item = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                componentDataFromEntity.Add(item);
            }
            else if (!(field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(BufferFromEntity<>))))
            {
                ComponentSystemInjection.ThrowUnsupportedInjectException(field);
            }
            else
            {
                InjectionData item = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                bufferFromEntity.Add(item);
            }
        }

        public unsafe void UpdateInjection(byte* pinnedSystemPtr, EntityManager entityManager)
        {
            int index = 0;
            while (true)
            {
                if (index == this.m_InjectComponentDataFromEntity.Length)
                {
                    for (int i = 0; i != this.m_InjectBufferFromEntity.Length; i++)
                    {
                        BufferFromEntity<ProxyBufferElementData> bufferFromEntity = entityManager.GetBufferFromEntity<ProxyBufferElementData>(this.m_InjectBufferFromEntity[i].ComponentType.TypeIndex, this.m_InjectBufferFromEntity[i].IsReadOnly);
                        UnsafeUtility.CopyStructureToPtr<BufferFromEntity<ProxyBufferElementData>>(ref bufferFromEntity, (void*) (pinnedSystemPtr + this.m_InjectBufferFromEntity[i].FieldOffset));
                    }
                    return;
                }
                ComponentDataFromEntity<ProxyComponentData> componentDataFromEntity = entityManager.GetComponentDataFromEntity<ProxyComponentData>(this.m_InjectComponentDataFromEntity[index].ComponentType.TypeIndex, this.m_InjectComponentDataFromEntity[index].IsReadOnly);
                UnsafeUtility.CopyStructureToPtr<ComponentDataFromEntity<ProxyComponentData>>(ref componentDataFromEntity, (void*) (pinnedSystemPtr + this.m_InjectComponentDataFromEntity[index].FieldOffset));
                index++;
            }
        }

        public void ExtractJobDependencyTypes(ComponentSystemBase system)
        {
            if (this.m_InjectComponentDataFromEntity != null)
            {
                foreach (InjectionData data in this.m_InjectComponentDataFromEntity)
                {
                    system.AddReaderWriter(data.ComponentType);
                }
            }
            if (this.m_InjectBufferFromEntity != null)
            {
                foreach (InjectionData data2 in this.m_InjectBufferFromEntity)
                {
                    system.AddReaderWriter(data2.ComponentType);
                }
            }
        }
    }
}

