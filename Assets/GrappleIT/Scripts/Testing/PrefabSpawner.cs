using System;
using System.Timers;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private float _spawnTime = 2f;
    [SerializeField] private GameObject _prefabToSpawn;
    [SerializeField] private Transform _spawnTransform;
    private float _spawnTimer = 0f;
    private GameObject _spawnParent;
    private void OnValidate()
    {
        Assert.AreNotEqual(null, _prefabToSpawn, $"{nameof(_prefabToSpawn)}, prefab to spawn not set!");
        Assert.AreNotEqual(null, _spawnTransform, $"{nameof(_prefabToSpawn)}, spawn transform not set!");

    }

    private void Awake()
    {
        _spawnParent = new GameObject("Spawned Objects");
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer > _spawnTime)
        {
            _spawnTimer = 0;
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        Instantiate(_prefabToSpawn, _spawnTransform.position, _spawnTransform.rotation,_spawnParent.transform);
    }
    
    
}
