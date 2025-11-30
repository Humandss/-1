using UnityEngine;


public class PooledObject : MonoBehaviour
{

    public void ReturnToPool()
    {
        //혹시 풀 없이 쓴 경우 대비
        if (PoolManager.Instance != null)
        {
            Debug.Log("풀로 복귀함");
            PoolManager.Instance.Return(gameObject);

        }
        else
        {
            Destroy(gameObject);
            Debug.Log("풀이 존재 X");

        }
    }


}
