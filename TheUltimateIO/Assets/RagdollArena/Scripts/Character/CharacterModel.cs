﻿using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Character
{
    public class CharacterModel : MonoBehaviourPun, IDrunk
    {
        private List<IUpdatable> _allUpdatables = new List<IUpdatable>();
        private List<IConstructable> _allConstructables = new List<IConstructable>();
        private CharacterMovement _movementController;
        public GameObject _ragdollCapsule;

        private LevelManager _lvlMng;
        public string nickname;
        public Rigidbody pelvisRb;

        public Animator anim;
        private Color _color;
        private Renderer[] _allMyRenderers;
        [Tooltip("Radio que va a tener el jugador para comprobar cosas como cuantos amigos tiene alrededor, etc")]
        public float contactRadius = 4f;
        public float speed = 60f;
        public float jumpSpeed = 200f;
        public float rotationSpeed = 2f;
        public float cameraSpeed = 0.6f;
        [Tooltip("Distancia que vamos a necesitar estar del piso para poder saltar.")]
        public float inAirDistance = 0.6f;
        public float minFOV;
        public float maxFOV;
        [Tooltip("Offset de la camara con respecto al character")]
        public Vector3 cameraOffset = new Vector3(-0.01f, 5.9f, -4f);
        [Tooltip("Mientras mas bajo, mas va a quedar en el MinFoV. Caso contrario, del MaxFoV.")]
        public float ratioMultiplierFoV;
        public float sqrMagnitudeInTimeSpeed;
        [Tooltip("Altura minima en la que el player debe volver")]
        public float heightRespawn = -3f;
        [Tooltip("Fuerza para lanzar cosas")]
        public float pushForce;
        [Tooltip("Velocidad de lanzamiento de granada")]
        public float grenadeThrowSpeed;
        private float _grenadeThrowSpeed;
        [Tooltip("Cuanto para arriba va a tirar las granadas")]
        public float grenadeYThrowSpeed;
        [Tooltip("Este va a ser la velocidad a la que va a subir el grenadethrowspeed cuando apretamos")]
        public float grenadeThrowSpeedThreshold;
        public LayerMask floorLayers;

        [Tooltip("Tiempo que dura la borrachera")]
        public float timeDrunk;
        public ParticleSystem particlesDrunk;
        bool _drunkActive;
        float _counterDrunk;
        public event Action<int> OnPointsUpdate = delegate { }; //se llama cada vez que ganamos o perdemos puntos
        public event Action OnJump = delegate { }; //se llama cada vez que saltamos
        public event Action<bool> OnCrowned = delegate { }; //se llama cuando agarramos la corona o perdemos la corona
        public Func<int> GetActiveModeValue; //Va a conseguir el valor importante del modo de juego actual (amigos, puntos, etc)

        public List<CharacterHands> hands = new List<CharacterHands>();

        [HideInInspector] public int team = 0; // { 0 } = sin equipo. { 1, 2, 3, 4 } = posibles equipos que pueden haber.
        [Tooltip("Este owned es parecido al photonView.isMine, solo que es para FullAutho, ya que el server es el photonView.isMine")] public bool owned;

        [PunRPC] public void RPCSetModelOwner(bool own) => owned = own;

        [PunRPC] public void RPCArtificialAwake()
        {
            _lvlMng = FindObjectOfType<LevelManager>();

            _allMyRenderers = GetComponentsInChildren<Renderer>();

            var characterView = new CharacterView(this);
            _allUpdatables.Add(characterView);
            _allConstructables.Add(characterView);

            if (!owned) return;
            Debug.Log("<color=green> Paso por aca awake </color>");

            var allChilds = GetComponentsInChildren<Transform>();
            allChilds.Select(x =>
            {
                x.gameObject.layer = Layers.PLAYER;
                _ragdollCapsule.layer = Layers.RAGDOLL;
                return x;
            }).ToList();

            anim = GetComponent<Animator>();

            _movementController = new CharacterMovement(this, pelvisRb, pelvisRb.transform.localRotation, floorLayers);
            _allConstructables.Add(_movementController);
            _allConstructables.Add(GetComponentInChildren<CharacterHands>());
            _allUpdatables.Add(GetComponentInChildren<CharacterHands>());
            _allUpdatables.Add(_movementController);
            _allUpdatables.Add(new CharacterCamera(this, pelvisRb));
            _allUpdatables.Add(new CharacterPointsManager(this, _lvlMng, PhotonNetwork.NickName));
            _allUpdatables.Add(new CharacterFriendsManager(this, _lvlMng.playerFriendsLayermask));

            var colorA = _allMyRenderers[1].material.GetColor("_ColorA");
            var colorB = _allMyRenderers[1].material.GetColor("_ColorB");
            var colorC = _allMyRenderers[1].material.GetColor("_ColorC");
            photonView.RPC("RPCUpdateColor", RpcTarget.AllBuffered,
                new float[] { colorA.r, colorA.g, colorA.b },
                new float[] { colorB.r, colorB.g, colorB.b },
                new float[] { colorC.r, colorC.g, colorC.b });

            ChangeTeam(0);

            ArtificialAwakes();
        }

        private void ChangeTeam(int newTeamID) //cambiar el team equivale tambien a cambiar el color del jugador y color de efectos
        {
            Debug.Log("<color=green> Fuiste cambiado al equipo " + newTeamID.ToString() + "</color>");
            team = newTeamID;

            Color previousColorA = _allMyRenderers[1].material.GetColor("_ColorA");
            Color previousColorC = _allMyRenderers[1].material.GetColor("_ColorC");
            Color newCol = newTeamID == 0 ? Color.grey : newTeamID == 1 ? Color.blue : Color.red;

            photonView.RPC("RPCUpdateColor", RpcTarget.AllBuffered,
                new float[] { previousColorA.r, previousColorA.g, previousColorA.b },
                new float[] { newCol.r, newCol.g, newCol.b },
                new float[] { previousColorC.r, previousColorC.g, previousColorC.b });
        }

        public void StartGame(int teamID, Vector3 spawnPos)
        {
            //TODO: primero aca hacer efecto de teletransportarse o lo que sea, junto con sonidos, etc.
            ChangeTeam(teamID);
            transform.position = spawnPos;
        }

        private void OnDrawGizmos()
        {
            if (pelvisRb != null)
                Gizmos.DrawWireSphere(pelvisRb.transform.position, contactRadius);
        }

        public void UpdatePoints(int addedPoints) => OnPointsUpdate(addedPoints);
        public void Crowned(bool on) => OnCrowned(on);
        public void TryJump() { if (_movementController.inAir) return; OnJump(); }
        private void Start() { if (!owned) return; ArtificialStart(); }
        private void Update() { if (!owned && pelvisRb != null) return; ArtificialUpdate(); }
        private void FixedUpdate() { if (!owned) return; ArtificialFixedUpdate(); }
        private void LateUpdate() { if (!owned) return; ArtificialLateUpdate(); }
            
        [PunRPC]
        public void RPCUpdateColor(float[] colorA, float[] colorB, float[] colorC)
        {
            _allMyRenderers.Select(x =>
            {
                x.material.SetColor("_ColorA", new Color(colorA[0], colorA[1], colorA[2]));
                x.material.SetColor("_ColorB", new Color(colorB[0], colorB[1], colorB[2]));
                x.material.SetColor("_ColorC", new Color(colorC[0], colorC[1], colorC[2]));
                return x;
            }).ToList();
        }
        public void ArtificialAwakes() { _allConstructables.Select(x => { x.ArtificialAwake(); return x; }).ToList(); }
        public void ArtificialStart() { _allConstructables.Select(x => { x.ArtificialStart(); return x; }).ToList(); }
        public void ArtificialUpdate() { _allUpdatables.Select(x => { x.ArtificialUpdate(); return x; }).ToList(); CheckHeight(); }
        public void ArtificialFixedUpdate() { _allUpdatables.Select(x => { x.ArtificialFixedUpdate(); return x; }).ToList(); }
        public void ArtificialLateUpdate() { _allUpdatables.Select(x => { x.ArtificialLateUpdate(); return x; }).ToList(); }

        void CheckHeight()
        {
            if (pelvisRb.transform.position.y < heightRespawn)
                _lvlMng.RespawnRandom(pelvisRb.transform);
        }

        public void AddPoint(int points)
        {
            _lvlMng.UpdateUserPoints(PhotonNetwork.NickName, points);
            var cubeSpawner = FindObjectOfType<CubeSpawner>();
            if(cubeSpawner != null)
                cubeSpawner.ConstructionPoints += points;
        }

        public bool OnClickPlayer()
        {
            foreach (var item in hands)
                if (item.activeTaken) 
                    return true;
            return false;
        }
        #region Grenade
        public void TryGrenade()
        {
            if (_throwGrenadeCoroutine != null)
                StopCoroutine(_throwGrenadeCoroutine);
            _throwGrenadeCoroutine = StartCoroutine(ThrowGrenadeCoroutine());
        }
        private Coroutine _throwGrenadeCoroutine;
        IEnumerator ThrowGrenadeCoroutine()
        {
            _grenadeThrowSpeed = 0;
            while (true)
            {
                _grenadeThrowSpeed = 
                    Mathf.Clamp(_grenadeThrowSpeed + Time.deltaTime * grenadeThrowSpeedThreshold ,0.1f,grenadeThrowSpeed);
                yield return new WaitForEndOfFrame();
            }
        }
        public void ThrowGrenade()
        {
            StopCoroutine(_throwGrenadeCoroutine);

            GameObject grenade = PhotonNetwork.Instantiate("Grenade",
                pelvisRb.transform.position + transform.forward * 2f, Quaternion.identity);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayers);
            grenade.GetComponent<Rigidbody>()
                .AddForce((hit.point - pelvisRb.transform.position + Vector3.up * grenadeYThrowSpeed).normalized
                * _grenadeThrowSpeed * Time.deltaTime, ForceMode.Impulse);
        }
        #endregion
        #region Drunk
        public void DrunkEffectActive()
        {
            if (!photonView.IsMine) return;

            if (!_drunkActive)
            {
                _drunkActive = true;
                StartCoroutine(Drunk());
            }
            else _counterDrunk = 0;
        }

        public void DrunkEffectDesactive()
        {
            if (!photonView.IsMine) return;

            _drunkActive = false;
            _counterDrunk = 0;
            photonView.RPC("ParticlesDrunk", RpcTarget.All, false);
            _movementController.ChangeControls(true);
        }

        IEnumerator Drunk()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            _movementController.ChangeControls(false);
            photonView.RPC("ParticlesDrunk", RpcTarget.All, true);
            while (_drunkActive)
            {
                _counterDrunk += Time.deltaTime;
                if (_counterDrunk >= timeDrunk)
                    _drunkActive = false;

                yield return waitForEndOfFrame;
            }
            DrunkEffectDesactive();
        }

        [PunRPC]
        void ParticlesDrunk(bool active)
        {
            if(active) particlesDrunk.Play();
            else particlesDrunk.Stop();
        }

        public void TryGrenadeDrunk()
        {
            GameObject grenadeDrunk = PhotonNetwork.Instantiate("GrenadeDrunk",
                pelvisRb.transform.position + (transform.forward * 2f) + (transform.up * 3f), Quaternion.identity);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayers);
            grenadeDrunk.GetComponent<Rigidbody>()
                .AddForce((hit.point - pelvisRb.transform.position).normalized
                * 20, ForceMode.Impulse);
        }
        #endregion
    }
}
