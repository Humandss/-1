using System.Collections.Generic;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// Collider.GetInstanceID() → Collider 매핑. RaycastHit.colliderInstanceID로 받은 ID를
    /// 메인 스레드에서 Collider 인스턴스로 복원하기 위한 전역 레지스트리.
    /// 등록 주체: ColliderRegistrar 컴포넌트 또는 직접 호출.
    /// </summary>
    public static class ColliderRegistry
    {
        private static readonly Dictionary<int, Collider> map = new Dictionary<int, Collider>();

        public static void Register(Collider col)
        {
            if (col == null) return;
            map[col.GetInstanceID()] = col;
        }

        public static void Unregister(Collider col)
        {
            if (col == null) return;
            map.Remove(col.GetInstanceID());
        }

        public static bool TryGet(int colliderInstanceID, out Collider col)
        {
            return map.TryGetValue(colliderInstanceID, out col);
        }

        public static int Count => map.Count;

        public static void Clear() => map.Clear();
    }
}
