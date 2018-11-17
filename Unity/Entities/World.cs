namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Unity;

    public class World : IDisposable
    {
        private static readonly List<World> allWorlds = new List<World>();
        private bool m_AllowGetManager = true;
        private Dictionary<Type, ScriptBehaviourManager> m_BehaviourManagerLookup = new Dictionary<Type, ScriptBehaviourManager>();
        private List<ScriptBehaviourManager> m_BehaviourManagers = new List<ScriptBehaviourManager>();
        private int m_SystemIDAllocator = 0;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string <Name>k__BackingField;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int <Version>k__BackingField;
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static World <Active>k__BackingField;

        public World(string name)
        {
            this.<Name>k__BackingField = name;
            allWorlds.Add(this);
        }

        private void AddTypeLookup(Type type, ScriptBehaviourManager manager)
        {
            while (type != typeof(ScriptBehaviourManager))
            {
                if (!this.m_BehaviourManagerLookup.ContainsKey(type))
                {
                    this.m_BehaviourManagerLookup.Add(type, manager);
                }
                type = type.BaseType;
            }
        }

        internal int AllocateSystemID()
        {
            int num = this.m_SystemIDAllocator + 1;
            this.m_SystemIDAllocator = num;
            return num;
        }

        public T CreateManager<T>(params object[] constructorArgumnents) where T: ScriptBehaviourManager => 
            ((T) this.CreateManagerInternal(typeof(T), constructorArgumnents));

        public ScriptBehaviourManager CreateManager(Type type, params object[] constructorArgumnents) => 
            this.CreateManagerInternal(type, constructorArgumnents);

        private ScriptBehaviourManager CreateManagerInternal(Type type, object[] constructorArguments)
        {
            ScriptBehaviourManager manager;
            if (!this.m_AllowGetManager)
            {
                throw new ArgumentException("During destruction of a system you are not allowed to create more systems.");
            }
            if ((constructorArguments != null) && (constructorArguments.Length != 0))
            {
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if ((constructors.Length == 1) && constructors[0].IsPrivate)
                {
                    throw new MissingMethodException($"Constructing {type} failed because the constructor was private, it must be public.");
                }
            }
            this.m_AllowGetManager = true;
            try
            {
                manager = Activator.CreateInstance(type, constructorArguments) as ScriptBehaviourManager;
            }
            catch
            {
                this.m_AllowGetManager = false;
                throw;
            }
            this.m_BehaviourManagers.Add(manager);
            this.AddTypeLookup(type, manager);
            try
            {
                manager.CreateInstance(this);
            }
            catch
            {
                this.RemoveManagerInteral(manager);
                throw;
            }
            int num = this.Version + 1;
            this.Version = num;
            return manager;
        }

        public void DestroyManager(ScriptBehaviourManager manager)
        {
            this.RemoveManagerInteral(manager);
            manager.DestroyInstance();
        }

        public void Dispose()
        {
            if (!this.IsCreated)
            {
                throw new ArgumentException("World is already disposed");
            }
            if (allWorlds.Contains(this))
            {
                allWorlds.Remove(this);
            }
            this.m_BehaviourManagers.Reverse();
            foreach (ScriptBehaviourManager manager in this.m_BehaviourManagers)
            {
                if (manager is EntityManager)
                {
                    this.m_BehaviourManagers.Remove(manager);
                    this.m_BehaviourManagers.Add(manager);
                    break;
                }
            }
            this.m_AllowGetManager = false;
            foreach (ScriptBehaviourManager manager2 in this.m_BehaviourManagers)
            {
                try
                {
                    manager2.DestroyInstance();
                }
                catch (Exception exception1)
                {
                    Unity.Debug.LogException(exception1);
                }
            }
            if (Active == this)
            {
                Active = null;
            }
            this.m_BehaviourManagers.Clear();
            this.m_BehaviourManagerLookup.Clear();
            this.m_BehaviourManagers = null;
            this.m_BehaviourManagerLookup = null;
        }

        public static void DisposeAllWorlds()
        {
            while (allWorlds.Count != 0)
            {
                allWorlds[0].Dispose();
            }
        }

        public T GetExistingManager<T>() where T: ScriptBehaviourManager => 
            ((T) this.GetExistingManagerInternal(typeof(T)));

        public ScriptBehaviourManager GetExistingManager(Type type) => 
            this.GetExistingManagerInternal(type);

        private ScriptBehaviourManager GetExistingManagerInternal(Type type)
        {
            ScriptBehaviourManager manager;
            ScriptBehaviourManager manager2;
            if (!this.IsCreated)
            {
                throw new ArgumentException("During destruction ");
            }
            if (!this.m_AllowGetManager)
            {
                throw new ArgumentException("During destruction of a system you are not allowed to get or create more systems.");
            }
            if (this.m_BehaviourManagerLookup.TryGetValue(type, out manager))
            {
                manager2 = manager;
            }
            else
            {
                manager2 = null;
            }
            return manager2;
        }

        public T GetOrCreateManager<T>() where T: ScriptBehaviourManager => 
            ((T) this.GetOrCreateManagerInternal(typeof(T)));

        public ScriptBehaviourManager GetOrCreateManager(Type type) => 
            this.GetOrCreateManagerInternal(type);

        private ScriptBehaviourManager GetOrCreateManagerInternal(Type type) => 
            (this.GetExistingManagerInternal(type) ?? this.CreateManagerInternal(type, null));

        private void RemoveManagerInteral(ScriptBehaviourManager manager)
        {
            if (!this.m_BehaviourManagers.Remove(manager))
            {
                throw new ArgumentException("manager does not exist in the world");
            }
            int num = this.Version + 1;
            this.Version = num;
            for (Type type = manager.GetType(); type != typeof(ScriptBehaviourManager); type = type.BaseType)
            {
                if (this.m_BehaviourManagerLookup[type] == manager)
                {
                    this.m_BehaviourManagerLookup.Remove(type);
                    foreach (ScriptBehaviourManager manager2 in this.m_BehaviourManagers)
                    {
                        if (manager2.GetType().IsSubclassOf(type))
                        {
                            this.AddTypeLookup(manager2.GetType(), manager2);
                        }
                    }
                }
            }
        }

        public override string ToString() => 
            this.Name;

        public IEnumerable<ScriptBehaviourManager> BehaviourManagers =>
            new ReadOnlyCollection<ScriptBehaviourManager>(this.m_BehaviourManagers);

        public string Name =>
            this.<Name>k__BackingField;

        public int Version { get; private set; }

        public static World Active
        {
            [CompilerGenerated]
            get => 
                <Active>k__BackingField;
            [CompilerGenerated]
            set => 
                (<Active>k__BackingField = value);
        }

        public static ReadOnlyCollection<World> AllWorlds =>
            new ReadOnlyCollection<World>(allWorlds);

        public bool IsCreated =>
            (this.m_BehaviourManagers != null);
    }
}

