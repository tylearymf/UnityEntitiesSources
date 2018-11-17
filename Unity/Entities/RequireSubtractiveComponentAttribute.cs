namespace Unity.Entities
{
    using System;

    [AttributeUsage(AttributeTargets.Struct)]
    public class RequireSubtractiveComponentAttribute : Attribute
    {
        public Type[] SubtractiveComponents;

        public RequireSubtractiveComponentAttribute(params Type[] subtractiveComponents)
        {
            this.SubtractiveComponents = subtractiveComponents;
        }
    }
}

