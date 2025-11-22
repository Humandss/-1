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

    [Header("Dialogues")]
    [SerializeField] private List<AudioClip> attackDialogue;
    [SerializeField] private List<AudioClip> patrolDialogue;
    [SerializeField] private List<AudioClip> chaseDialogue;
    [SerializeField] private List<AudioClip> retreatDialogue;

    [Header("Cooldowns")]
    [SerializeField] private float globalCooldown = 1.5f;   // 아무 말이나 최소 간격
    [SerializeField] private float attackCooldown = 3.0f;
    [SerializeField] private float chaseCooldown = 3.0f;
    [SerializeField] private float patrolCooldown = 5.0f;
    [SerializeField] private float retreatCooldown = 5.0f;

    private float nextGlobalTime = 0.0f;
    private float lastChaseTime = float.NegativeInfinity;
    private float lastPatrolTime = float.NegativeInfinity;
    private float lastRetreatTime = float.NegativeInfinity;
    private float lastAttackTime = float.NegativeInfinity;

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

    private bool TryPlayDialogue(List<AudioClip> audioClips, ref float lastTime, float localCooldown)
    {
        if (!CheckAudioSource()) return false;

        float now = Time.time;

        // 전역 간격
        if (now < nextGlobalTime) return false;

        // 각 타입별 간격: 같은 타입 연속 난사 방지
        if (now - lastTime < localCooldown) return false;

        AudioClip clip = GetRandomAudioClip(audioClips);
        if (clip == null) return false;
  
        //이전에 말하고 있는거 스탑 후 재생
        if (enemyAudioSource.isPlaying) enemyAudioSource.Stop();
  
        enemyAudioSource.PlayOneShot(clip);

        lastTime = now;
        nextGlobalTime = now + globalCooldown;
        return true;
    }

    public void PlayAttackDialogue()
    {
        TryPlayDialogue(attackDialogue, ref lastAttackTime, attackCooldown);
    }
    public void PlayChaseDialogue()
    {
        TryPlayDialogue(chaseDialogue, ref lastChaseTime, chaseCooldown);
    }

    public void PlayPatrolDialogue()
    {
        TryPlayDialogue(patrolDialogue, ref lastPatrolTime, patrolCooldown);
    }

    public void PlayRetreatDialogue()
    {
        TryPlayDialogue(retreatDialogue, ref lastRetreatTime, retreatCooldown);
    }
}
