using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IPlayerMoveInfoProvider
{
    float GetDesiredGait();
}
public class MovementSettings : MonoBehaviour, IPlayerMoveInfoProvider
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

    private bool sprinting;
    private bool tacticalSprinting;
    private float gait;
  
    public float GetSpeed(in MovementMode mode, bool isForward)
    {
        if (mode.prone) return proneSpeed;

        if (mode.crouch) return crouchSpeed;

        if (mode.sprint && isForward) return sprintSpeed;

        if (mode.tacticalSprint && isForward) return tacticalSprintSpeed;

        return walkSpeed;
    }

    public bool IsForward(Vector2 moveInfo, float dot = 0.65f)
    {
        //�Է� ������ ������ false => �������� �ʴ� ����
        if (moveInfo == Vector2.zero) return false;

        Vector2 wish = new Vector2
        (
            playerRoot.forward.x * moveInfo.y + playerRoot.right.x * moveInfo.x,
            playerRoot.forward.z * moveInfo.y + playerRoot.right.z * moveInfo.x
        );

        if (wish.sqrMagnitude < 1e-6f) return false;

        //ĳ���� ���� ���� ���� ��
        Vector2 fwd = new Vector2(playerRoot.forward.x, playerRoot.forward.z);
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
    public void CheckDesiredGait(Vector2 moveInfo, in MovementMode mode)
    {

        if (mode.tacticalSprint) gait = 3.0f;
        else if (mode.sprint) gait = 2.0f;
        else gait = moveInfo.magnitude;

    }
    public float GetDesiredGait()
    {
       // Debug.Log(gait);
        return gait;
    }
   
}
