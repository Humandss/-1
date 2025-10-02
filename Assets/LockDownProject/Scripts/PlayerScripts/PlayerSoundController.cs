using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [Header("Ref")]
    private PlayerAnimationController playerAnimationController;

    [Header("Providers")]
    private IPlayerAnimationGetFloatWeight weightProvider;

    [Header("Weapon swapping")]
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioClip unEquipSound;

    [Header("Movement")]
    [SerializeField] private List<AudioClip> walkSounds;
    [SerializeField] private float walkDelay = 0.4f;
    [SerializeField] private List<AudioClip> sprintSounds;
    [SerializeField] private float sprintDelay = 0.4f;
    [SerializeField] private float tacSprintDelay = 0.4f;

    [Header("Jumping")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Aiming")]
    [SerializeField] private AudioClip aimInSound;
    [SerializeField] private AudioClip aimOutSound;

    [Header("FireMode")]
    [SerializeField] private AudioClip fireModeSwitchSound;

    private AudioSource playerAudioSource;
    private bool isSourceValid;

    private float playback = 0f;

    public static AudioClip GetRandomAudioClip(List<AudioClip> audioClips)
    {
        int index = Random.Range(0, audioClips.Count - 1);
        return audioClips[index];
    }

    private void Start()
    {
        playerAudioSource = GetComponent<AudioSource>();
        isSourceValid = playerAudioSource != null;
   
    }

    private void PlayMovementSounds(float gait, float error = 0.4f)
    {
        if (gait >= error && gait <= 1f + error)
        {
            if (playback >= walkDelay)
            {
                PlayWalkSound();
                playback = 0f;
            }
            return;
        }

        if (gait >= 1f + error && gait <= 2f + error)
        {
            if (playback >= sprintDelay)
            {
                PlaySprintSound();
                playback = 0f;
            }
            return;
        }

        if (gait >= 2f + error && gait <= 3f)
        {
            if (playback >= tacSprintDelay)
            {
                PlaySprintSound();
                playback = 0f;
            }
        }
    }

    private void Update()
    {
        float gait = playerAnimationController.GetFloatGaitWeight();
        if (Mathf.Approximately(gait, 0f) || playerAnimationController.GetBoolIsInAirWeight())
        {
            playback = 0f;
            return;
        }

        PlayMovementSounds(gait);
        playback += Time.deltaTime;
    }

    private bool CheckAudioSource()
    {
        if (!isSourceValid)
        {
            Debug.LogWarning($"Player Audio Source is invalid!");
            return false;
        }

        return true;
    }

    public void PlayAimSound(bool isAimIn = true)
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(isAimIn ? aimInSound : aimOutSound);
    }

    public void PlayFireModeSwitchSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(fireModeSwitchSound, Random.Range(0.2f, 0.25f));
    }

    public void PlayEquipSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(equipSound);
    }

    public void PlayUnEquipSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(unEquipSound);
    }

    public void PlayWalkSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(GetRandomAudioClip(walkSounds));
    }

    public void PlaySprintSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(GetRandomAudioClip(sprintSounds));
    }

    public void PlayJumpSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(jumpSound);
    }

    public void PlayLandSound()
    {
        if (!CheckAudioSource()) return;
        playerAudioSource.PlayOneShot(landSound);
    }

}
