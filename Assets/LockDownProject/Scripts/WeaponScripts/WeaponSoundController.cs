using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using UnityEngine;

public class WeaponSoundController : MonoBehaviour
{
    private FPSWeaponSettings settings;
    private AudioSource audioSource;

    private void Awake()
    {
        settings = GetComponentInParent<WeaponBase>().weaponSettings;
        audioSource = GetComponentInChildren<AudioSource>();
    }

    public void PlayFireSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[WeaponSoundController] Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        audioSource.pitch = Random.Range(settings.firePitchRange.x, settings.firePitchRange.y);
        audioSource.volume = Random.Range(settings.fireVolumeRange.x, settings.fireVolumeRange.y);
        audioSource.PlayOneShot(FPSPlayerSound.GetRandomAudioClip(settings.fireSounds));
    }

    public void PlayWeaponSound(int clipIndex)
    {
        if (clipIndex < 0 || clipIndex > settings.weaponEventSounds.Count - 1)
        {
            Debug.LogWarning("[WeaponSoundController] Failed to play weapon sound: invalid index!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("[WeaponSoundController] Failed to play weapon sound: invalid Audio Source!");
            return;
        }

        audioSource.PlayOneShot(settings.weaponEventSounds[clipIndex]);
    }
}
