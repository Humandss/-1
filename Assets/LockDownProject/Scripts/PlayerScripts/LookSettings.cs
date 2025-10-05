using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookSettings : MonoBehaviour
{
    [Header("CameraPositionForPosition")]
    [SerializeField] private float proneCameraPos = 0.5f;
    [SerializeField] private float crouchCameraPos = 1.0f;
    [SerializeField] private float idleCameraPos = 1.65f;

    [Header("RotationSpeeds")]
    [SerializeField] private float proneRotationSpeed = 0.15f;
    [SerializeField] private float crouchRotationSpeed = 0.2f;
    [SerializeField] private float walkRotationSpeed = 0.2f;
    [SerializeField] private float sprintRotationSpeed = 0.15f;
    [SerializeField] private float tacticalRotationSprintSpeed = 0.1f;

    [Header("MosueSensitivity")]
    [SerializeField] private float mouseSensitivity = 0.1f;

    [Header("ChangePositionTime")]
    [SerializeField] private float changeToProneTime = 0.5f;
    [SerializeField] private float changeToCrouchTime = 0.1f;
    [SerializeField] private float changeToIdleTime = 0.2f;



    public float GetRotationSpeed(in MovementMode mode)
    {
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
        return mouseSensitivity;
    }
}
