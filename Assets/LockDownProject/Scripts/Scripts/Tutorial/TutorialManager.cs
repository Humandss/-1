using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StepData
{
    public TutorialStep step;
    public GameObject gateWall;
    public AudioClip tutorialAudio;

    [TextArea] public string title;
    [TextArea] public string body;
}

public enum TutorialStep
{
    Move,
    Sprint,
    Jump,
    Crouch,
    Prone,
    EneterShootingRange,
    ShootTargets,
    Penetration_Terrain,
    Ricochet,
    Health_Body_Parts,
    Item_IFAK,
    Item_Tourniquet,
    Item_Splint,
    Item_CMS,
    ClearRoom,
    Extract,
    Done
}
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialUIManager tutorialUI;
    [SerializeField] private CharacterController playerCC;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private List<StepData> steps = new List<StepData>();
    [SerializeField] private HealthManager healthManager;
    [SerializeField] private Transform extractRoom;
    private HashSet<TutorialStep> clearedSteps = new HashSet<TutorialStep>();
    private AudioSource audioSource;
    [SerializeField] private GameObject dummy1;
    [SerializeField] private GameObject dummy2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("[TutorialManager] audioSource is NULL");
        }

    
    }

    private void Start()
    {
        //StartStep(TutorialStep.Move);
        dummy1.SetActive(false);
        dummy2.SetActive(false);
    }
    public void StartStep(TutorialStep step)
    {
        var data = steps.Find(s => s.step == step);

        if (data.step == TutorialStep.Penetration_Terrain) dummy1.SetActive(true);
        if (data.step == TutorialStep.Ricochet) 
        {
            dummy2.SetActive(true);
            dummy1.SetActive(false);
        }
        if (data.step == TutorialStep.Health_Body_Parts) 
        {
            Destroy(dummy1);
            Destroy(dummy2);
        }
        if (data.step == TutorialStep.Item_IFAK) healthManager.GetLBleeding();

        if (data.step == TutorialStep.Item_Tourniquet) healthManager.GetHBleeding();

        if (data.step == TutorialStep.Item_Splint) healthManager.GetFracture();

        if (data.step == TutorialStep.Item_CMS) healthManager.GetBlackout();

        if (data.step == TutorialStep.ClearRoom) TransportPlayerToExtractRoom();

        if (data != null && tutorialUI != null)
        {
            audioSource.PlayOneShot(data.tutorialAudio);
            tutorialUI.Show(data.title, data.body);
            
        }
    }

    // 이 함수가 호출되면 해당 단계 게이트를 삭제
    public void CompleteStep(TutorialStep step)
    {
        if (clearedSteps.Contains(step)) return; // 이미 클리어 했으면 무시
        clearedSteps.Add(step);

        // 이 단계에 해당하는 게이트 찾기
        StepData data = steps.Find(s => s.step == step);
        if (data != null)
        {
            if(data.gateWall !=null) data.gateWall.SetActive(false);
            audioSource.Stop();
        }

        if (tutorialUI != null)
        {
            tutorialUI.Hide();
        }

        var s = GetNextSteps(step);
        if(s != null) StartStep(s.Value);
        else tutorialUI.Hide();

    }
    private TutorialStep? GetNextSteps(TutorialStep step)
    {
        int index = steps.FindIndex(s => s.step == step);
        if (index < 0 || index >= steps.Count - 1) return null;

        return steps[index + 1].step;

    }

    private void TransportPlayerToExtractRoom()
    {
        if (extractRoom == null || playerCC == null || playerTransform == null) return;

        playerCC.enabled = false;
        playerTransform.SetLocalPositionAndRotation(extractRoom.transform.position, extractRoom.rotation);
        playerCC.enabled = true;

        Invoke(nameof(HideUI), 5.0f);
   
    }
    private void HideUI()
    { 
       tutorialUI.Hide();
    }
    private void ResetPlayer()
    {
        //Debug.Log("반복실행중");
        if (extractRoom == null || playerCC == null || playerTransform == null) return;

        playerCC.enabled = false;
        playerTransform.SetLocalPositionAndRotation(extractRoom.transform.position, extractRoom.rotation);
        playerCC.enabled = true;

        healthManager.GetInitializeHealth();
    }
    private void FixedUpdate()
    {
        bool isDead = healthManager.CheckHP();
        if(isDead) ResetPlayer();
    }
}
