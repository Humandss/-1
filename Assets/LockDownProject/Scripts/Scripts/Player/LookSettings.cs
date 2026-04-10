using UnityEngine;

public class LookSettings : MonoBehaviour
{
    [Header("CameraPositionForPosition")]
    [SerializeField] private float proneCameraPos = 1.0f;
    [SerializeField] private float crouchCameraPos = 1.2f;
    [SerializeField] private float idleCameraPos = 1.65f;

    [Header("RotationSpeeds")]
    [SerializeField] private float woundedRotationSpeed = 0.05f;
    [SerializeField] private float proneRotationSpeed = 0.15f;
    [SerializeField] private float crouchRotationSpeed = 0.2f;
    [SerializeField] private float walkRotationSpeed = 0.2f;
    [SerializeField] private float sprintRotationSpeed = 0.15f;
    [SerializeField] private float tacticalRotationSprintSpeed = 0.1f;

    [Header("MosueSensitivity")]
    [SerializeField] private float bothArmWoundedMouseSensitivity = 0.025f;
    [SerializeField] private float oneArmWoundedMouseSensitivity = 0.05f;
    [SerializeField] private float mouseSensitivity = 0.1f;

    [Header("ChangePositionTime")]
    [SerializeField] private float changeToProneTime = 0.5f;
    [SerializeField] private float changeToCrouchTime = 0.1f;
    [SerializeField] private float changeToIdleTime = 0.2f;
    private PlayerInjuryState injuryState;

    public void ApplyInjuryState(PlayerInjuryState newInjuryState)
    {
        injuryState = newInjuryState;
    }

    public float GetRotationSpeed(in MovementMode mode)
    {
        if (injuryState.HasAnyArmInjury) return woundedRotationSpeed;
        if (mode.prone) return proneRotationSpeed;
        if (mode.crouch) return crouchRotationSpeed;
        if (mode.sprint) return sprintRotationSpeed;
        if (mode.tacticalSprint) return tacticalRotationSprintSpeed;

        return walkRotationSpeed;
    }

    public float GetCameraPosition(in MovementMode mode)
    {
        if (mode.prone) return proneCameraPos;
        if (mode.crouch) return crouchCameraPos;

        return idleCameraPos;
    }

    public float GetCameraChangeTime(in MovementMode mode)
    {
        if (mode.prone) return changeToProneTime;
        if (mode.crouch) return changeToCrouchTime;

        return changeToIdleTime;
    }

    public float GetMouseSensitivity()
    {
        if (injuryState.HasBothArmInjuries) return bothArmWoundedMouseSensitivity;
        if (injuryState.HasAnyArmInjury) return oneArmWoundedMouseSensitivity;

        return mouseSensitivity;
    }
}
