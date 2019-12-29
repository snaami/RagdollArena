﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Realtime;
using Photon.Pun;
using Character;

public class Controller : MonoBehaviourPun
{
    private void Start()
    {
        if (!photonView.IsMine) return;

        Server.Instance.AddPlayer(photonView.Controller, LevelManager.Instance.SpawnUser());
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("<color=red>Desconectado de la partida actual</color>");
            PhotonNetwork.Disconnect();
        }
    }
}
