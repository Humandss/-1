using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StepData
{
    public TutorialStep step;
    public GameObject gateWall;

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
    Aim,
    ShootTargets,
    FirstEnemySpotted,
    TakeHit,
    UseMed,
    ClearRoom,
    Extract,
    Done
}
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialUIManager tutorialUI;
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private List<StepData> steps = new List<StepData>();
    private HashSet<TutorialStep> clearedSteps = new HashSet<TutorialStep>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartStep(TutorialStep.Move);
    }
    public void StartStep(TutorialStep step)
    {
        var data = steps.Find(s => s.step == step);
        if (data != null && tutorialUI != null)
        {
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
        if (data != null && data.gateWall != null)
        {
            data.gateWall.SetActive(false);  
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
}
