using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.KAnimationCore.Runtime.Core;
using UnityEngine;

public class PlayerCameraAnimation : MonoBehaviour
{
    [SerializeField] private Transform cameraBone;

    private FPSCameraShake activeShake;
    private Vector3 cameraShake;
    private Vector3 cameraShakeTarget;
    private float cameraShakePlayback;

    private UnityEngine.Camera _camera;
    private PlayerAnimationController player;
    private PlayerWeaponController playerWeaponController;
    private float baseFov;

    private IPlayerWeaponInfoProvider playerWeaponInfoProvider;

    public virtual void PlayCameraShake(FPSCameraShake newShake)
    {
        if (newShake == null) return;

        activeShake = newShake;
        cameraShakePlayback = 0f;

        cameraShakeTarget.x = FPSCameraShake.GetTarget(activeShake.pitch);
        cameraShakeTarget.y = FPSCameraShake.GetTarget(activeShake.yaw);
        cameraShakeTarget.z = FPSCameraShake.GetTarget(activeShake.roll);
    }

    protected virtual void UpdateCameraShake()
    {
        if (activeShake == null) return;

        float length = activeShake.shakeCurve.GetCurveLength();
        cameraShakePlayback += Time.deltaTime * activeShake.playRate;
        cameraShakePlayback = Mathf.Clamp(cameraShakePlayback, 0f, length);

        float alpha = KMath.ExpDecayAlpha(activeShake.smoothSpeed, Time.deltaTime);
        if (!KAnimationMath.IsWeightRelevant(activeShake.smoothSpeed))
        {
            alpha = 1f;
        }

        Vector3 target = activeShake.shakeCurve.GetValue(cameraShakePlayback);
        target.x *= cameraShakeTarget.x;
        target.y *= cameraShakeTarget.y;
        target.z *= cameraShakeTarget.z;

        cameraShake = Vector3.Lerp(cameraShake, target, alpha);
        transform.rotation *= Quaternion.Euler(cameraShake);
    }

    protected virtual void UpdateFOV()
    {
        if (_camera == null || player == null) return;

        _camera.fieldOfView = Mathf.Lerp(baseFov,
           playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.aimFov, player.AdsWeight);
    }

    private void Awake()
    {
        player = transform.root.GetComponentInChildren<PlayerAnimationController>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerCameraAnimation] player is NULL ");
        }

        playerWeaponController = transform.root.GetComponentInChildren<PlayerWeaponController>();
        if(playerWeaponController == null )
        {
            Debug.LogWarning("[PlayerCameraAnimation] playerWeaponController is NULL ");
        }

        playerWeaponInfoProvider = playerWeaponController as IPlayerWeaponInfoProvider;
        if (playerWeaponInfoProvider == null)
        {
            Debug.LogWarning("[PlayerCameraAnimation] playerWeaponInfoProvider is NULL ");
        }

        _camera = GetComponent<UnityEngine.Camera>();
        baseFov = _camera.fieldOfView;
    }

    private void LateUpdate()
    {
        transform.localRotation = player.transform.localRotation * cameraBone.localRotation;
        UpdateCameraShake();
        UpdateFOV();
    }
}
