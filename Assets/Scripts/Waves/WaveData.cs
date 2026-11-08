using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Combat/Wave Data")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public class SpawnGroup
    {
        public GameObject enemyPrefab;
        [Min(1)] public int count = 3;
    }

    public List<SpawnGroup> groups = new List<SpawnGroup>();
    [Tooltip("Segundos entre la aparición de cada enemigo")]
    public float spawnInterval = 0.6f;
    public bool isBossWave;

    public int TotalEnemies
    {
        get
        {
            int total = 0;
            foreach (SpawnGroup group in groups) total += group.count;
            return total;
        }
    }
}