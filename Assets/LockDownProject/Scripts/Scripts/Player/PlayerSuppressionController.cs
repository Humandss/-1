using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGetPlayerSuprressionInfo
{
    public void AddHitSuppression(Vector3 sourcePos);
    public float GetSuppression01();

}
public class PlayerSuppressionController : MonoBehaviour, IGetPlayerSuprressionInfo
{
    [Header("Suppression Settings")]
    [SerializeField] private float maxSuppression = 100.0f;       // 서프레션 최대값
    [SerializeField] private float hitSuppressionAmount = 50.0f;  // 피탄 시 추가량
    [SerializeField] private float decayPerSecond = 20.0f;        // 초당 감소량

    private float currentSuppression;
    private float lastSuppressionTime;

    private void Update()
    {
        // 서프레션 자연 감소
        if (currentSuppression > 0f)
        {
            currentSuppression -= decayPerSecond * Time.deltaTime;
            if (currentSuppression < 0.0f) currentSuppression = 0.0f;
        }
    }

    public void AddHitSuppression(Vector3 sourcePos)
    {
        AddSuppression(hitSuppressionAmount, sourcePos);
    }

    private void AddSuppression(float amount, Vector3 sourcePos)
    {
        currentSuppression = Mathf.Clamp(currentSuppression + amount, 0.0f, maxSuppression);
        lastSuppressionTime = Time.time;

    }
    public float GetSuppression01()
    {
        return Mathf.Clamp01(currentSuppression / maxSuppression);
    }
}
