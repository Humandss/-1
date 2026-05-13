using Unity.Collections;
using Unity.Mathematics;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 활성 총알 1발의 데이터. NativeArray&lt;BulletState&gt;의 원소.
    /// blittable 자료형만 포함. bool 대신 byte를 쓰는 이유는 Burst 호환성.
    /// </summary>
    public struct BulletState
    {
        // 위치/속력
        public float3 pos;
        public float3 prevPos;
        public float3 velocity;
        public float3 dir;

        public float speed;
        public float pen;
        public float refArea;
        public float k;          // 공기저항 계수 (0.5 * airDensity * dragCoeff * refArea / mass)
        public float armorDam;
        public float flightTime;

        public int bulletInfoIndex;   // BulletInfoTable.Infos 인덱스
        public int id;
        public int ricochetChance;

        public byte isPlayerShot;
        public byte isPenetratingTerrain;
        public byte isAlive;     // 1 = 활성, 0 = 빈 슬롯

        public FixedList64Bytes<int> hitTargets;   // 한 발이 같은 콜라이더 중복 데미지 안 주도록
        public Random rng;                         // 슬롯별 RNG (도탄 각도, 방탄 관통 확률)
    }
}
