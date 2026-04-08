
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

    [Header("Volume")]
    [SerializeField, Range(0f, 2f)] private float ricochetVolume = 1.0f;
    [SerializeField, Range(0f, 2f)] private float defaultImpactVolume = 1.0f;
    [SerializeField, Range(0f, 2f)] private float metalImpactVolume = 1.0f;
    [SerializeField, Range(0f, 2f)] private float bodyImpactVolume = 1.0f;
    [SerializeField, Range(0f, 2f)] private float headImpactVolume = 1.0f;

    [Header("Ricochet Sound")]
    [SerializeField] private List<AudioClip> ricochetSounds;

    [Header("Default Impact Sound")]
    [SerializeField] private List<AudioClip> defaultImpactSounds;

    [Header("Metal Impact Sound")]
    [SerializeField] private List<AudioClip> metalImpactSounds;

    [Header("Bullet Fly By Sound")]
    [SerializeField] private List<AudioClip> bulletFlyBySounds;

    [Header("Human Impact Sound")]
    [SerializeField] private List<AudioClip> bodyImpactSounds;
    [SerializeField] private List<AudioClip> headImpactSounds;

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
        if (audioClips == null || audioClips.Count == 0) return null;
        int index = Random.Range(0, audioClips.Count);
        return audioClips[index];
    }
    public void PlayRicochetSound()
    {
        if (bulletAudioSource == null) return;
        AudioClip clip = GetRandomAudioClip(ricochetSounds);
        if (clip == null) return;
        bulletAudioSource.PlayOneShot(clip, ricochetVolume);
        return;
    }
    public void PlayDefaultImpactSound(Vector3 hitPoint)
    {
        AudioClip clip = GetRandomAudioClip(defaultImpactSounds);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, hitPoint, defaultImpactVolume);
        return;
    }
    public void PlayMetalImpactSound(Vector3 hitPoint)
    {
        AudioClip clip = GetRandomAudioClip(metalImpactSounds);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, hitPoint, metalImpactVolume);
        return;
    }
    public void PlayBodyImpactSound(Vector3 hitPoint)
    {
        AudioClip clip = GetRandomAudioClip(bodyImpactSounds);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, hitPoint, bodyImpactVolume);
        return;
    }
    public void PlayHeadImpactSound(Vector3 hitPoint)
    {
        AudioClip clip = GetRandomAudioClip(headImpactSounds);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, hitPoint, headImpactVolume);
        return;
    }
}
