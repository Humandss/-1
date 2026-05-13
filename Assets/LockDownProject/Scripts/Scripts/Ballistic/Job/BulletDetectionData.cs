using Unity.Mathematics;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 적 AI가 근접 총알을 감지할 때 사용하는 경량 뷰.
    /// BulletSimulationSystem이 매 FixedUpdate 끝에 활성 플레이어 총알만 추려서 채워준다.
    /// </summary>
    public struct BulletDetectionData
    {
        public float3 pos;
        public float3 travelDir;
    }
}
