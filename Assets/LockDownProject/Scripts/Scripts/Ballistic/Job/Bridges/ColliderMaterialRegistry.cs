using System.Collections.Generic;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// Collider.GetInstanceID() → MaterialManager 매핑.
    /// MaterialManager가 OnEnable에서 자기 GameObject(+자식) Collider들을 자가 등록.
    /// 히트 드레인 시 GetComponent&lt;MaterialManager&gt; 비용을 피하기 위한 캐시.
    /// </summary>
    public static class ColliderMaterialRegistry
    {
        private static readonly Dictionary<int, MaterialManager> map = new Dictionary<int, MaterialManager>();

        public static void Register(Collider col, MaterialManager mat)
        {
            if (col == null || mat == null) return;
            map[col.GetInstanceID()] = mat;
        }

        public static void Unregister(Collider col)
        {
            if (col == null) return;
            map.Remove(col.GetInstanceID());
        }

        public static bool TryGet(int colliderInstanceID, out MaterialManager mat)
        {
            return map.TryGetValue(colliderInstanceID, out mat);
        }

        public static int Count => map.Count;

        public static void Clear() => map.Clear();
    }
}
