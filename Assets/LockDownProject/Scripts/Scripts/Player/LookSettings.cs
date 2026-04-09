using UnityEngine;

public class LookSettings : MonoBehaviour
{
    [Header("Refs")]
    private HealthManager healthManager;
    [Header("Providers")]
    private IHealthStateProvider healthStateProvider;
    private MovementSettings movementSettings;

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

    private void Awake()
    {
        healthManager = GetComponent<HealthManager>();
        if (healthManager == null)
        {
            Debug.LogWarning("[PlayerController]  healthManager is NULL");
        }

        healthStateProvider = healthManager as IHealthStateProvider;
        if (healthStateProvider == null)
        {
            Debug.LogWarning("[PlayerController] healthStateProvider is NULL");
        }

        movementSettings = GetComponent<MovementSettings>();
        if (movementSettings == null)
        {
            Debug.LogWarning("[PlayerController] movementSettings is NULL");
        }
    }

    public float GetRotationSpeed(in MovementMode mode)
    {
        if (movementSettings != null && movementSettings.HasAnyArmInjury()) return woundedRotationSpeed;
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
        if (movementSettings != null && movementSettings.HasBothArmInjuries()) return bothArmWoundedMouseSensitivity;
        if (movementSettings != null && movementSettings.HasAnyArmInjury()) return oneArmWoundedMouseSensitivity;

        return mouseSensitivity;
    }
}
