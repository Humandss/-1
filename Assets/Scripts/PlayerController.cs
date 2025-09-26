using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// �÷��̾� ���� ����ü
public struct MovementMode
{
    public bool prone;
    public bool crouch;
    public bool sprint;
    public bool tacticalSprint;

}
public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private PlayerLookController lookController;
    [SerializeField] private WeaponRigBinder rigBinder;
    [SerializeField] private PlayerWeaponController weaponController;

    [Header("PlayerControllerClassComponent")]
    [SerializeField] private PlayerMovementManager movementManager;
    [SerializeField] private PlayerLookManager lookManager; 

    private void Awake()
    {
        inputController=GetComponent<PlayerInputController>();
        movementController=GetComponent<PlayerMovementController>();
        lookController=GetComponent<PlayerLookController>();

        rigBinder=GetComponent<WeaponRigBinder>();
        weaponController =GetComponent<PlayerWeaponController>();

    }
   
    private void Update()
    {
        //������ ��忡 ���� ���� ��ȭ ����
        var movementInfo = new MovementMode
        {
            prone = inputController.Prone,
            crouch = inputController.Crouch,
            sprint = inputController.Sprint,
            tacticalSprint = inputController.TacticalSprint,
        };
        //������
        bool isForward = movementManager.IsForward(inputController.Move);
        float speed = movementManager.GetSpeed(inputController.Move, movementInfo, isForward);
        bool canJumpe = movementManager.CanJump(movementInfo, inputController.Jump);
        //ī�޶� 
        float rotationSpeed = lookManager.GetRotationSpeed(movementInfo);
        float cameraPosition = lookManager.GetCameraPosition(movementInfo);
        float cameraChangeSpeed = lookManager.GetCameraChangeTime(movementInfo);
        bool isFreeLook = inputController.FreeLook;
        //get�Լ�
        float mSensitivity = lookManager.GetMouseSensitivity();
        float h = movementManager.GetJumpHeight();
        //���
       // bool isFire- 
        //���
        movementController.UpdateMovement(inputController.Move, speed, canJumpe, h);
        lookController.UpdateLook(inputController.Look, rotationSpeed, cameraPosition,
                                  cameraChangeSpeed, mSensitivity, isFreeLook);

    }
    
 
}
[System.Serializable]
public class PlayerActionManager
{

}
[System.Serializable]
public class PlayerMovementManager
{
    [Header("PlayerRoot")]
    [SerializeField] private Transform playerRoot;

    [Header("Speeds")]
    [SerializeField] private float proneSpeed = 0.75f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float tacticalSprintSpeed = 6.5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.0f;


    public float GetSpeed(Vector2 moveInfo, in MovementMode mode, bool isForward)
    {
        if(mode.prone) return proneSpeed;

        if(mode.crouch) return crouchSpeed;

        if(mode.sprint && isForward) return sprintSpeed;

        if(mode.tacticalSprint && isForward) return tacticalSprintSpeed;

        return walkSpeed;
    }

    public bool IsForward(Vector2 moveInfo, float dot=0.65f)
    {
        //�Է� ������ ������ false => �������� �ʴ� ����
        if(moveInfo == Vector2.zero) return false;

        Vector2 wish = new Vector2
        (
            playerRoot.forward.x * moveInfo.y + playerRoot.right.x * moveInfo.x,
            playerRoot.forward.z * moveInfo.y + playerRoot.right.z * moveInfo.x
        );

        if(wish.sqrMagnitude < 1e-6f) return false;

        //ĳ���� ���� ���� ���� ��
        Vector2 fwd = new Vector2 (playerRoot.forward.x, playerRoot.forward.z);
        //�� ���� ����ȭ(�񱳸� �����ϱ� ���� ���̸� 1�� ����) => cos������ dot���� ũ�� ���� �Ǵ�
        return Vector2.Dot(wish.normalized, fwd.normalized) > dot;

    }

    public bool CanJump(in MovementMode mode, bool isJumped)
    {
        //������ ���� ���� + �������� ���� ���¿����� ���� �����ϰԲ�
        if (isJumped && !mode.prone) return true;

        return false;

    }
    public float GetJumpHeight()
    {
        return jumpHeight;
    }
}

[System.Serializable]
public class PlayerLookManager
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
