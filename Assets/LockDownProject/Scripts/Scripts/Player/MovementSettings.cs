using UnityEngine;

public interface IPlayerMoveInfoProvider
{
    float GetDesiredGait();
}

public class MovementSettings : MonoBehaviour, IPlayerMoveInfoProvider
{
    [Header("Speeds")]
    [SerializeField] private float legWoundedSpeed = 1.0f;
    [SerializeField] private float proneSpeed = 0.75f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float tacticalSprintSpeed = 6.5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.0f;

    private PlayerInjuryState injuryState;

    private float gait;

    public float GetSpeed(in MovementMode mode, bool isForward)
    {
        if (injuryState.HasBothLegInjuries) return legWoundedSpeed;
        if (mode.prone) return proneSpeed;
        if (mode.crouch) return crouchSpeed;

        bool legsHealthy = !injuryState.HasAnyLegInjury;
        if (isForward && legsHealthy)
        {
            if (mode.sprint) return sprintSpeed;
            if (mode.tacticalSprint) return tacticalSprintSpeed;
        }

        return walkSpeed;
    }

    public bool IsForward(Vector2 moveInfo, float dot = 0.65f)
    {
        if (moveInfo == Vector2.zero) return false;
        return moveInfo.normalized.y > dot;
    }

    public bool CanJump(in MovementMode mode, bool isJumped, bool isGrounded)
    {
        bool leftOk = !injuryState.leftLegInjured;
        bool rightOk = !injuryState.rightLegInjured;
        bool canJump = leftOk || rightOk;

        return isJumped && !mode.prone && isGrounded && canJump;
    }

    public float GetJumpHeight()
    {
        return jumpHeight;
    }

    public void CheckDesiredGait(Vector2 moveInfo, in MovementMode mode, float speed)
    {
        if (mode.tacticalSprint && speed == tacticalSprintSpeed) gait = 3.0f;
        else if (mode.sprint && speed == sprintSpeed) gait = 2.0f;
        else gait = moveInfo.magnitude;
    }

    public float GetDesiredGait()
    {
        return gait;
    }

    public bool CanFire(in MovementMode mode)
    {
        return !mode.tacticalSprint;
    }

    public bool CanAim(in MovementMode mode, bool isAim)
    {
        if (injuryState.HasBothArmInjuries) return false;
        if (!isAim) return false;
        if (mode.sprint) return false;
        if (mode.tacticalSprint) return false;

        return true;
    }

    public bool CanReload(in MovementMode mode, bool isReload)
    {
        return CanPerformRequestedAction(mode, isReload);
    }

    public bool CanChangeFireMode(in MovementMode mode, bool isChangeFireMode)
    {
        return CanPerformRequestedAction(mode, isChangeFireMode);
    }

    public bool CanChangeWeapon(in MovementMode mode, bool isChangeWeapon)
    {
        return CanPerformRequestedAction(mode, isChangeWeapon);
    }

    public void ApplyInjuryState(PlayerInjuryState newInjuryState)
    {
        injuryState = newInjuryState;
    }

    public bool HasAnyArmInjury()
    {
        return injuryState.HasAnyArmInjury;
    }

    public bool HasBothArmInjuries()
    {
        return injuryState.HasBothArmInjuries;
    }

    private static bool CanPerformRequestedAction(in MovementMode mode, bool isRequested)
    {
        return isRequested && !mode.tacticalSprint;
    }
}
