using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ManageFactions : MonoBehaviour {

    [Serializable]
    public enum Faction {
        green,
        purple,
        security
    }

    public static ManageFactions instance;
    public Transform player;
    public List<Transform> startingUnits;
    public LayerMask deployableShield;

    Dictionary<Faction, List<Transform>> factions;

    private void Awake() {
        instance = this;

        factions = new Dictionary<Faction, List<Transform>>();

        factions[Faction.green] = new List<Transform>();
        factions[Faction.purple] = new List<Transform>();
        factions[Faction.security] = new List<Transform>();
        if (startingUnits != null) {
            foreach (Transform unit in startingUnits) {
                if (unit != null) {
                    if (unit.GetComponent<Health>() != null) {
                        factions[unit.GetComponent<Health>().faction].Add(unit);
                    }
                    else {
                        foreach (Health unitGroup in unit.GetComponentsInChildren<Health>()) {
                            factions[unitGroup.faction].Add(unitGroup.transform);
                        }
                    }
                }
            }
            if (startingUnits.Count > 0)
                factions[Faction.green].Add(player);
        }
    }

    // Use this for initialization
    public void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public List<Transform> GetAllyFactionList(Faction f) {
        List<Transform> aliveUnits = new List<Transform>();

        if (f == Faction.green && player != null && !player.GetComponent<Health>().IsDead()) {
            aliveUnits.Add(player);
        }

        foreach (Transform unit in factions[f]) {
            if (unit != null && unit.gameObject.activeSelf && !unit.GetComponent<Health>().IsDead()) {
                aliveUnits.Add(unit);
            }
        }

        return aliveUnits;
    }

    public List<Transform> GetEnemyFactionList(Faction f) {
        List<Transform> newList = new List<Transform>();
        foreach (Faction k in factions.Keys) {
            if (f != k) newList.AddRange(GetAllyFactionList(k));
        }
        return newList;
    }

    public void RemoveUnit(Faction f, Transform t) {
        factions[f].Remove(t);
    }

    public void AddUnit(Faction f, Transform t) {
        factions[f].Add(t);
    }
}
