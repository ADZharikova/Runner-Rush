using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Spawn
{
    public class RoadSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] _roadPrefabs;
        [SerializeField] private GameObject[] _asylumPrefabs;
        [SerializeField] private float _speed = 10;
        [SerializeField] private float _acceleration = 5f;
        [SerializeField] private float _maxSpeed = 100;
        [SerializeField] private Text _scoreText;
        [SerializeField] private float _roadLength = 100;
        [SerializeField] private int _maxRoads = 10;
        [SerializeField] private int _activeRoads = 5;
        private readonly List<GameObject> _roads = new();
        private GameObject _prev;
        private float _score;
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public List<GameObject> Roads => _roads;


        void Start()
        {
            float spawnPos = 0;
            if (_asylumPrefabs.Length > _activeRoads)
            {
                throw new Exception("Cannot have init prefabs more than active roads");
            }

            for (var i = 0; i < _maxRoads; i++)
            {
                if (i >= _activeRoads)
                {
                    var initializedRoad = Instantiate(_roadPrefabs[Random.Range(0, _roadPrefabs.Length)],
                        new Vector3(0, 0, 0), Quaternion.identity);
                    initializedRoad.SetActive(false);
                    _roads.Add(initializedRoad);
                }
                else if (i < _asylumPrefabs.Length)
                {
                    _prev = Instantiate(_asylumPrefabs[i],
                        transform.forward * spawnPos, Quaternion.identity);
                    _prev.SetActive(true);
                    spawnPos += _roadLength;
                }
                else
                {
                    // TODO: to replace with SpawnRoad call
                    var initializedRoad = Instantiate(_roadPrefabs[Random.Range(0, _roadPrefabs.Length)],
                        transform.forward * spawnPos, Quaternion.identity);
                    initializedRoad.SetActive(true);
                    // _coinsSpawner.GenerateCoins(_spawnPos - 50);
                    spawnPos += _roadLength;
                    _roads.Add(initializedRoad);
                }
            }
        }

        public void FixedUpdate()
        {
            if (Time.timeScale != 0)
            {
                _score += (float)_speed / 10;
                _scoreText.text = Math.Round(_score, 0).ToString(CultureInfo.InvariantCulture);
            }

            if (_speed < _maxSpeed)
            {
                _speed += _acceleration * Time.fixedDeltaTime;
            }
        }

        public void SpawnRoad(GameObject road)
        {
            _roads.Find(r => r.Equals(_prev))?.SetActive(false);
            _prev = road;
            var activationCandidates = _roads.FindAll(r => r.activeSelf == false);
            var roadToActivate = activationCandidates[Random.Range(0, activationCandidates.Count)];
            var position = road.transform.position;
            roadToActivate.transform.position = new Vector3(0, 0, _activeRoads * _roadLength + position.z);
            roadToActivate.SetActive(true);
        }
    }
}