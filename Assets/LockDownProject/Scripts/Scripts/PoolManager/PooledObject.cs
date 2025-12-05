using UnityEngine;


public class PooledObject : MonoBehaviour
{
    public void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }


}
