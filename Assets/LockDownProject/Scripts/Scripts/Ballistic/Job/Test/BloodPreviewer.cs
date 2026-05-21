using System.Collections.Generic;
using UnityEngine;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// KriptoFX 같은 피 VFX 프리팹들을 차례로 미리보기하는 디버그 도구.
    /// 어떤 변종이 헤드샷/몸통/팔다리에 어울리는지 시각 비교용.
    ///
    /// 사용:
    /// 1. 빈 GameObject 만들고 이 컴포넌트 부착
    /// 2. previewPoint Transform 지정 (스폰 위치)
    /// 3. Blood Prefabs 리스트에 KriptoFX 프리팹들 다 드래그
    /// 4. Play → 화면 좌상단에 현재 프리팹 이름 + 키 안내 표시
    ///    - [N] / [→] : 다음 프리팹
    ///    - [P] / [←] : 이전 프리팹
    ///    - [Space] : 현재 프리팹 다시 스폰
    ///    - [1] : "Head용 후보"로 마킹 (콘솔 로그)
    ///    - [2] : "Body용 후보"로 마킹
    ///    - [3] : "이 변종은 별로" 마킹
    ///    - [S] : 마킹 결과를 콘솔에 정리 출력
    /// </summary>
    public class BloodPreviewer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform previewPoint;
        [SerializeField] private List<GameObject> bloodPrefabs = new List<GameObject>();

        [Header("Settings")]
        [SerializeField, Tooltip("스폰 시 회전 (피가 분출되는 방향)")]
        private Vector3 spawnEulerOverride = new Vector3(0f, 180f, 0f);
        [SerializeField, Tooltip("이전 스폰 인스턴스 자동 정리할지")]
        private bool autoCleanPrevious = true;
        [SerializeField, Tooltip("스폰 후 자동 삭제까지 시간(초). 0이면 자동 삭제 안 함")]
        private float autoDestroyAfter = 5f;

        private int currentIndex = 0;
        private GameObject currentInstance;

        // 분류 결과
        private readonly HashSet<int> headCandidates = new HashSet<int>();
        private readonly HashSet<int> bodyCandidates = new HashSet<int>();
        private readonly HashSet<int> rejected = new HashSet<int>();

        private void Start()
        {
            if (bloodPrefabs.Count > 0) SpawnCurrent();
        }

        private void Update()
        {
            if (bloodPrefabs.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentIndex = (currentIndex + 1) % bloodPrefabs.Count;
                SpawnCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentIndex = (currentIndex - 1 + bloodPrefabs.Count) % bloodPrefabs.Count;
                SpawnCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                MarkHead();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                MarkBody();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                MarkRejected();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                PrintSummary();
            }
        }

        private void SpawnCurrent()
        {
            if (autoCleanPrevious && currentInstance != null)
                Destroy(currentInstance);

            var prefab = bloodPrefabs[currentIndex];
            if (prefab == null) return;

            Vector3 pos = previewPoint != null ? previewPoint.position : transform.position;
            Quaternion rot = Quaternion.Euler(spawnEulerOverride);
            currentInstance = Instantiate(prefab, pos, rot);

            if (autoDestroyAfter > 0f) Destroy(currentInstance, autoDestroyAfter);

            Debug.Log($"[BloodPreviewer] Spawned [{currentIndex}] {prefab.name}");
        }

        private void MarkHead()
        {
            bodyCandidates.Remove(currentIndex);
            rejected.Remove(currentIndex);
            headCandidates.Add(currentIndex);
            Debug.Log($"[BloodPreviewer] ✅ HEAD 후보 → [{currentIndex}] {bloodPrefabs[currentIndex].name}");
        }

        private void MarkBody()
        {
            headCandidates.Remove(currentIndex);
            rejected.Remove(currentIndex);
            bodyCandidates.Add(currentIndex);
            Debug.Log($"[BloodPreviewer] ✅ BODY 후보 → [{currentIndex}] {bloodPrefabs[currentIndex].name}");
        }

        private void MarkRejected()
        {
            headCandidates.Remove(currentIndex);
            bodyCandidates.Remove(currentIndex);
            rejected.Add(currentIndex);
            Debug.Log($"[BloodPreviewer] ❌ REJECTED → [{currentIndex}] {bloodPrefabs[currentIndex].name}");
        }

        private void PrintSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== Blood Preview Summary =====");

            sb.AppendLine("\n[HEAD 후보]");
            foreach (var i in headCandidates) sb.AppendLine($"  - {bloodPrefabs[i].name}");

            sb.AppendLine("\n[BODY 후보]");
            foreach (var i in bodyCandidates) sb.AppendLine($"  - {bloodPrefabs[i].name}");

            sb.AppendLine("\n[REJECTED]");
            foreach (var i in rejected) sb.AppendLine($"  - {bloodPrefabs[i].name}");

            sb.AppendLine("\n[Unmarked]");
            for (int i = 0; i < bloodPrefabs.Count; i++)
                if (!headCandidates.Contains(i) && !bodyCandidates.Contains(i) && !rejected.Contains(i))
                    sb.AppendLine($"  - {bloodPrefabs[i].name}");

            Debug.Log(sb.ToString());
        }

        private void OnGUI()
        {
            if (bloodPrefabs.Count == 0)
            {
                GUI.Label(new Rect(10, 10, 600, 24), "[BloodPreviewer] Blood Prefabs 리스트가 비어있음");
                return;
            }

            string name = bloodPrefabs[currentIndex] != null ? bloodPrefabs[currentIndex].name : "(null)";
            string mark = headCandidates.Contains(currentIndex) ? " [HEAD ✅]"
                       : bodyCandidates.Contains(currentIndex) ? " [BODY ✅]"
                       : rejected.Contains(currentIndex) ? " [❌]"
                       : "";

            GUI.Box(new Rect(10, 10, 480, 130), "");
            GUI.Label(new Rect(20, 14, 460, 22),
                $"[{currentIndex + 1}/{bloodPrefabs.Count}] {name}{mark}");
            GUI.Label(new Rect(20, 36, 460, 22),
                "[N/→] 다음   [P/←] 이전   [Space] 다시 스폰");
            GUI.Label(new Rect(20, 58, 460, 22),
                "[1] HEAD 후보   [2] BODY 후보   [3] Reject");
            GUI.Label(new Rect(20, 80, 460, 22),
                "[S] 결과 요약 콘솔 출력");
            GUI.Label(new Rect(20, 102, 460, 22),
                $"HEAD:{headCandidates.Count}  BODY:{bodyCandidates.Count}  REJ:{rejected.Count}");
        }
    }
}
