using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialShoot : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private float shootTick;
    private WeaponFireController weaponFireController;
    private float nextTick;

    private void Awake()
    {
        weapon.TutorialDummyInitialize(gameObject);
    }
    private void Update()
    {
        if(Time.time > nextTick)
        {
            nextTick = Time.time + shootTick;
            weapon.TutorialDummyPressed();
        }
        
    }
}
