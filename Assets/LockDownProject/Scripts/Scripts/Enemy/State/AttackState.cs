using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    [Header("Refs")]
    private EnemyController enemy;
    private EnemyStateMachine fsm;
    private WeaponFireController weaponFire;

    private Quaternion startRot;
    private Quaternion targetRot;

    public AttackState(EnemyController enemy, EnemyStateMachine fsm)
    {
        this.enemy = enemy;
        this.fsm = fsm;
    }

    public void Enter()
    {
        startRot = enemy.transform.rotation;
        //목표 각도 세팅
        //targetRot = Quaternion.AngleAxis(turnAngle, Vector3.up) * startRot;
    }
    public void Execute()
    {

    }

    public void Exit()
    {

    }
}
