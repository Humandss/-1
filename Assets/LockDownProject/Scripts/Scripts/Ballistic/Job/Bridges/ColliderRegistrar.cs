using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// 부착된 GameObject의 모든 Collider를 ColliderRegistry에 자가 등록한다.
    /// 동일 GameObject에 여러 콜라이더가 있을 경우 모두 등록.
    /// BulletSimulationSystem이 씬 로드 시 hitLayerMask에 해당하는 콜라이더에 동적 부착.
    /// </summary>
    public class ColliderRegistrar : MonoBehaviour
    {
        private Collider[] cols;

        private void Awake()
        {
            cols = GetComponents<Collider>();
        }

        private void OnEnable()
        {
            if (cols == null) cols = GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
                ColliderRegistry.Register(cols[i]);
        }

        private void OnDisable()
        {
            if (cols == null) return;
            for (int i = 0; i < cols.Length; i++)
                ColliderRegistry.Unregister(cols[i]);
        }
    }
}
