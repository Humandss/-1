using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSound : MonoBehaviour
{
    private FPSWeaponSettings _settings;
    private AudioSource _audioSource;

    private void Awake()
    {
        _settings = transform.parent.GetComponent<Weapon>().weaponSettings;
        _audioSource = transform.root.GetComponentInChildren<AudioSource>();
    }

    public void PlayFireSound()
    {
        if (_audioSource == null)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        _audioSource.pitch = Random.Range(_settings.firePitchRange.x, _settings.firePitchRange.y);
        _audioSource.volume = Random.Range(_settings.fireVolumeRange.x, _settings.fireVolumeRange.y);
        _audioSource.PlayOneShot(FPSPlayerSound.GetRandomAudioClip(_settings.fireSounds));
    }

    public void PlayWeaponSound(int clipIndex)
    {
        if (clipIndex < 0 || clipIndex > _settings.weaponEventSounds.Count - 1)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid index!");
            return;
        }

        if (_audioSource == null)
        {
            Debug.LogWarning($"Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        _audioSource.PlayOneShot(_settings.weaponEventSounds[clipIndex]);
    }
}
