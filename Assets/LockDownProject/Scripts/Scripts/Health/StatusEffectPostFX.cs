using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class StatusEffectPostFX : MonoBehaviour
{
    [SerializeField] private Volume volume;
    private Vignette vig;
    private ChromaticAberration ca;
    private FilmGrain grain;

    [SerializeField, Range(0f, 10f)] float speed = 1.0f;   
    [SerializeField] float min = 0.3f;
    [SerializeField] float max = 0.6f;

    private void Awake()
    {
        volume = GetComponent<Volume>();

        volume.profile.TryGet(out vig);
    }
    
    public void PlayTunnelVisionEffect()
    {
        if (vig == null) return;
     
        float t = 0.5f * (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f);
        vig.intensity.Override(Mathf.Lerp(min, max, t));
    }
}
