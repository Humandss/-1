
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public interface IBulletSoundProvider
{
    void PlayRicochetSound();

    void PlayDefaultImpactSound(Vector3 hitPoint);

    void PlayMetalImpactSound(Vector3 hitPoint);
}

public class BulletSoundController : MonoBehaviour,IBulletSoundProvider
{
    private AudioSource bulletAudioSource;

    [Header("Ricochet Sound")]
    [SerializeField] private List<AudioClip> ricochetSounds;

    [Header("Default Impact Sound")]
    [SerializeField] private List<AudioClip> defaultImpactSounds;

    [Header("Metal Impact Sound")]
    [SerializeField] private List<AudioClip> metalImpactSounds;

    [Header("Bullet Fly By Sound")]
    [SerializeField] private List<AudioClip> bulletFlyBySounds;


    private void Awake()
    {
        InitializeBulletAudioSource();
    }
    public void InitializeBulletAudioSource()
    {
        bulletAudioSource = GetComponent<AudioSource>();
        if (bulletAudioSource == null)
        {
            Debug.LogWarning("[BulletSoundController] bulletAudioSource is NULL ");
           
        }
    }
    private static AudioClip GetRandomAudioClip(List<AudioClip> audioClips)
    {
        int index = Random.Range(0, audioClips.Count - 1);
        return audioClips[index];
    }
    public void PlayRicochetSound()
    {
        bulletAudioSource.PlayOneShot(GetRandomAudioClip(ricochetSounds));
        return;
    }
    public void PlayDefaultImpactSound(Vector3 hitPoint)
    {
        AudioSource.PlayClipAtPoint(GetRandomAudioClip(defaultImpactSounds), hitPoint);
        return;
    }
    public void PlayMetalImpactSound(Vector3 hitPoint)
    {
        AudioSource.PlayClipAtPoint(GetRandomAudioClip(metalImpactSounds), hitPoint);
        return;
    }
}
