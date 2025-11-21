using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySound : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<AudioClip> walkSounds;
    [SerializeField] private float walkDelay = 0.4f;
    [SerializeField] private List<AudioClip> sprintSounds;
    [SerializeField] private float sprintDelay = 0.4f;
    [SerializeField] private float tacSprintDelay = 0.4f;

    [Header("Aiming")]
    [SerializeField] private AudioClip aimInSound;
    [SerializeField] private AudioClip aimOutSound;

    private AudioSource enemyAudioSource;
    private bool isSourceValid;

    public static AudioClip GetRandomAudioClip(List<AudioClip> audioClips)
    {
        int index = Random.Range(0, audioClips.Count - 1);
        return audioClips[index];
    }

    private void Start()
    {
        enemyAudioSource = GetComponent<AudioSource>();
        isSourceValid = enemyAudioSource != null;

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
        enemyAudioSource.PlayOneShot(isAimIn ? aimInSound : aimOutSound);
    }

    public void PlayWalkSound()
    {
        if (!CheckAudioSource()) return;
        enemyAudioSource.PlayOneShot(GetRandomAudioClip(walkSounds));
    }

    public void PlaySprintSound()
    {
        if (!CheckAudioSource()) return;
        enemyAudioSource.PlayOneShot(GetRandomAudioClip(sprintSounds));
    }

}
