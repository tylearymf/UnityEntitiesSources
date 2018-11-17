namespace Unity.Entities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed class InjectionContext
    {
        private readonly List<Entry> m_Entries = new List<Entry>();
        [CompilerGenerated, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool <HasComponentRequirements>k__BackingField;

        internal void AddEntry(Entry entry)
        {
            this.HasComponentRequirements = this.HasComponentRequirements || (entry.ComponentRequirements.Length != 0);
            this.m_Entries.Add(entry);
        }

        public void PrepareEntries(ComponentGroup entityGroup)
        {
            if (this.HasEntries)
            {
                int index = 0;
                while (true)
                {
                    if (index >= this.m_Entries.Count)
                    {
                        break;
                    }
                    Entry entry = this.m_Entries[index];
                    entry.Hook.PrepareEntry(ref entry, entityGroup);
                    this.m_Entries.set_Item(index, entry);
                    index++;
                }
            }
        }

        internal unsafe void UpdateEntries(ComponentGroup entityGroup, ref ComponentChunkIterator iterator, int length, byte* groupStructPtr)
        {
            if (this.HasEntries)
            {
                foreach (Entry entry in this.m_Entries)
                {
                    entry.Hook.InjectEntry(entry, entityGroup, ref iterator, length, groupStructPtr);
                }
            }
        }

        public bool HasComponentRequirements { get; private set; }

        public bool HasEntries =>
            (this.m_Entries.Count != 0);

        public IReadOnlyCollection<Entry> Entries =>
            this.m_Entries;

        public IEnumerable<ComponentType> ComponentRequirements =>
            new <get_ComponentRequirements>d__10(-2) { <>4__this=this };


        [StructLayout(LayoutKind.Sequential)]
        public struct Entry
        {
            public int FieldOffset;
            public System.Reflection.FieldInfo FieldInfo;
            public Type[] ComponentRequirements;
            public InjectionHook Hook;
            public Unity.Entities.ComponentType.AccessMode AccessMode;
            public int IndexInComponentGroup;
            public bool IsReadOnly;
            public Unity.Entities.ComponentType ComponentType;
        }
    }
}

