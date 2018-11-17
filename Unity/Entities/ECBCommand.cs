namespace Unity.Entities
{
    using System;

    internal enum ECBCommand
    {
        InstantiateEntity,
        CreateEntity,
        DestroyEntity,
        AddComponent,
        RemoveComponent,
        SetComponent,
        AddBuffer,
        SetBuffer,
        AddSharedComponentData,
        SetSharedComponentData
    }
}

