using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {

    [System.Serializable]
    public class SaveInfo {
        public List<bool> isAlive;
    }

    public bool spawnOnStart;
    public GameObject leader;
    public List<GameObject> minions;
    [HideInInspector]
    public float radius;
    [HideInInspector]
    public int minionSpawnCount;
    [HideInInspector]
    public float minionOffsetRotation;

    [SerializeField, HideInInspector]
    int seed;
    List<Health> spawnedUnits;

    private void Start() {
        if (spawnOnStart) Spawn(null);
    }

    // Use this for initialization
    public void Spawn (SaveInfo info) {
        Random.InitState(seed);

        spawnedUnits = new List<Health>();
        

        if (leader != null) {
            if (info == null || info.isAlive[0]) {
                Transform lead = Instantiate(leader, transform.position, transform.rotation, transform.parent).transform;
                spawnedUnits.Add(lead.GetComponent<Health>());
                ManageFactions.instance.AddUnit(lead.GetComponent<Health>().faction, lead);

                if (info != null) {
                    info.isAlive.RemoveAt(0);
                }
            }
            else {
                spawnedUnits.Add(null);
            }
        }

        List<Vector3> minionSpawnLocations = GetMinionSpawnLocations();
        List<int> minionSpawnIndices = GetRandomMinionSpawns();
        for (int i = 0; i < minionSpawnCount; i++) {
            if (info == null || info.isAlive == null || info.isAlive.Count < i || info.isAlive[i]) {
                Transform minion = Instantiate(minions[minionSpawnIndices[i]], minionSpawnLocations[i], transform.rotation, transform.parent).transform;
                spawnedUnits.Add(minion.GetComponent<Health>());
                ManageFactions.instance.AddUnit(minion.GetComponent<Health>().faction, minion);
            }
            else {
                spawnedUnits.Add(null);
            }
        }
    }

    public List<Vector3> GetMinionSpawnLocations() {
        List<Vector3> spawnLocations = new List<Vector3>();

        for (float i = minionSpawnCount * -.5f + .5f; i < minionSpawnCount * .5f; i++) {
            Quaternion offsetRotation = Quaternion.AngleAxis(i * minionOffsetRotation, Vector3.up);
            spawnLocations.Add(transform.position + offsetRotation * transform.forward * radius);
        }

        return spawnLocations;
    }

    public List<int> GetRandomMinionSpawns() {
        Random.InitState(seed);

        List<int> minionSpawns = new List<int>();
        
        for (int i = 0; i < minionSpawnCount; i++) {
            minionSpawns.Add((int)(Random.value * minions.Count) % minionSpawnCount);
        }

        return minionSpawns;
    }

    public void RandomizeSeed() {
        seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);
    }

    public SaveInfo GetSaveInfo() {
        if (spawnedUnits != null && spawnedUnits.Count > 0) {
            SaveInfo info = new SaveInfo {
                isAlive = new List<bool>()
            };
            for (int i = 0; i < spawnedUnits.Count; i++) {
                info.isAlive.Add(spawnedUnits[i] != null && !spawnedUnits[i].IsDead());
            }
            return info;
        }
        return null;
    }
}
