﻿using Character;
using Photon.Pun;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Leaderboard;

public class LevelManager : MonoBehaviourPun
{
    public bool offlineMode;
    private bool ShowIfOffline() => offlineMode;
    [ShowIf("ShowIfOffline", true, true)]
    [SerializeField] private GameObject _myCamera;
    private LeaderboardManager _leaderboardMng;

    public void ArtificialAwake()
    {
        Debug.Log("Starting Level Manager");

        var user = PhotonNetwork.Instantiate("User",
            new Vector3(Random.Range(-2f, 2f), 1, Random.Range(-2f, 2f)), Quaternion.identity);
        user.GetComponentInChildren<CharacterModel>().name = PhotonNetwork.NickName;
        user.GetComponentInChildren<Character3DUI>().photonView.RPC("RPCUpdateNickname", RpcTarget.AllBuffered, PhotonNetwork.NickName);

        _leaderboardMng = new LeaderboardManager(this);

        if(photonView.IsMine)
            _leaderboardMng.table = FindObjectOfType<LeaderboardTable>();

        UpdateUserPoints(PhotonNetwork.NickName, 0);
    }

    public void UpdateUserPoints(string nickName, int addedPoints)
    { photonView.RPC("RPCUpdateUserPoints", RpcTarget.MasterClient, nickName, addedPoints); }

    [PunRPC]
    private void RPCUpdateUserPoints(string newNickname, int addedPoints)
    { _leaderboardMng.UpdateUserPoints(newNickname, addedPoints); }

    public void UpdateLeaderboardTables(string[] names, int[] points)
    { photonView.RPC("RPCUpdateLeaderboardTables", RpcTarget.AllBuffered, names, points); }

    [PunRPC]
    private void RPCUpdateLeaderboardTables(string[] names, int[] points)
    { _leaderboardMng.UpdateTableInfo(names, points); }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.R)) //debugear el top del ranking en orden de mayor a menor
            _leaderboardMng.DebugTopRanking();

        if (Input.GetKeyDown(KeyCode.N)) //agregar un nuevo "jugador" para el leaderboard
        {
            UpdateUserPoints("randomname" + Random.Range(0, 1000), Random.Range(0, 1000));
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) //shuffle a todo el leaderboard, cambian todos los puntos de todos
        {
            _leaderboardMng.ShufflePoints();
            _leaderboardMng.DebugTopRanking();
        }
    }

    private void Awake() //Al ser instanciado en realidad por el netmanager, no se va a llamar excepto q estemos testeando
    {
        PhotonNetwork.OfflineMode = offlineMode;

        if (PhotonNetwork.OfflineMode)
        {
            Instantiate(_myCamera, transform.position, transform.localRotation);
        }
        else
        {
            if (photonView.IsMine) return;
        }

        ArtificialAwake();

        _leaderboardMng.table = FindObjectOfType<LeaderboardTable>();
    }
}
