namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public abstract class ComponentSystemBase : ScriptBehaviourManager
    {
        private InjectComponentGroupData[] m_InjectedComponentGroups;
        private InjectFromEntityData m_InjectFromEntityData;
        private ComponentGroupArrayStaticCache[] m_CachedComponentGroupArrays;
        private ComponentGroup[] m_ComponentGroups;
        private NativeList<int> m_JobDependencyForReadingManagers;
        private NativeList<int> m_JobDependencyForWritingManagers;
        internal unsafe int* m_JobDependencyForReadingManagersPtr;
        internal int m_JobDependencyForReadingManagersLength;
        internal unsafe int* m_JobDependencyForWritingManagersPtr;
        internal int m_JobDependencyForWritingManagersLength;
        private uint m_LastSystemVersion;
        internal ComponentJobSafetyManager m_SafetyManager;
        internal Unity.Entities.EntityManager m_EntityManager;
        private Unity.Entities.World m_World;
        private bool m_AlwaysUpdateSystem;
        internal bool m_PreviouslyEnabled;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool <Enabled>k__BackingField = true;
        internal int m_SystemID;
        internal static ComponentSystemBase ms_ExecutingSystem;

        protected ComponentSystemBase()
        {
        }

        internal unsafe void AddReaderWriter(ComponentType componentType)
        {
            if (CalculateReaderWriterDependency.Add(componentType, this.m_JobDependencyForReadingManagers, this.m_JobDependencyForWritingManagers))
            {
                this.m_JobDependencyForReadingManagersPtr = (int*) this.m_JobDependencyForReadingManagers.GetUnsafePtr<int>();
                this.m_JobDependencyForWritingManagersPtr = (int*) this.m_JobDependencyForWritingManagers.GetUnsafePtr<int>();
                this.m_JobDependencyForReadingManagersLength = this.m_JobDependencyForReadingManagers.Length;
                this.m_JobDependencyForWritingManagersLength = this.m_JobDependencyForWritingManagers.Length;
                this.CompleteDependencyInternal();
            }
        }

        private void AfterGroupCreated(ComponentGroup group)
        {
            group.SetFilterChangedRequiredVersion(this.m_LastSystemVersion);
            group.DisallowDisposing = "ComponentGroup.Dispose() may not be called on a ComponentGroup created with ComponentSystem.GetComponentGroup. The ComponentGroup will automatically be disposed by the ComponentSystem.";
            ArrayUtilityAdd<ComponentGroup>(ref this.m_ComponentGroups, group);
        }

        protected internal unsafe void AfterUpdateVersioning()
        {
            this.m_LastSystemVersion = this.EntityManager.Entities.GlobalSystemVersion;
        }

        private static void ArrayUtilityAdd<T>(ref T[] array, T item)
        {
            Array.Resize<T>(ref array, array.Length + 1);
            array[array.Length - 1] = item;
        }

        protected internal unsafe void BeforeUpdateVersioning()
        {
            this.m_EntityManager.Entities.IncrementGlobalSystemVersion();
            foreach (ComponentGroup group in this.m_ComponentGroups)
            {
                group.SetFilterChangedRequiredVersion(this.m_LastSystemVersion);
            }
        }

        internal unsafe void CompleteDependencyInternal()
        {
            this.m_SafetyManager.CompleteDependenciesNoChecks(this.m_JobDependencyForReadingManagersPtr, this.m_JobDependencyForReadingManagersLength, this.m_JobDependencyForWritingManagersPtr, this.m_JobDependencyForWritingManagersLength);
        }

        public ArchetypeChunkBufferType<T> GetArchetypeChunkBufferType<T>(bool isReadOnly = false) where T: struct, IBufferElementData
        {
            this.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.Create<T>());
            return this.EntityManager.GetArchetypeChunkBufferType<T>(isReadOnly);
        }

        public ArchetypeChunkComponentType<T> GetArchetypeChunkComponentType<T>(bool isReadOnly = false) where T: struct, IComponentData
        {
            this.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.Create<T>());
            return this.EntityManager.GetArchetypeChunkComponentType<T>(isReadOnly);
        }

        public ArchetypeChunkEntityType GetArchetypeChunkEntityType()
        {
            this.AddReaderWriter(ComponentType.ReadOnly<Entity>());
            return this.EntityManager.GetArchetypeChunkEntityType();
        }

        public ArchetypeChunkSharedComponentType<T> GetArchetypeChunkSharedComponentType<T>() where T: struct, ISharedComponentData
        {
            this.AddReaderWriter(ComponentType.ReadOnly<T>());
            return this.EntityManager.GetArchetypeChunkSharedComponentType<T>();
        }

        public ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(bool isReadOnly = false) where T: struct, IComponentData
        {
            this.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.Create<T>());
            return this.EntityManager.GetComponentDataFromEntity<T>(isReadOnly);
        }

        protected ComponentGroup GetComponentGroup(params ComponentType[] componentTypes) => 
            this.GetComponentGroupInternal(componentTypes);

        protected ComponentGroup GetComponentGroup(params EntityArchetypeQuery[] query) => 
            this.GetComponentGroupInternal(query);

        internal ComponentGroup GetComponentGroupInternal(ComponentType[] componentTypes)
        {
            int index = 0;
            while (true)
            {
                ComponentGroup group2;
                if (index != this.m_ComponentGroups.Length)
                {
                    if (!this.m_ComponentGroups[index].CompareComponents(componentTypes))
                    {
                        index++;
                        continue;
                    }
                    group2 = this.m_ComponentGroups[index];
                }
                else
                {
                    ComponentGroup group = this.EntityManager.CreateComponentGroup(componentTypes);
                    int num2 = 0;
                    while (true)
                    {
                        if (num2 == componentTypes.Length)
                        {
                            this.AfterGroupCreated(group);
                            group2 = group;
                            break;
                        }
                        this.AddReaderWriter(componentTypes[num2]);
                        num2++;
                    }
                }
                return group2;
            }
        }

        internal ComponentGroup GetComponentGroupInternal(EntityArchetypeQuery[] query)
        {
            int index = 0;
            while (true)
            {
                ComponentGroup group2;
                if (index == this.m_ComponentGroups.Length)
                {
                    ComponentGroup group = this.EntityManager.CreateComponentGroup(query);
                    this.AfterGroupCreated(group);
                    group2 = group;
                }
                else
                {
                    if (!this.m_ComponentGroups[index].CompareQuery(query))
                    {
                        index++;
                        continue;
                    }
                    group2 = this.m_ComponentGroups[index];
                }
                return group2;
            }
        }

        protected ComponentGroupArray<T> GetEntities<T>() where T: struct
        {
            int index = 0;
            while (true)
            {
                ComponentGroupArray<T> array;
                if (index == this.m_CachedComponentGroupArrays.Length)
                {
                    ComponentGroupArrayStaticCache item = new ComponentGroupArrayStaticCache(typeof(T), this.EntityManager, this);
                    ArrayUtilityAdd<ComponentGroupArrayStaticCache>(ref this.m_CachedComponentGroupArrays, item);
                    array = new ComponentGroupArray<T>(item);
                }
                else
                {
                    if (!(this.m_CachedComponentGroupArrays[index].CachedType == typeof(T)))
                    {
                        index++;
                        continue;
                    }
                    array = new ComponentGroupArray<T>(this.m_CachedComponentGroupArrays[index]);
                }
                return array;
            }
        }

        internal ComponentSystemBase GetSystemFromSystemID(Unity.Entities.World world, int systemID)
        {
            using (IEnumerator<ScriptBehaviourManager> enumerator = world.BehaviourManagers.GetEnumerator())
            {
                while (true)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    ScriptBehaviourManager current = enumerator.Current;
                    ComponentSystemBase base2 = current as ComponentSystemBase;
                    if ((base2 != null) && (base2.m_SystemID == systemID))
                    {
                        return base2;
                    }
                }
            }
            return null;
        }

        private void InjectNestedIJobProcessComponentDataJobs()
        {
            foreach (Type type in base.GetType().GetNestedTypes(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public))
            {
                this.GetComponentGroupForIJobProcessComponentData(type);
            }
        }

        protected sealed override void OnAfterDestroyManagerInternal()
        {
            foreach (ComponentGroup group in this.m_ComponentGroups)
            {
                group.DisallowDisposing = null;
                group.Dispose();
            }
            this.m_ComponentGroups = null;
            this.m_InjectedComponentGroups = null;
            this.m_CachedComponentGroupArrays = null;
            if (this.m_JobDependencyForReadingManagers.IsCreated)
            {
                this.m_JobDependencyForReadingManagers.Dispose();
            }
            if (this.m_JobDependencyForWritingManagers.IsCreated)
            {
                this.m_JobDependencyForWritingManagers.Dispose();
            }
        }

        protected override void OnBeforeCreateManagerInternal(Unity.Entities.World world)
        {
            this.m_SystemID = world.AllocateSystemID();
            this.m_World = world;
            this.m_EntityManager = world.GetOrCreateManager<Unity.Entities.EntityManager>();
            this.m_SafetyManager = this.m_EntityManager.ComponentJobSafetyManager;
            this.m_AlwaysUpdateSystem = base.GetType().GetCustomAttributes(typeof(AlwaysUpdateSystemAttribute), true).Length != 0;
            this.m_ComponentGroups = new ComponentGroup[0];
            this.m_CachedComponentGroupArrays = new ComponentGroupArrayStaticCache[0];
            this.m_JobDependencyForReadingManagers = new NativeList<int>(10, Allocator.Persistent);
            this.m_JobDependencyForWritingManagers = new NativeList<int>(10, Allocator.Persistent);
            ComponentSystemInjection.Inject(this, world, this.m_EntityManager, out this.m_InjectedComponentGroups, out this.m_InjectFromEntityData);
            this.m_InjectFromEntityData.ExtractJobDependencyTypes(this);
            this.InjectNestedIJobProcessComponentDataJobs();
            this.UpdateInjectedComponentGroups();
        }

        protected override void OnBeforeDestroyManagerInternal()
        {
            if (this.m_PreviouslyEnabled)
            {
                this.m_PreviouslyEnabled = false;
                this.OnStopRunning();
            }
            this.CompleteDependencyInternal();
            this.UpdateInjectedComponentGroups();
        }

        protected virtual void OnStartRunning()
        {
        }

        protected virtual void OnStopRunning()
        {
        }

        public bool ShouldRunSystem()
        {
            bool flag2;
            if (!this.m_World.IsCreated)
            {
                flag2 = false;
            }
            else if (this.m_AlwaysUpdateSystem)
            {
                flag2 = true;
            }
            else
            {
                int length;
                if (this.m_ComponentGroups != null)
                {
                    length = this.m_ComponentGroups.Length;
                }
                else
                {
                    object componentGroups = this.m_ComponentGroups;
                    length = 0;
                }
                int num = length;
                if (num == 0)
                {
                    flag2 = true;
                }
                else
                {
                    int index = 0;
                    while (true)
                    {
                        if (index == num)
                        {
                            flag2 = false;
                        }
                        else
                        {
                            if (this.m_ComponentGroups[index].IsEmptyIgnoreFilter)
                            {
                                index++;
                                continue;
                            }
                            flag2 = true;
                        }
                        break;
                    }
                }
            }
            return flag2;
        }

        protected unsafe void UpdateInjectedComponentGroups()
        {
            if (this.m_InjectedComponentGroups != null)
            {
                ulong num;
                byte* pinnedSystemPtr = (byte*) ref UnsafeUtility.PinGCObjectAndGetAddress(this, out num);
                try
                {
                    InjectComponentGroupData[] injectedComponentGroups = this.m_InjectedComponentGroups;
                    int index = 0;
                    while (true)
                    {
                        if (index >= injectedComponentGroups.Length)
                        {
                            this.m_InjectFromEntityData.UpdateInjection(pinnedSystemPtr, this.EntityManager);
                            break;
                        }
                        injectedComponentGroups[index].UpdateInjection(pinnedSystemPtr);
                        index++;
                    }
                }
                catch
                {
                    UnsafeUtility.ReleaseGCObject(num);
                    throw;
                }
                UnsafeUtility.ReleaseGCObject(num);
            }
        }

        public bool Enabled { get; set; }

        public ComponentGroup[] ComponentGroups =>
            this.m_ComponentGroups;

        public uint GlobalSystemVersion =>
            this.m_EntityManager.GlobalSystemVersion;

        public uint LastSystemVersion =>
            this.m_LastSystemVersion;

        protected Unity.Entities.EntityManager EntityManager =>
            this.m_EntityManager;

        protected Unity.Entities.World World =>
            this.m_World;
    }
}

