using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class WeaponData : ScriptableObject {

    public enum WeaponType {
        projectile, battery, energy
    }

    public WeaponType weaponType;
    public GameObject prefab;
    public Texture uiImage;
    public GameObject projectile;
    public Transform impactFX;
    public AudioClip shootClip;
    public AudioClip reloadClip;
    public float damage;
    public float range;
    [Tooltip("Used by AI to determine range")]
    public float effectiveRange;
    [Tooltip("RPM (Rounds Per Minute)")]
    public float rateOfFire;
    public float reload;
    public float switchTime;
    public float accuracy;
    public float recoil;
    public float volume;
    public float burstTime;
    public int shotsPerFire;
    public bool semiAuto;
    public bool sidearm;
    public bool magReload = true;
    [SerializeField, HideInInspector]
    public float reloadReadyTime;
    [SerializeField, HideInInspector]
    public int ammo;
    [SerializeField, HideInInspector]
    public float heatPerShot;
    [SerializeField, HideInInspector]
    public float heatDissipationRate;
    [SerializeField, HideInInspector]
    public int numCanisters;
    [SerializeField, HideInInspector]
    public float canisterReloadTime;
}
