namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity;
    using UnityEngine.Experimental.LowLevel;
    using UnityEngine.Experimental.PlayerLoop;

    public static class ScriptBehaviourUpdateOrder
    {
        private static PlayerLoopSystem currentPlayerLoop;

        private static void AddDependencies(DependantBehavior targetSystem, IReadOnlyDictionary<Type, DependantBehavior> dependencies, IReadOnlyDictionary<Type, ScriptBehaviourGroup> allGroups, PlayerLoopSystem defaultPlayerLoop)
        {
            Type item = targetSystem.Manager.GetType();
            foreach (object obj2 in item.GetCustomAttributes(typeof(UpdateAfterAttribute), true))
            {
                DependantBehavior behavior;
                UpdateAfterAttribute attribute = obj2 as UpdateAfterAttribute;
                if (dependencies.TryGetValue(attribute.SystemType, out behavior))
                {
                    targetSystem.UpdateAfter.Add(attribute.SystemType);
                    behavior.UpdateBefore.Add(item);
                }
                else
                {
                    ScriptBehaviourGroup group;
                    if (allGroups.TryGetValue(attribute.SystemType, out group))
                    {
                        group.AddUpdateBeforeToAllChildBehaviours(targetSystem, dependencies);
                    }
                    else
                    {
                        UpdateInsertionPos(targetSystem, attribute.SystemType, defaultPlayerLoop, true);
                    }
                }
            }
            foreach (object obj3 in item.GetCustomAttributes(typeof(UpdateBeforeAttribute), true))
            {
                DependantBehavior behavior2;
                UpdateBeforeAttribute attribute2 = obj3 as UpdateBeforeAttribute;
                if (dependencies.TryGetValue(attribute2.SystemType, out behavior2))
                {
                    targetSystem.UpdateBefore.Add(attribute2.SystemType);
                    behavior2.UpdateAfter.Add(item);
                }
                else
                {
                    ScriptBehaviourGroup group2;
                    if (allGroups.TryGetValue(attribute2.SystemType, out group2))
                    {
                        group2.AddUpdateAfterToAllChildBehaviours(targetSystem, dependencies);
                    }
                    else
                    {
                        UpdateInsertionPos(targetSystem, attribute2.SystemType, defaultPlayerLoop, false);
                    }
                }
            }
            foreach (object obj4 in item.GetCustomAttributes(typeof(UpdateInGroupAttribute), true))
            {
                ScriptBehaviourGroup group3;
                UpdateInGroupAttribute attribute3 = obj4 as UpdateInGroupAttribute;
                if (allGroups.TryGetValue(attribute3.GroupType, out group3))
                {
                    DependantBehavior behavior3;
                    ScriptBehaviourGroup group4;
                    foreach (Type type2 in group3.UpdateAfter)
                    {
                        if (dependencies.TryGetValue(type2, out behavior3))
                        {
                            targetSystem.UpdateAfter.Add(type2);
                            behavior3.UpdateBefore.Add(item);
                            continue;
                        }
                        if (allGroups.TryGetValue(type2, out group4))
                        {
                            group4.AddUpdateBeforeToAllChildBehaviours(targetSystem, dependencies);
                            continue;
                        }
                        UpdateInsertionPos(targetSystem, type2, defaultPlayerLoop, true);
                    }
                    foreach (Type type3 in group3.UpdateBefore)
                    {
                        if (dependencies.TryGetValue(type3, out behavior3))
                        {
                            targetSystem.UpdateBefore.Add(type3);
                            behavior3.UpdateAfter.Add(item);
                            continue;
                        }
                        if (allGroups.TryGetValue(type3, out group4))
                        {
                            group4.AddUpdateAfterToAllChildBehaviours(targetSystem, dependencies);
                            continue;
                        }
                        UpdateInsertionPos(targetSystem, type3, defaultPlayerLoop, false);
                    }
                }
            }
        }

        private static Dictionary<Type, DependantBehavior> BuildSystemGraph(IEnumerable<ScriptBehaviourManager> activeManagers, PlayerLoopSystem defaultPlayerLoop)
        {
            Dictionary<Type, ScriptBehaviourGroup> dictionary;
            Dictionary<Type, DependantBehavior> dictionary2;
            CollectGroups(activeManagers, out dictionary, out dictionary2);
            foreach (KeyValuePair<Type, DependantBehavior> pair in dictionary2)
            {
                AddDependencies(pair.Value, dictionary2, dictionary, defaultPlayerLoop);
            }
            ValidateAndFixSystemGraph(dictionary2);
            return dictionary2;
        }

        private static void CollectGroups(IEnumerable<ScriptBehaviourManager> activeManagers, out Dictionary<Type, ScriptBehaviourGroup> allGroups, out Dictionary<Type, DependantBehavior> dependencies)
        {
            allGroups = new Dictionary<Type, ScriptBehaviourGroup>();
            dependencies = new Dictionary<Type, DependantBehavior>();
            foreach (ScriptBehaviourManager manager in activeManagers)
            {
                object[] objArray2 = manager.GetType().GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                int index = 0;
                while (true)
                {
                    ScriptBehaviourGroup group;
                    if (index >= objArray2.Length)
                    {
                        DependantBehavior behavior = new DependantBehavior(manager);
                        dependencies.Add(manager.GetType(), behavior);
                        break;
                    }
                    object obj2 = objArray2[index];
                    UpdateInGroupAttribute attribute = obj2 as UpdateInGroupAttribute;
                    if (!allGroups.TryGetValue(attribute.GroupType, out group))
                    {
                        group = new ScriptBehaviourGroup(attribute.GroupType, allGroups, null);
                    }
                    group.Managers.Add(manager.GetType());
                    index++;
                }
            }
        }

        private static PlayerLoopSystem CreatePlayerLoop(List<InsertionBucket> insertionBuckets, PlayerLoopSystem defaultPlayerLoop)
        {
            insertionBuckets.Sort();
            int num = 0;
            PlayerLoopSystem system = new PlayerLoopSystem {
                subSystemList = new PlayerLoopSystem[defaultPlayerLoop.subSystemList.Length]
            };
            int num2 = 0;
            for (int i = 0; i < defaultPlayerLoop.subSystemList.Length; i++)
            {
                int num4 = num + 1;
                int num5 = num4 + defaultPlayerLoop.subSystemList[i].subSystemList.Length;
                int num6 = 0;
                foreach (InsertionBucket bucket in insertionBuckets)
                {
                    if ((bucket.InsertPos >= num4) && (bucket.InsertPos <= num5))
                    {
                        num6 += bucket.Systems.Count;
                    }
                }
                system.subSystemList[i] = defaultPlayerLoop.subSystemList[i];
                if (num6 > 0)
                {
                    system.subSystemList[i].subSystemList = new PlayerLoopSystem[defaultPlayerLoop.subSystemList[i].subSystemList.Length + num6];
                    int index = 0;
                    int num8 = 0;
                    while (true)
                    {
                        if (num8 >= defaultPlayerLoop.subSystemList[i].subSystemList.Length)
                        {
                            while (true)
                            {
                                if ((num2 >= insertionBuckets.Count) || (insertionBuckets[num2].InsertPos > num5))
                                {
                                    break;
                                }
                                foreach (DependantBehavior behavior2 in insertionBuckets[num2].Systems)
                                {
                                    system.subSystemList[i].subSystemList[index].type = behavior2.Manager.GetType();
                                    DummyDelagateWrapper wrapper2 = new DummyDelagateWrapper(behavior2.Manager);
                                    system.subSystemList[i].subSystemList[index].updateDelegate = new PlayerLoopSystem.UpdateFunction(wrapper2.TriggerUpdate);
                                    index++;
                                }
                                num2++;
                            }
                            break;
                        }
                        while (true)
                        {
                            if ((num2 >= insertionBuckets.Count) || (insertionBuckets[num2].InsertPos > (num4 + num8)))
                            {
                                system.subSystemList[i].subSystemList[index] = defaultPlayerLoop.subSystemList[i].subSystemList[num8];
                                num8++;
                                index++;
                                break;
                            }
                            foreach (DependantBehavior behavior in insertionBuckets[num2].Systems)
                            {
                                system.subSystemList[i].subSystemList[index].type = behavior.Manager.GetType();
                                DummyDelagateWrapper wrapper = new DummyDelagateWrapper(behavior.Manager);
                                system.subSystemList[i].subSystemList[index].updateDelegate = new PlayerLoopSystem.UpdateFunction(wrapper.TriggerUpdate);
                                index++;
                            }
                            num2++;
                        }
                    }
                }
                num = num5;
            }
            return system;
        }

        private static List<InsertionBucket> CreateSystemDependencyList(IEnumerable<ScriptBehaviourManager> activeManagers, PlayerLoopSystem defaultPlayerLoop)
        {
            Dictionary<Type, DependantBehavior> dependencyGraph = BuildSystemGraph(activeManagers, defaultPlayerLoop);
            MarkSchedulingAndWaitingJobs(dependencyGraph);
            HashSet<DependantBehavior> set = new HashSet<DependantBehavior>();
            HashSet<DependantBehavior> set2 = new HashSet<DependantBehavior>();
            HashSet<DependantBehavior> set3 = new HashSet<DependantBehavior>();
            foreach (KeyValuePair<Type, DependantBehavior> pair in dependencyGraph)
            {
                DependantBehavior item = pair.Value;
                if (item.spawnsJobs)
                {
                    set.Add(item);
                    continue;
                }
                if (item.WaitsForJobs)
                {
                    set3.Add(item);
                    continue;
                }
                set2.Add(item);
            }
            List<DependantBehavior> list = new List<DependantBehavior>();
            while (true)
            {
                foreach (DependantBehavior behavior2 in set)
                {
                    foreach (Type type in behavior2.UpdateAfter)
                    {
                        DependantBehavior item = dependencyGraph[type];
                        if (set2.Remove(item) || set3.Remove(item))
                        {
                            list.Add(item);
                        }
                    }
                }
                if (list.Count == 0)
                {
                    while (true)
                    {
                        foreach (DependantBehavior behavior5 in set3)
                        {
                            foreach (Type type2 in behavior5.UpdateBefore)
                            {
                                DependantBehavior item = dependencyGraph[type2];
                                if (set2.Remove(item))
                                {
                                    list.Add(item);
                                }
                            }
                        }
                        if (list.Count == 0)
                        {
                            int num = 0;
                            PlayerLoopSystem[] subSystemList = defaultPlayerLoop.subSystemList;
                            int index = 0;
                            while (true)
                            {
                                if (index < subSystemList.Length)
                                {
                                    PlayerLoopSystem system = subSystemList[index];
                                    num += 1 + system.subSystemList.Length;
                                    if (!(system.type == typeof(Update)))
                                    {
                                        index++;
                                        continue;
                                    }
                                }
                                Dictionary<int, InsertionBucket> dictionary2 = new Dictionary<int, InsertionBucket>();
                                int num2 = 0;
                                while (true)
                                {
                                    if ((set.Count <= 0) && (set3.Count <= 0))
                                    {
                                        num2 = 0;
                                        while (set2.Count > 0)
                                        {
                                            foreach (DependantBehavior behavior11 in set2)
                                            {
                                                if (behavior11.LongestSystemsUpdatingBeforeChain == num2)
                                                {
                                                    if (behavior11.MinInsertPos == 0)
                                                    {
                                                        behavior11.MinInsertPos = num;
                                                    }
                                                    behavior11.MaxInsertPos = behavior11.MinInsertPos;
                                                    list.Add(behavior11);
                                                    foreach (Type type5 in behavior11.UpdateBefore)
                                                    {
                                                        if (dependencyGraph[type5].MinInsertPos < behavior11.MinInsertPos)
                                                        {
                                                            dependencyGraph[type5].MinInsertPos = behavior11.MinInsertPos;
                                                        }
                                                    }
                                                }
                                            }
                                            foreach (DependantBehavior behavior12 in list)
                                            {
                                                InsertionBucket bucket2;
                                                set2.Remove(behavior12);
                                                int key = (behavior12.MinInsertPos << 2) | 1;
                                                if (!dictionary2.TryGetValue(key, out bucket2))
                                                {
                                                    bucket2 = new InsertionBucket {
                                                        InsertPos = behavior12.MinInsertPos,
                                                        InsertSubPos = 1
                                                    };
                                                    dictionary2.Add(key, bucket2);
                                                }
                                                bucket2.Systems.Add(behavior12);
                                            }
                                            list.Clear();
                                            num2++;
                                        }
                                        return new List<InsertionBucket>(dictionary2.Values);
                                    }
                                    foreach (DependantBehavior behavior8 in set)
                                    {
                                        if (behavior8.LongestSystemsUpdatingBeforeChain == num2)
                                        {
                                            if (behavior8.MinInsertPos == 0)
                                            {
                                                behavior8.MinInsertPos = num;
                                            }
                                            behavior8.MaxInsertPos = behavior8.MinInsertPos;
                                            list.Add(behavior8);
                                            foreach (Type type3 in behavior8.UpdateBefore)
                                            {
                                                if (dependencyGraph[type3].MinInsertPos < behavior8.MinInsertPos)
                                                {
                                                    dependencyGraph[type3].MinInsertPos = behavior8.MinInsertPos;
                                                }
                                            }
                                        }
                                    }
                                    foreach (DependantBehavior behavior9 in set3)
                                    {
                                        if (behavior9.LongestSystemsUpdatingAfterChain == num2)
                                        {
                                            if (behavior9.MaxInsertPos == 0)
                                            {
                                                behavior9.MaxInsertPos = num;
                                            }
                                            behavior9.MinInsertPos = behavior9.MaxInsertPos;
                                            list.Add(behavior9);
                                            foreach (Type type4 in behavior9.UpdateAfter)
                                            {
                                                if ((dependencyGraph[type4].MaxInsertPos == 0) || (dependencyGraph[type4].MaxInsertPos > behavior9.MaxInsertPos))
                                                {
                                                    dependencyGraph[type4].MaxInsertPos = behavior9.MaxInsertPos;
                                                }
                                            }
                                        }
                                    }
                                    foreach (DependantBehavior behavior10 in list)
                                    {
                                        InsertionBucket bucket;
                                        set.Remove(behavior10);
                                        bool flag16 = set3.Remove(behavior10);
                                        int num4 = flag16 ? 2 : 0;
                                        int key = (behavior10.MinInsertPos << 2) | num4;
                                        if (!dictionary2.TryGetValue(key, out bucket))
                                        {
                                            InsertionBucket bucket1 = new InsertionBucket();
                                            bucket1.InsertPos = behavior10.MinInsertPos;
                                            bucket1.InsertSubPos = num4;
                                            bucket = bucket1;
                                            dictionary2.Add(key, bucket);
                                        }
                                        bucket.Systems.Add(behavior10);
                                    }
                                    list.Clear();
                                    num2++;
                                }
                            }
                        }
                        foreach (DependantBehavior behavior7 in list)
                        {
                            set3.Add(behavior7);
                        }
                        list.Clear();
                    }
                }
                foreach (DependantBehavior behavior4 in list)
                {
                    set.Add(behavior4);
                }
                list.Clear();
            }
        }

        internal static PlayerLoopSystem InsertManagersInPlayerLoop(IEnumerable<ScriptBehaviourManager> activeManagers, PlayerLoopSystem defaultPlayerLoop)
        {
            PlayerLoopSystem system;
            if (activeManagers.Count<ScriptBehaviourManager>() == 0)
            {
                system = defaultPlayerLoop;
            }
            else
            {
                system = CreatePlayerLoop(CreateSystemDependencyList(activeManagers, defaultPlayerLoop), defaultPlayerLoop);
            }
            return system;
        }

        private static PlayerLoopSystem InsertWorldManagersInPlayerLoop(PlayerLoopSystem defaultPlayerLoop, params World[] worlds)
        {
            List<InsertionBucket> insertionBuckets = new List<InsertionBucket>();
            foreach (World world in worlds)
            {
                if (world.BehaviourManagers.Count<ScriptBehaviourManager>() != 0)
                {
                    insertionBuckets.AddRange(CreateSystemDependencyList(world.BehaviourManagers, defaultPlayerLoop));
                }
            }
            return CreatePlayerLoop(insertionBuckets, defaultPlayerLoop);
        }

        private static void MarkSchedulingAndWaitingJobs(Dictionary<Type, DependantBehavior> dependencyGraph)
        {
            HashSet<DependantBehavior> set = new HashSet<DependantBehavior>();
            foreach (KeyValuePair<Type, DependantBehavior> pair in dependencyGraph)
            {
                DependantBehavior item = pair.Value;
                if (item.Manager is JobComponentSystem)
                {
                    item.spawnsJobs = true;
                    set.Add(item);
                }
            }
            foreach (KeyValuePair<Type, DependantBehavior> pair2 in dependencyGraph)
            {
                ComponentGroup[] componentGroups;
                DependantBehavior behavior2 = pair2.Value;
                ComponentSystem manager = behavior2.Manager as ComponentSystem;
                if (manager != null)
                {
                    componentGroups = manager.ComponentGroups;
                }
                else
                {
                    object obj1 = manager;
                    componentGroups = null;
                }
                if (componentGroups != null)
                {
                    HashSet<int> set2 = new HashSet<int>();
                    ComponentGroup[] componentGroups = ((ComponentSystem) behavior2.Manager).ComponentGroups;
                    int index = 0;
                    while (true)
                    {
                        if (index >= componentGroups.Length)
                        {
                            using (HashSet<DependantBehavior>.Enumerator enumerator3 = set.GetEnumerator())
                            {
                                while (true)
                                {
                                    if (enumerator3.MoveNext())
                                    {
                                        DependantBehavior current = enumerator3.Current;
                                        if (!(current.Manager is ComponentSystem))
                                        {
                                            continue;
                                        }
                                        HashSet<int> set3 = new HashSet<int>();
                                        ComponentGroup[] groupArray2 = ((ComponentSystem) current.Manager).ComponentGroups;
                                        int num3 = 0;
                                        while (true)
                                        {
                                            if (num3 < groupArray2.Length)
                                            {
                                                ComponentGroup group2 = groupArray2[num3];
                                                ComponentType[] typeArray2 = group2.Types;
                                                int num4 = 0;
                                                while (true)
                                                {
                                                    if (num4 >= typeArray2.Length)
                                                    {
                                                        num3++;
                                                        break;
                                                    }
                                                    ComponentType type2 = typeArray2[num4];
                                                    if (type2.RequiresJobDependency)
                                                    {
                                                        set3.Add(type2.TypeIndex);
                                                    }
                                                    num4++;
                                                }
                                                continue;
                                            }
                                            bool flag4 = false;
                                            foreach (int num5 in set2)
                                            {
                                                if (set3.Contains(num5))
                                                {
                                                    flag4 = true;
                                                    break;
                                                }
                                            }
                                            if (flag4)
                                            {
                                                behavior2.WaitsForJobs = true;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                        ComponentGroup group = componentGroups[index];
                        ComponentType[] types = group.Types;
                        int num2 = 0;
                        while (true)
                        {
                            if (num2 >= types.Length)
                            {
                                index++;
                                break;
                            }
                            ComponentType type = types[num2];
                            if (type.RequiresJobDependency)
                            {
                                set2.Add(type.TypeIndex);
                            }
                            num2++;
                        }
                    }
                }
            }
        }

        private static void SetPlayerLoop(PlayerLoopSystem playerLoop)
        {
            PlayerLoop.SetPlayerLoop(playerLoop);
            currentPlayerLoop = playerLoop;
        }

        private static void UpdateInsertionPos(DependantBehavior target, Type dep, PlayerLoopSystem defaultPlayerLoop, bool after)
        {
            int num = 0;
            PlayerLoopSystem[] subSystemList = defaultPlayerLoop.subSystemList;
            int index = 0;
            while (true)
            {
                while (true)
                {
                    if (index < subSystemList.Length)
                    {
                        PlayerLoopSystem system = subSystemList[index];
                        num++;
                        if (!(system.type == dep))
                        {
                            int num3 = num;
                            int num4 = num + system.subSystemList.Length;
                            PlayerLoopSystem[] systemArray2 = system.subSystemList;
                            int num5 = 0;
                            while (true)
                            {
                                if (num5 < systemArray2.Length)
                                {
                                    PlayerLoopSystem system2 = systemArray2[num5];
                                    if (!(system2.type == dep))
                                    {
                                        num++;
                                        num5++;
                                        continue;
                                    }
                                    if (!after)
                                    {
                                        if (target.MinInsertPos < num3)
                                        {
                                            target.MinInsertPos = num3;
                                        }
                                        if ((target.MaxInsertPos == 0) || (target.MaxInsertPos > num))
                                        {
                                            target.MaxInsertPos = num;
                                        }
                                    }
                                    else
                                    {
                                        num++;
                                        if (target.MinInsertPos < num)
                                        {
                                            target.MinInsertPos = num;
                                        }
                                        if ((target.MaxInsertPos == 0) || (target.MaxInsertPos > num4))
                                        {
                                            target.MaxInsertPos = num4;
                                        }
                                    }
                                }
                                else
                                {
                                    index++;
                                    continue;
                                }
                                break;
                            }
                        }
                        else if (!after)
                        {
                            if (target.MinInsertPos < num)
                            {
                                target.MinInsertPos = num;
                            }
                            if ((target.MaxInsertPos == 0) || (target.MaxInsertPos > num))
                            {
                                target.MaxInsertPos = num;
                            }
                        }
                        else
                        {
                            num += system.subSystemList.Length;
                            if (target.MinInsertPos < num)
                            {
                                target.MinInsertPos = num;
                            }
                            if ((target.MaxInsertPos == 0) || (target.MaxInsertPos > num))
                            {
                                target.MaxInsertPos = num;
                            }
                        }
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        public static void UpdatePlayerLoop(params World[] worlds)
        {
            PlayerLoopSystem defaultPlayerLoop = PlayerLoop.GetDefaultPlayerLoop();
            if ((worlds != null) ? (worlds.Length != 0) : false)
            {
                SetPlayerLoop(InsertWorldManagersInPlayerLoop(defaultPlayerLoop, (from x in worlds
                    where x != null
                    select x).ToArray<World>()));
            }
            else
            {
                SetPlayerLoop(defaultPlayerLoop);
            }
        }

        private static void ValidateAndFixSingleChainMaxPos(DependantBehavior system, Dictionary<Type, DependantBehavior> dependencyGraph, int maxInsertPos)
        {
            foreach (Type type in system.UpdateAfter)
            {
                DependantBehavior behavior = dependencyGraph[type];
                if (system.LongestSystemsUpdatingAfterChain >= behavior.LongestSystemsUpdatingAfterChain)
                {
                    behavior.LongestSystemsUpdatingAfterChain = system.LongestSystemsUpdatingAfterChain + 1;
                }
                if ((behavior.MaxInsertPos == 0) || (behavior.MaxInsertPos > maxInsertPos))
                {
                    behavior.MaxInsertPos = maxInsertPos;
                }
                if ((behavior.MaxInsertPos > 0) && (behavior.MaxInsertPos < behavior.MinInsertPos))
                {
                    Debug.LogError($"{type} is over constrained with engine and system containts - ignoring dependencies");
                    behavior.MinInsertPos = behavior.MaxInsertPos;
                }
                ValidateAndFixSingleChainMaxPos(behavior, dependencyGraph, behavior.MaxInsertPos);
            }
        }

        private static void ValidateAndFixSingleChainMinPos(DependantBehavior system, IReadOnlyDictionary<Type, DependantBehavior> dependencyGraph, int minInsertPos)
        {
            foreach (Type type in system.UpdateBefore)
            {
                DependantBehavior behavior = dependencyGraph[type];
                if (system.LongestSystemsUpdatingBeforeChain >= behavior.LongestSystemsUpdatingBeforeChain)
                {
                    behavior.LongestSystemsUpdatingBeforeChain = system.LongestSystemsUpdatingBeforeChain + 1;
                }
                if (behavior.MinInsertPos < minInsertPos)
                {
                    behavior.MinInsertPos = minInsertPos;
                }
                if ((behavior.MaxInsertPos > 0) && (behavior.MaxInsertPos < behavior.MinInsertPos))
                {
                    Debug.LogError($"{type} is over constrained with engine and system containts - ignoring dependencies");
                    behavior.MaxInsertPos = behavior.MinInsertPos;
                }
                ValidateAndFixSingleChainMinPos(behavior, dependencyGraph, behavior.MinInsertPos);
            }
        }

        private static void ValidateAndFixSystemGraph(Dictionary<Type, DependantBehavior> dependencyGraph)
        {
            foreach (KeyValuePair<Type, DependantBehavior> pair in dependencyGraph)
            {
                DependantBehavior behavior = pair.Value;
                if (behavior.MinInsertPos > behavior.MaxInsertPos)
                {
                    Debug.LogError($"{behavior.Manager.GetType()} is over constrained with engine containts - ignoring dependencies");
                    behavior.MinInsertPos = behavior.MaxInsertPos = 0;
                }
                behavior.UnvalidatedSystemsUpdatingBefore = behavior.UpdateAfter.Count;
                behavior.LongestSystemsUpdatingBeforeChain = 0;
                behavior.LongestSystemsUpdatingAfterChain = 0;
            }
            bool flag = true;
            while (true)
            {
                if (!flag)
                {
                    foreach (KeyValuePair<Type, DependantBehavior> pair3 in dependencyGraph)
                    {
                        DependantBehavior behavior3 = pair3.Value;
                        if (behavior3.UnvalidatedSystemsUpdatingBefore > 0)
                        {
                            Debug.LogError($"{behavior3.Manager.GetType()} is in a chain of circular dependencies - ignoring dependencies");
                            foreach (Type type2 in behavior3.UpdateAfter)
                            {
                                dependencyGraph[type2].UpdateBefore.Remove(behavior3.Manager.GetType());
                            }
                            behavior3.UpdateAfter.Clear();
                        }
                    }
                    foreach (KeyValuePair<Type, DependantBehavior> pair4 in dependencyGraph)
                    {
                        DependantBehavior system = pair4.Value;
                        if (system.UpdateBefore.Count == 0)
                        {
                            ValidateAndFixSingleChainMaxPos(system, dependencyGraph, system.MaxInsertPos);
                        }
                        if (system.UpdateAfter.Count == 0)
                        {
                            ValidateAndFixSingleChainMinPos(system, dependencyGraph, system.MinInsertPos);
                        }
                    }
                    return;
                }
                flag = false;
                foreach (KeyValuePair<Type, DependantBehavior> pair2 in dependencyGraph)
                {
                    DependantBehavior behavior2 = pair2.Value;
                    if (behavior2.UnvalidatedSystemsUpdatingBefore == 0)
                    {
                        behavior2.UnvalidatedSystemsUpdatingBefore = -1;
                        foreach (Type type in behavior2.UpdateBefore)
                        {
                            DependantBehavior local1 = dependencyGraph[type];
                            local1.UnvalidatedSystemsUpdatingBefore--;
                            flag = true;
                        }
                    }
                }
            }
        }

        public static PlayerLoopSystem CurrentPlayerLoop =>
            currentPlayerLoop;

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly ScriptBehaviourUpdateOrder.<>c <>9 = new ScriptBehaviourUpdateOrder.<>c();
            public static Func<World, bool> <>9__12_0;

            internal bool <UpdatePlayerLoop>b__12_0(World x) => 
                (x != null);
        }

        private class DependantBehavior
        {
            public readonly ScriptBehaviourManager Manager;
            public readonly HashSet<Type> UpdateAfter = new HashSet<Type>();
            public readonly HashSet<Type> UpdateBefore = new HashSet<Type>();
            public int LongestSystemsUpdatingAfterChain;
            public int LongestSystemsUpdatingBeforeChain;
            public int MaxInsertPos;
            public int MinInsertPos;
            public bool spawnsJobs;
            public int UnvalidatedSystemsUpdatingBefore;
            public bool WaitsForJobs;

            public DependantBehavior(ScriptBehaviourManager man)
            {
                this.Manager = man;
                this.MinInsertPos = 0;
                this.MaxInsertPos = 0;
                this.spawnsJobs = false;
                this.WaitsForJobs = false;
                this.UnvalidatedSystemsUpdatingBefore = 0;
                this.LongestSystemsUpdatingBeforeChain = 0;
                this.LongestSystemsUpdatingAfterChain = 0;
            }
        }

        internal class DummyDelagateWrapper
        {
            private readonly ScriptBehaviourManager m_Manager;

            public DummyDelagateWrapper(ScriptBehaviourManager man)
            {
                this.m_Manager = man;
            }

            public void TriggerUpdate()
            {
                this.m_Manager.Update();
            }

            internal ScriptBehaviourManager Manager =>
                this.m_Manager;
        }

        private class InsertionBucket : IComparable
        {
            public readonly List<ScriptBehaviourUpdateOrder.DependantBehavior> Systems = new List<ScriptBehaviourUpdateOrder.DependantBehavior>();
            public int InsertPos = 0;
            public int InsertSubPos = 0;

            public int CompareTo(object other)
            {
                int num;
                ScriptBehaviourUpdateOrder.InsertionBucket bucket = other as ScriptBehaviourUpdateOrder.InsertionBucket;
                if (this.InsertPos == bucket.InsertPos)
                {
                    num = this.InsertSubPos - bucket.InsertSubPos;
                }
                else
                {
                    num = this.InsertPos - bucket.InsertPos;
                }
                return num;
            }
        }

        private class ScriptBehaviourGroup
        {
            private readonly List<ScriptBehaviourUpdateOrder.ScriptBehaviourGroup> m_Groups = new List<ScriptBehaviourUpdateOrder.ScriptBehaviourGroup>();
            public readonly List<Type> Managers = new List<Type>();
            public readonly HashSet<Type> UpdateAfter = new HashSet<Type>();
            public readonly HashSet<Type> UpdateBefore = new HashSet<Type>();
            private readonly Type m_GroupType;
            private readonly List<ScriptBehaviourUpdateOrder.ScriptBehaviourGroup> m_Parents = new List<ScriptBehaviourUpdateOrder.ScriptBehaviourGroup>();

            public ScriptBehaviourGroup(Type grpType, IDictionary<Type, ScriptBehaviourUpdateOrder.ScriptBehaviourGroup> allGroups, HashSet<Type> circularCheck = null)
            {
                this.m_GroupType = grpType;
                foreach (object obj2 in grpType.GetCustomAttributes(typeof(UpdateAfterAttribute), true))
                {
                    UpdateAfterAttribute attribute = obj2 as UpdateAfterAttribute;
                    this.UpdateAfter.Add(attribute.SystemType);
                }
                foreach (object obj3 in grpType.GetCustomAttributes(typeof(UpdateBeforeAttribute), true))
                {
                    UpdateBeforeAttribute attribute2 = obj3 as UpdateBeforeAttribute;
                    this.UpdateBefore.Add(attribute2.SystemType);
                }
                allGroups.Add(this.m_GroupType, this);
                foreach (object obj4 in this.m_GroupType.GetCustomAttributes(typeof(UpdateInGroupAttribute), true))
                {
                    ScriptBehaviourUpdateOrder.ScriptBehaviourGroup group;
                    if (circularCheck == null)
                    {
                        HashSet<Type> set1 = new HashSet<Type>();
                        set1.Add(this.m_GroupType);
                        circularCheck = set1;
                    }
                    UpdateInGroupAttribute attribute3 = obj4 as UpdateInGroupAttribute;
                    if (!circularCheck.Add(attribute3.GroupType))
                    {
                        string message = "Found circular chain in update groups involving: ";
                        bool flag3 = true;
                        foreach (Type type in circularCheck)
                        {
                            message = message + (flag3 ? "" : ", ") + type;
                            flag3 = false;
                        }
                        Debug.LogError(message);
                    }
                    if (!allGroups.TryGetValue(attribute3.GroupType, out group))
                    {
                        group = new ScriptBehaviourUpdateOrder.ScriptBehaviourGroup(attribute3.GroupType, allGroups, circularCheck);
                    }
                    circularCheck.Remove(attribute3.GroupType);
                    group.m_Groups.Add(this);
                    this.m_Parents.Add(group);
                    foreach (Type type2 in group.UpdateBefore)
                    {
                        this.UpdateBefore.Add(type2);
                    }
                    foreach (Type type3 in group.UpdateAfter)
                    {
                        this.UpdateAfter.Add(type3);
                    }
                }
            }

            public void AddUpdateAfterToAllChildBehaviours(ScriptBehaviourUpdateOrder.DependantBehavior target, IReadOnlyDictionary<Type, ScriptBehaviourUpdateOrder.DependantBehavior> dependencies)
            {
                Type item = target.Manager.GetType();
                foreach (Type type2 in this.Managers)
                {
                    ScriptBehaviourUpdateOrder.DependantBehavior behavior;
                    if (dependencies.TryGetValue(type2, out behavior))
                    {
                        target.UpdateBefore.Add(type2);
                        behavior.UpdateAfter.Add(item);
                    }
                }
                foreach (ScriptBehaviourUpdateOrder.ScriptBehaviourGroup group in this.m_Groups)
                {
                    group.AddUpdateAfterToAllChildBehaviours(target, dependencies);
                }
            }

            public void AddUpdateBeforeToAllChildBehaviours(ScriptBehaviourUpdateOrder.DependantBehavior target, IReadOnlyDictionary<Type, ScriptBehaviourUpdateOrder.DependantBehavior> dependencies)
            {
                Type item = target.Manager.GetType();
                foreach (Type type2 in this.Managers)
                {
                    ScriptBehaviourUpdateOrder.DependantBehavior behavior;
                    if (dependencies.TryGetValue(type2, out behavior))
                    {
                        target.UpdateAfter.Add(type2);
                        behavior.UpdateBefore.Add(item);
                    }
                }
                foreach (ScriptBehaviourUpdateOrder.ScriptBehaviourGroup group in this.m_Groups)
                {
                    group.AddUpdateBeforeToAllChildBehaviours(target, dependencies);
                }
            }
        }
    }
}

