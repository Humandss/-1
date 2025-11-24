using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGateTrigger : MonoBehaviour
{
    [SerializeField] private TutorialStep step;   // 이 존이 담당하는 튜토리얼 단계

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 플레이어가 존에 들어오면 그 단계 클리어
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteStep(step);
            Destroy(gameObject);
        }
    }
}
