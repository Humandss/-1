using UnityEngine;

public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}
public class EnemyStateMachine : MonoBehaviour
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public void Tick()
    {
        CurrentState?.Execute();
    }
}
