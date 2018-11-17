namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;

    internal class WorldDebuggingTools
    {
        private static bool Match(ComponentGroup group, NativeArray<ComponentType> entityComponentTypes)
        {
            using (IEnumerator<ComponentType> enumerator = group.Types.Skip<ComponentType>(1).GetEnumerator())
            {
                while (true)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    ComponentType current = enumerator.Current;
                    bool flag = false;
                    foreach (ComponentType type2 in entityComponentTypes)
                    {
                        if (type2.TypeIndex == current.TypeIndex)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag == (current.AccessModeType == ComponentType.AccessMode.Subtractive))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static void MatchEntityInComponentGroups(World world, Entity entity, List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>> matchList)
        {
            using (NativeArray<ComponentType> array = world.GetExistingManager<EntityManager>().GetComponentTypes(entity, Allocator.Temp))
            {
                foreach (ScriptBehaviourManager manager in World.Active.BehaviourManagers)
                {
                    List<ComponentGroup> list = new List<ComponentGroup>();
                    ComponentSystemBase base2 = manager as ComponentSystemBase;
                    if (base2 != null)
                    {
                        ComponentGroup[] componentGroups = base2.ComponentGroups;
                        int index = 0;
                        while (true)
                        {
                            if (index >= componentGroups.Length)
                            {
                                if (list.Count > 0)
                                {
                                    matchList.Add(new Tuple<ScriptBehaviourManager, List<ComponentGroup>>(manager, list));
                                }
                                break;
                            }
                            ComponentGroup group = componentGroups[index];
                            if (Match(group, array))
                            {
                                list.Add(group);
                            }
                            index++;
                        }
                    }
                }
            }
        }
    }
}

