using KINEMATION.FPSAnimationPack.Scripts.Camera;
using KINEMATION.KAnimationCore.Runtime.Core;
using UnityEngine;

public interface ICameraAnimation
{
     void PlayCameraShake(FPSCameraShake newShake);
     void UpdateFOVandCameraShake();

}
public class PlayerLookController : MonoBehaviour,ICameraAnimation
{
    [Header("Refs")]
    private FPSCameraShake activeShake;
    //private PlayerAnimationController player;
    // private PlayerWeaponController playerWeaponController;
    private Player player;

    [Header("Camera")]
    private Vector3 cameraShake;
    private Vector3 cameraShakeTarget;
    private float cameraShakePlayback;
    private UnityEngine.Camera _camera;
    private float baseFov;

    [Header("Root")]
    [SerializeField] Transform cameraRoot;
    [SerializeField] Transform yawRoot;
 

    [Header("Limits")]
    [SerializeField] private float maxPitch = 65.0f;
    [SerializeField] private float minPitch = -65.0f;
    [SerializeField] private float freeLookYawLimit = 75.0f;

    [Header("FreeLookRecoverTime")]
    [SerializeField] private float freeLookRecoverTime = 0.25f;

    [Header("PlayerLookComponent")]
    private float pitch;
    private float bodyYaw;
    private float headYaw;
    private float smoothTime; 
    private float headYawTime; //��� yaw �ð�
    private bool bodyYawControllable = true;

   

    private IPlayerWeaponInfoProvider playerWeaponInfoProvider;
    private void Awake()
    {
        if (!yawRoot) yawRoot = transform;
        /*
        player = GetComponentInChildren<PlayerAnimationController>();
        if (player == null)
        {
            Debug.LogWarning("[PlayerCameraAnimation] player is NULL ");
        }

        playerWeaponController = GetComponentInChildren<PlayerWeaponController>();
        if (playerWeaponController == null)
        {
            Debug.LogWarning("[PlayerCameraAnimation] playerWeaponController is NULL ");
        }

        playerWeaponInfoProvider = playerWeaponController as IPlayerWeaponInfoProvider;
        if (playerWeaponInfoProvider == null)
        {
            Debug.LogWarning("[PlayerCameraAnimation] playerWeaponInfoProvider is NULL ");
        }*/
        player = GetComponentInChildren<Player> ();

        _camera = GetComponentInChildren<UnityEngine.Camera>();
        baseFov = _camera.fieldOfView;
    }
    private void OnEnable()
    {
        bodyYaw = yawRoot.eulerAngles.y;
        pitch = cameraRoot ? cameraRoot.localEulerAngles.x : 0;
        if (pitch > 180f) pitch -= 360f;
    }

    public void UpdateLook(Vector2 lookInfo, float rotationSpeed,float cameraPosition, 
                            float camChangeSpeed, float mouseSensitivity, bool isFreeLook)
    {
 
        float dx = lookInfo.x * rotationSpeed;
        float dy = -lookInfo.y * mouseSensitivity;

        pitch  = Mathf.Clamp(dy+pitch, minPitch, maxPitch);

        if(isFreeLook)
        {
            headYaw = Mathf.Clamp(headYaw+dx, -freeLookYawLimit, freeLookYawLimit);
        }
        else
        {
            if (bodyYawControllable) bodyYaw += dx;

            headYaw = Mathf.SmoothDamp(headYaw, 0.0f, ref headYawTime, freeLookRecoverTime);
        }

        Vector3 newCameraPos = UpdateCameraPosition(cameraRoot.localPosition, cameraPosition, camChangeSpeed);

        yawRoot.rotation = Quaternion.Euler(0.0f, bodyYaw, 0.0f);

        cameraRoot.localPosition= newCameraPos;
        cameraRoot.localRotation=Quaternion.Euler(pitch, headYaw, 0.0f);
        

    }
    private Vector3 UpdateCameraPosition(Vector3 pos,  float cameraPosition, float camChangeSpeed)
    {
        //ī�޶� ���� ������ ���� vector3�� �޾ƿ� ���� �ڼ��� float ���� y���� ����
        Vector3 cameraPos = cameraRoot.localPosition;
        float speed = camChangeSpeed;
        float newPos = Mathf.SmoothDamp(cameraPos.y, cameraPosition, ref smoothTime, speed);
        Vector3 newCameraPosition = new Vector3(cameraPos.x, newPos, cameraPos.z);

        return newCameraPosition;
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
    public virtual void PlayCameraShake(FPSCameraShake newShake)
    {
        if (newShake == null) return;

        activeShake = newShake;
        cameraShakePlayback = 0f;

        cameraShakeTarget.x = FPSCameraShake.GetTarget(activeShake.pitch);
        cameraShakeTarget.y = FPSCameraShake.GetTarget(activeShake.yaw);
        cameraShakeTarget.z = FPSCameraShake.GetTarget(activeShake.roll);
    }
    protected virtual void UpdateFOV()
    {
        if (_camera == null || player == null) return;

        //_camera.fieldOfView = Mathf.Lerp(baseFov,
        // playerWeaponInfoProvider.GetActiveWeapon().weaponSettings.aimFov, player.AdsWeight);

        _camera.fieldOfView = Mathf.Lerp(baseFov,
         player.GetActiveWeapon().weaponSettings.aimFov, player.AdsWeight);
    }
    

    public void UpdateFOVandCameraShake()
    {
        UpdateFOV();
        UpdateCameraShake();
    }

}
