using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSound : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private float fireSfxVolume = 1.0f;
    [SerializeField] private float weaponEventSfxVolume = 1.0f;

    private FPSWeaponSettings settings;
    private AudioSource audioSource;

    private void Awake()
    {
        //settings = transform.parent.GetComponent<Weapon>().weaponSettings;
        // audioSource = transform.root.GetComponentInChildren<AudioSource>();
        var weapon = GetComponentInParent<Weapon>();
        settings = weapon.weaponSettings;
        audioSource = GetComponent<AudioSource>();

    }

    public void PlayFireSound()
    {
        
        if (audioSource == null)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        audioSource.pitch = Random.Range(settings.firePitchRange.x, settings.firePitchRange.y);
        //audioSource.PlayOneShot(FPSPlayerSound.GetRandomAudioClip(settings.fireSounds));

        var clip = FPSPlayerSound.GetRandomAudioClip(settings.fireSounds);
        if (clip != null)
        {
            float baseVolume = Random.Range(settings.fireVolumeRange.x, settings.fireVolumeRange.y);
            audioSource.PlayOneShot(clip, baseVolume * fireSfxVolume);
        }
    }

    public void PlayWeaponSound(int clipIndex)
    {
        if (clipIndex < 0 || clipIndex > settings.weaponEventSounds.Count - 1)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid index!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        audioSource.PlayOneShot(settings.weaponEventSounds[clipIndex], weaponEventSfxVolume);
    }
}
