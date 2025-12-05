using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IFireBulletProvider
{
    void FireBullet(bool isPlayerShot);

}

public class WeaponFireController : MonoBehaviour, IFireBulletProvider
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private BallisticProjectile projectile;
    private Vector3 dir;

    private EnemyController enemyController;
    private IGetBulletDirection getBulletDirection;
    private void Initialize(bool isPlayerShot)
    {
        if(!isPlayerShot)
        {
            enemyController = GetComponentInParent<EnemyController>();
            if(enemyController == null)
            {
                Debug.LogWarning("[WeaponFireController] enemyController is NULL");
            }
               
            getBulletDirection = enemyController as IGetBulletDirection;
            if (getBulletDirection == null)
            {
                Debug.LogWarning("[WeaponFireController] getBulletDirection is NULL");
            }
        }    
        if(muzzle == null)
        {
            Debug.LogWarning("[WeaponFireController] muzzle is NULL");
        }
        if(projectile == null)
        {
            Debug.LogWarning("[WeaponFireController] projectile is NULL");
        }
  
 
        //총 발사 방향 세팅
        if (isPlayerShot)
        {
            dir = muzzle.forward.normalized;
        }
        else
        {
            Vector3 baseDir = getBulletDirection.GetBulletDirection();
            //Debug.Log($"[WFC] baseDir = {baseDir}");
            float dist = getBulletDirection.GetVectorBetweenPlayerAndEnemy().magnitude;

            float h = Random.Range(-getBulletDirection.GetHoriontalOffset(), getBulletDirection.GetHoriontalOffset());
            float v = Random.Range(-getBulletDirection.GetVerticalOffset(), getBulletDirection.GetVerticalOffset());
            
            Quaternion spreadRot = Quaternion.AngleAxis(h, muzzle.right) * Quaternion.AngleAxis(v, muzzle.up);
           // Debug.Log($"sR= {spreadRot}");

            dir = (spreadRot * baseDir).normalized;

           // Debug.Log($"right={muzzle.right}, up={muzzle.up}");
           // Debug.Log($"h={h}, v={v}");
            // Debug.Log($"sR={spreadRot}");
            // Debug.Log($"dir={dir}, sqr={dir.sqrMagnitude}");

            // dir 값이 너무 작을 경우 -> 랜덤 앵글 지정하지 말기
            if (dir.sqrMagnitude < 0.0001f ||
                float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z))
            {
               // Debug.Log($" dir.sqr = {dir.sqrMagnitude}, dirx={dir.x}, diry={dir.y}, dirz={dir.z}");
                Debug.LogWarning("dir is vector zero");
                dir = muzzle.forward;
            }
            

            /*
            dir = (baseDir + muzzle.right * h + muzzle.up * v ).normalized;

               if (dir.sqrMagnitude < 0.0001f ||
                   float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z))
               {
                   Debug.LogWarning($"[WeaponFireController] final dir invalid ({dir}), fallback to baseDir", this);
                   dir = baseDir;
               }*/
        }

        //총알 스폰 장소 세팅
       Vector3 spawnPos = muzzle.position + dir * 0.01f;

        GameObject bulletObj = PoolManager.Instance.Spawn(projectile.gameObject,spawnPos,Quaternion.LookRotation(dir));
        if (bulletObj == null)
        {
            Debug.LogWarning("[WeaponFireController] bulletObj is NULL from PoolManager");
            return;
        }

        var bullet = bulletObj.GetComponent<BallisticProjectile>();
        if (bullet == null)
        {
            Debug.LogWarning("[WeaponFireController] Bullet component is NULL on spawned object");
            return;
        }
        bullet.Initialize(spawnPos, dir ,isPlayerShot);
        SpawnMuzzleFlash();
    }
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlash == null || muzzle == null) return;

        PoolManager.Instance.Spawn(muzzleFlash, muzzle.position, Quaternion.LookRotation(dir));

    }
    public void FireBullet(bool isPlayerShot)
    {
         Initialize(isPlayerShot);
    }
    void OnDrawGizmosSelected()
    {
        if (muzzle) { Gizmos.color = Color.yellow; Gizmos.DrawRay(muzzle.position, muzzle.forward * 0.5f); }
    }

    public string GetAmmoName()
    {
        return projectile.name;
    }
   
}
