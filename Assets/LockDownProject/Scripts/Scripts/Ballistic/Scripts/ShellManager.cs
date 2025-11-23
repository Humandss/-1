using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellManager : MonoBehaviour
{
    [Header("Shell Sounds")]
    [SerializeField] private List<AudioClip> shellSounds;

    [SerializeField] private float shellLifeTime = 7.5f;

    private AudioSource shellAudioSource;
    private bool isSourceValid;
    private LayerMask layerMask;
    bool isPlayOnced = false;
    private void Start()
    {
        shellAudioSource = GetComponent<AudioSource>();
        if(shellAudioSource == null )
        {
            Debug.LogWarning("[ShellManager] AudioSource is NULL");
        }
        isSourceValid = shellAudioSource != null;
        layerMask = LayerMask.GetMask("Floor");
    }

    private static AudioClip GetRandomAudioClip(List<AudioClip> audioClips)
    {
        int index = Random.Range(0, audioClips.Count - 1);
        return audioClips[index];
    }
    private bool CheckAudioSource()
    {
        if (!isSourceValid)
        {
            Debug.LogWarning($"Shell Audio Source is invalid!");
            return false;
        }

        return true;
    }
    public void PlayShellSound()
    {
        if (!CheckAudioSource()) return;
        shellAudioSource.PlayOneShot(GetRandomAudioClip(shellSounds));
        isPlayOnced=false;
    }

    private void OnCollisionEnter(Collision collision)
    {
       if(!isPlayOnced) PlayShellSound();
    }
    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(DestroyShell), shellLifeTime);
    }

    private void DestroyShell()
    {
        Destroy(gameObject);
    }
}
