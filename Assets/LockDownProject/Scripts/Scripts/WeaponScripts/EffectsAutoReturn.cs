using UnityEngine;


public class EffectsAutoReturn : MonoBehaviour
{
    private ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        if (particle == null )
        {
            Debug.LogWarning("[MuzzleFlashAutoReturn] particle is NULL");
        }

    }

    private void OnEnable()
    {
        if (particle != null)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }

    private void Update()
    {
        //파티클이 사라졌다면 리턴
        if (!particle.IsAlive(true)) PoolManager.Instance.Return(gameObject);
       
    }

   
}
