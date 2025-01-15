using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;
using Spawn;

namespace Destructible
{
    public class PlayerController : Destructible
    {
        [SerializeField] private GameObject _lossPanel;
        [SerializeField] private GameObject _continuePanel;
        [SerializeField] private SpawnController _spawnController;
        [SerializeField] private Text _coinsText;
        [SerializeField] private float _gravity;
        [SerializeField] private float _lineDistanse = 3;
        [SerializeField] private int _coins;
        [SerializeField] private float _timeHit = 10f;
        [SerializeField] private float _timeShield = 10f;
        [SerializeField] private float _timeShooting = 10f;
        [SerializeField] private int _live;
        [SerializeField] private Transform bulletSpawnPosition;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] float bulletSpeed = 10;
        private RoadSpawner _roadSpawner;
        private Animator _animator;
        private CharacterController _characterController;
        private SkyController _skyController;
        private Vector3 _dir;
        private int _lineToMove = 1;
        private bool _highJump;
        private bool _isHit;
        private bool _isShield;
        private static readonly int IsHit = Animator.StringToHash("isHit");
        private static readonly int StartHited = Animator.StringToHash("startHited");
        private static readonly int StartShielded = Animator.StringToHash("startShielded");
        private static readonly int IsShielded = Animator.StringToHash("isShielded");
        private float _airDensity = 1.225f;
        private float _dragCoefficient = 1.1f;
        private float _crossSectionalArea = 0.5f;
        private float _mass = 60;
        private float _cachedSpeed;
        private Vector2 _touchStartPos;
        private Vector2 _touchEndPos;
        private AudioSource _jumpAudioSource;

        void Start()
        {
            _lossPanel.SetActive(false);
            Time.timeScale = 1;
            _characterController = GetComponent<CharacterController>();
            _skyController = GetComponent<SkyController>();
            _animator = GetComponent<Animator>();
            _spawnController = GameObject.Find("SpawnController").GetComponent<SpawnController>();
            _roadSpawner = GameObject.Find("RoadSpawner").GetComponent<RoadSpawner>();
            _cachedSpeed = _roadSpawner.Speed;
            _jumpAudioSource = GameObject.Find("JumpAudioSource").GetComponent<AudioSource>();
        }

        public override void OnChildTriggerEnter(Collider other, ChildTrigger childTrigger)
        {
            throw new NotImplementedException();
        }

        private void Update()
        {
            if (_lossPanel.activeSelf || _continuePanel.activeSelf) return;

            switch (SwipeController.CurrentSwipe, _lineToMove, _characterController.isGrounded, _highJump)
            {
                case (SwipeController.Swipe.SwipeLeft, > 0, _, _):
                    _lineToMove--;
                    break;
                case (SwipeController.Swipe.SwipeRight, < 2, _, _):
                    _lineToMove++;
                    break;
                case (SwipeController.Swipe.SwipeUp, _, true, _):
                    _highJump = true;
                    Jump(15);
                    break;
                case (SwipeController.Swipe.SwipeUp, _, _, true):
                    _highJump = false;
                    Jump(10);
                    break;
                default:
                    if (Input.touchCount > 0 && _characterController.isGrounded && _roadSpawner.Speed >= _cachedSpeed)
                    {
                        Jump(SwipeController.SwipeValue <= 500 ? 15 : 20);
                    }
                    break;
            }

            Vector3 targetPosition = transform.position.z * transform.forward + transform.position.y * transform.up;
            if (_lineToMove == 0)
            {
                targetPosition += Vector3.left * _lineDistanse;
            }
            else if (_lineToMove == 2)
            {
                targetPosition += Vector3.right * _lineDistanse;
            }

            if (transform.position == targetPosition)
            {
                return;
            }

            Vector3 dif = targetPosition - transform.position;
            Vector3 moveDir = dif.normalized * 25 * Time.deltaTime;
            _characterController.Move(moveDir.sqrMagnitude > dif.sqrMagnitude ? moveDir : dif);
        }

        void FixedUpdate()
        {
            if (!_characterController.isGrounded)
            {
                var accelerationDueToDrag = 0.5f * _airDensity * _roadSpawner.Speed * _roadSpawner.Speed *
                    _dragCoefficient * _crossSectionalArea / _mass;
                _roadSpawner.Speed -= accelerationDueToDrag * Time.fixedDeltaTime;
            }
            else if (_roadSpawner.Speed < _cachedSpeed)
            {
                _roadSpawner.Speed++;
            }

            _dir.y += _gravity * Time.fixedDeltaTime;
            _characterController.Move(_dir * Time.fixedDeltaTime);
        }

        private void Jump(float jumpForce)
        {
            _jumpAudioSource.Play();
            _dir.y = jumpForce;
        }

        private void OnTriggerEnter(Collider other)
        {
            switch (other.gameObject.tag, _isShield, _live, _isHit)
            {
                case ("Respawn", _, _, _):
                    _spawnController.GenerateNext(other);
                    break;

                case ("Died", false, > 0, _):
                    StartCoroutine(ActivatePanel(_continuePanel));
                    _live--;
                    break;
                
                case ("Died", false, _, _):
                    StartCoroutine(ActivatePanel(_lossPanel));
                    break;

                case ("Hit", false, _, false): 
                    _isHit = true;
                    StartCoroutine(Hit(_timeHit));
                    break;
                
                case ("Hit", false, > 0, true):
                    Time.timeScale = 0;
                    _continuePanel.SetActive(true);
                    _live--;
                    _isHit = false;
                    break;
                
                case ("Hit", false, 0, true):
                    _lossPanel.SetActive(true);
                    Time.timeScale = 0;
                    _isHit = false;
                    break;

                case ("Coin", _, _, _):
                    _coins++;
                    _coinsText.text = _coins.ToString();
                    other.gameObject.SetActive(false);
                    break;

                case ("Shield", _, _, _):
                    other.gameObject.SetActive(false);
                    _isShield = true;
                    StartCoroutine(Shielded(_timeShield));
                    break;

                case ("Shooting", _, _, _):
                    other.gameObject.SetActive(false);
                    StartCoroutine(Shooting(_timeShooting));
                    break;
            }
        }

        private IEnumerator ActivatePanel(GameObject panel)
        {
            yield return new WaitForSeconds(0.3f);
            panel.SetActive(true);
            Time.timeScale = 0;
        }

        private void OnCollisionEnter(Collision collision)
        {
            switch (collision.gameObject.tag)
            {
                case "Block":
                    if (_isShield)
                    {
                        collision.gameObject.SetActive(false);
                    }

                    break;
            }
        }

        IEnumerator Hit(float time)
        {
            _animator.SetTrigger(StartHited);
            _skyController.SetBloodySky();

            yield return new WaitForSeconds(time - 2f);

            _animator.SetTrigger(IsHit);

            yield return new WaitForSeconds(2f);
            _isHit = false;
            _skyController.SetNormalSky();
        }

        IEnumerator Shielded(float time)
        {
            _animator.SetTrigger(StartShielded);

            yield return new WaitForSeconds(time - 2f);
            _animator.SetTrigger(IsShielded);

            yield return new WaitForSeconds(2f);
            _isShield = false;
        }

        IEnumerator Shooting(float time)
        {
            while (time > 0)
            {
                var position = bulletSpawnPosition.position;
                var bullet = Instantiate(bulletPrefab, position, bulletSpawnPosition.rotation);
                bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPosition.forward * bulletSpeed;
                time -= 0.7f;
                yield return new WaitForSeconds(0.7f);
            }
        }
    }
}