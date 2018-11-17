namespace Unity.Entities
{
    using UnityEngine.Experimental.PlayerLoop;
    using UnityEngine.Scripting;

    [UpdateBefore(typeof(Initialization)), Preserve]
    public class EndFrameBarrier : BarrierSystem
    {
    }
}

