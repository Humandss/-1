using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSound : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HealthManager health;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private AudioClip deadClip;
    private AudioSource heartbeatSource;
    private AudioSource deadSource;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)]
    private float startThreshold = 0.3f;   // 체력 임계점

    private void Awake()
    {
        if (heartbeatClip == null)
        {
            Debug.LogWarning("[HealthSound] heartbeatClip 안 넣었음");
            return;
        }

        // AudioSource 자동 생성 (씬에 따로 안 놔도 됨)
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.clip = heartbeatClip;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f;
        heartbeatSource.volume = 1f;

        deadSource = gameObject.AddComponent<AudioSource>();
        deadSource.clip = deadClip;
        deadSource.loop = true;
        deadSource.playOnAwake = false;
        deadSource.spatialBlend = 0f;
        deadSource.volume = 1f;

    }

    private void Update()
    {
        PlayHeartBeatSound();
        PlayDeadSound();

    }
    private void PlayDeadSound()
    {
        if (health == null || heartbeatSource == null || heartbeatClip == null)
            return;

        if(health.CheckIsDead())
        {
            if (!deadSource.isPlaying) deadSource.Play();             
        }
        else
        {
            deadSource.Stop();
        }
    }

    private void PlayHeartBeatSound()
    {
        if (health == null || heartbeatSource == null || heartbeatClip == null)
            return;

        float hpRatio = Mathf.Clamp01(health.GetTotalHP() / health.GetMaxHP());

        // 죽었으면 강제 OFF
        if (health.CheckIsDead())
        {
            if (heartbeatSource.isPlaying)
                heartbeatSource.Stop();
            return;
        }

        // 체력이 임계치 이하면 재생, 아니면 정지
        if (hpRatio <= startThreshold)
        {
            if (!heartbeatSource.isPlaying)
            {
                Debug.Log($"[HealthSound] Heartbeat Play (hpRatio={hpRatio:F2})");
                heartbeatSource.Play();
            }
        }
        else
        {
            if (heartbeatSource.isPlaying)
            {
                Debug.Log($"[HealthSound] Heartbeat Stop  (hpRatio={hpRatio:F2})");
                heartbeatSource.Stop();
            }
        }
    }

}
