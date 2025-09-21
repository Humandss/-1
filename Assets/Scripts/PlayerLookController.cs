using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerLookController : MonoBehaviour
{
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
    private float headYawTime; //헤드 yaw 시간
    private bool bodyYawControllable = true;

    private void Awake()
    {
        if (!yawRoot) yawRoot = transform;
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
        //카메라 로컬 포지션 값을 vector3로 받아온 다음 자세별 float 값을 y값에 대입
        Vector3 cameraPos = cameraRoot.localPosition;
        float speed = camChangeSpeed;
        float newPos = Mathf.SmoothDamp(cameraPos.y, cameraPosition, ref smoothTime, speed);
        Vector3 newCameraPosition = new Vector3(cameraPos.x, newPos, cameraPos.z);

        return newCameraPosition;
    }


}
