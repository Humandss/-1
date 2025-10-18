
using UnityEngine;

public class LookSettings : MonoBehaviour
{
    [Header("Refs")]
    private HealthManager healthManager;
    [Header("Providers")]
    private IHealthStateProvider healthStateProvider;

    [Header("CameraPositionForPosition")]
    [SerializeField] private float proneCameraPos = 0.5f;
    [SerializeField] private float crouchCameraPos = 1.0f;
    [SerializeField] private float idleCameraPos = 1.65f;

    [Header("RotationSpeeds")]
    [SerializeField] private float fractureRotationSpeed = 0.1f;
    [SerializeField] private float proneRotationSpeed = 0.15f;
    [SerializeField] private float crouchRotationSpeed = 0.2f;
    [SerializeField] private float walkRotationSpeed = 0.2f;
    [SerializeField] private float sprintRotationSpeed = 0.15f;
    [SerializeField] private float tacticalRotationSprintSpeed = 0.1f;

    [Header("MosueSensitivity")]
    [SerializeField] private float bothArmFractureMouseSensitivity = 0.025f;
    [SerializeField] private float oneArmFractureMouseSensitivity = 0.05f;
    [SerializeField] private float mouseSensitivity = 0.1f;

    [Header("ChangePositionTime")]
    [SerializeField] private float changeToProneTime = 0.5f;
    [SerializeField] private float changeToCrouchTime = 0.1f;
    [SerializeField] private float changeToIdleTime = 0.2f;

    [Header("HealthState")]
    private bool isLeftLegFrac = false;
    private bool isRightLegFrac = false;
    private bool isLeftArmFrac = false;
    private bool isRightArmFrac = false;

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
    }
    public float GetRotationSpeed(in MovementMode mode)
    {
        if (isLeftLegFrac || isRightLegFrac) return fractureRotationSpeed;

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
        if (isLeftArmFrac && isRightArmFrac) return bothArmFractureMouseSensitivity;

        if (isLeftArmFrac || isRightArmFrac) return oneArmFractureMouseSensitivity;

        return mouseSensitivity;
    }
    public void CheckPlayerHealthState()
    {
        isLeftLegFrac = healthStateProvider.GetIsLeftLegFracture();
        isRightLegFrac = healthStateProvider.GetIsRightLegFracture();
        isLeftArmFrac = healthStateProvider.GetIsLeftArmFracture();
        isRightArmFrac = healthStateProvider.GetIsRightArmFracture();
    }
}
