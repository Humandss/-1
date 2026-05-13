using Unity.Mathematics;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// Burst Job → 메인 스레드 브릿지용. NativeQueue&lt;HitEvent&gt;에 큐잉되고 메인 스레드에서 드레인.
    /// 매니지드 자료(Collider, GameObject)는 InstanceID로만 들고 다닌다.
    /// </summary>
    public struct HitEvent
    {
        public int slotIndex;              // BulletState 슬롯 인덱스
        public float3 hitPoint;
        public float3 hitNormal;
        public int hitColliderInstanceID;  // ColliderRegistry로 조회
        public float segLen;
    }
}
