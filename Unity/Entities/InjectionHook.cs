namespace Unity.Entities
{
    using System;
    using System.Reflection;

    public abstract class InjectionHook
    {
        protected InjectionHook()
        {
        }

        public abstract InjectionContext.Entry CreateInjectionInfoFor(FieldInfo field, bool isReadOnly);
        internal abstract unsafe void InjectEntry(InjectionContext.Entry entry, ComponentGroup entityGroup, ref ComponentChunkIterator iterator, int length, byte* groupStructPtr);
        public abstract bool IsInterestedInField(FieldInfo fieldInfo);
        public virtual void PrepareEntry(ref InjectionContext.Entry entry, ComponentGroup entityGroup)
        {
        }

        public abstract string ValidateField(FieldInfo field, bool isReadOnly, InjectionContext injectionInfo);

        public abstract Type FieldTypeOfInterest { get; }
    }
}

