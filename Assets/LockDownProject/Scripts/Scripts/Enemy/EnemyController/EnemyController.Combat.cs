using System.Collections;
using UnityEngine;

//EnemyController.Combat
public partial class EnemyController
{
    private Coroutine burstRoutine;
    public void OnFirePressed(Vector3 bulletPos)
    {

        if (weapon == null) return;

        this.bulletPos = bulletPos;


        weapon.EnemyFirePressed();

    }

    public void IsEnemyAim(bool isAiming)
    {
        if (hasAimSfxState && lastAimSfxState == isAiming) return;

        enemySound.PlayAimSound(isAiming);
        hasAimSfxState = true;
        lastAimSfxState = isAiming;
    }
    public void ChangeFireOptionsByPlayerDistance()
    {
        float distance = GetVectorBetweenPlayerAndEnemy().magnitude;

        dx = horizontalOffset;
        dy = verticalOffset;

        if (distance <= detectionRange && distance > detectionRange * 0.8f)
        {
            fireRate = 2.0f;
            dx *= 1.5f;
            dy *= 1.5f;

        }

        else if (distance <= detectionRange * 0.8f && distance > detectionRange * 0.6f)
        {
            fireRate = 1.5f;
            dx *= 1.2f;
            dy *= 1.2f;

        }

        else if (distance <= detectionRange * 0.6f && distance > detectionRange * 0.3f)
        {
            fireRate = 1.0f;
            dx *= 1.0f;
            dy *= 1.0f;

        }

        else if (distance <= detectionRange * 0.3f && distance > detectionRange * 0.0f)
        {
            fireRate = 0.75f;
            dx *= 0.8f;
            dy *= 0.8f;
        }
    }

    public void TryStartBurst(Vector3 bulletPos)
    {
        if (burstRoutine != null) return;

        burstRoutine = StartCoroutine(BurstRoutine(bulletPos));
    }

    private IEnumerator BurstRoutine(Vector3 bulletPos)
    {
        int shots = Mathf.Min(3, GetEnemyAmmo());

        for (int i = 0; i < shots; i++)
        {
            OnFirePressed(bulletPos);
            yield return new WaitForSeconds(burstShotInterval);
        }

        yield return new WaitForSeconds(fireInterval);
        burstRoutine = null;
    }

    public void ReloadAmmo()
    {
        if (weapon == null) return;

        weapon.EnemyReload();
    }
}
