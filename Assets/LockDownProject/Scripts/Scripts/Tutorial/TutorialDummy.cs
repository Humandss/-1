using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDummy : MonoBehaviour
{
    [SerializeField] private HealthManager healthManager;


    private void Awake()
    {
        healthManager = GetComponent<HealthManager>();
    }

    private void Update()
    {
        bool isDie = healthManager.CheckIsDead();
        if (isDie) Destroy(gameObject);
    }
}
