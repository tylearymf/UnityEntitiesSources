namespace Unity.Entities
{
    using System;

    [AttributeUsage(AttributeTargets.Struct)]
    public class RequireComponentTagAttribute : Attribute
    {
        public Type[] TagComponents;

        public RequireComponentTagAttribute(params Type[] tagComponents)
        {
            this.TagComponents = tagComponents;
        }
    }
}

