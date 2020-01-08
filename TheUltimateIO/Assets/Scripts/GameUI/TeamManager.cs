﻿using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Realtime;
using Photon.Pun;
using System.Linq;

namespace GameUI
{
    public class TeamManager : MonoBehaviourPun
    {
        private List<TeamPanel> _allTeamPanels = new List<TeamPanel>();
        private List<Player> _allPlayers = new List<Player>();
        public Dictionary<int, int[]> _teamCombinations = new Dictionary<int, int[]>();
        public Dictionary<int, float> _teamCoresLife = new Dictionary<int, float>();
        private int playersAmount;
        [SerializeField] private TeamPanel _teamPanelPrefab;
        private Server _server;

        private void Awake()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _teamCombinations.Add(1, new int[] { 1, 1 }); //1 jugador, 1 panel de 1 persona
            _teamCombinations.Add(2, new int[] { 2, 1 }); //2 jugadores, 2 paneles de 1 persona
            _teamCombinations.Add(3, new int[] { 3, 1 }); //3 jugadores, 3 paneles de 1 persona
            _teamCombinations.Add(4, new int[] { 2, 2 }); //4 jugadores, 2 paneles de 2 personas
            _teamCombinations.Add(6, new int[] { 2, 3 }); //6 jugadores, 2 paneles de 3 personas
            _teamCombinations.Add(8, new int[] { 4, 2 }); //8 jugadores, 4 paneles de 2 personas
            _teamCombinations.Add(9, new int[] { 3, 3 }); //9 jugadores, 3 paneles de 3 personas
            _teamCombinations.Add(12, new int[] { 4, 3 }); //12 jugadores, 4 paneles de 3 personas
            _teamCombinations.Add(16, new int[] { 4, 4 }); //16 jugadores, 4 paneles de 4 personas
        }

        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (Input.GetKeyDown(KeyCode.U))
                AddPlayer(photonView.Controller);

            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus))
            {
                int randomTeamID = Random.Range(0, _allTeamPanels.Count);
                CoreLifeUpdate(randomTeamID, 0.2f);
            }
            else if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            {
                int randomTeamID = Random.Range(0, _allTeamPanels.Count);
                CoreLifeUpdate(randomTeamID, -0.2f);
            }
        }

        private void CoreLifeUpdate(int teamCoreID, float amount)
        {
            float resultLife = _teamCoresLife[teamCoreID] + amount;
            resultLife = Mathf.Clamp01(resultLife);
            _teamCoresLife[teamCoreID] = resultLife;
            photonView.RPC("RPCCoreLifeUpdate", RpcTarget.AllBuffered, teamCoreID, resultLife);

            if (resultLife <= 0)
            {
                //TODO: Destruir nexo, avisar a los jugadores en el chat y en popup, y matar a los jugadores de ese nexo, junto con muchisimo feedback
            }
        }
        [PunRPC] private void RPCCoreLifeUpdate(int teamCoreID, float newLife) => _allTeamPanels[teamCoreID].UpdateCoreLifebar(newLife);

        public void AddPlayer(Player newPlayer)
        {
            _allPlayers.Add(newPlayer);
            playersAmount++;

            int resultPlayersAmount = AnalyzeTeamOrganization(playersAmount);

            //crear paneles segun la estructura nueva
            photonView.RPC("CreatePanelsWithStructure", RpcTarget.All, _allPlayers.ToArray(), _teamCombinations[resultPlayersAmount][0], _teamCombinations[resultPlayersAmount][1]);
        }

        public int AnalyzeTeamOrganization(int actualPlayersAmount)
        {
            if (_teamCombinations.ContainsKey(actualPlayersAmount))
                return actualPlayersAmount;
            return AnalyzeTeamOrganization(actualPlayersAmount - 1);
        }

        [PunRPC]
        public void CreatePanelsWithStructure(Player[] allPlayers, int panelsAmount, int playersPerPanelAmount)
        {
            //destruir todos los paneles
            foreach (var x in _allTeamPanels)
                Destroy(x.gameObject);
            _allTeamPanels.Clear();
            _teamCoresLife.Clear();

            if (PhotonNetwork.IsMasterClient)
            {
                if (_server == null) _server = FindObjectOfType<Server>();
            }
            int actualNameID = 0;
            for (int i = 0; i < panelsAmount; i++)
            {
                var panel = Instantiate(_teamPanelPrefab, transform);

                panel.membersAmount = playersPerPanelAmount;
                panel.teamID = i;
                panel.UpdatePanel();

                for (int j = 0; j < playersPerPanelAmount; j++)
                {
                    actualNameID++;
                    if (PhotonNetwork.IsMasterClient)
                        _server.photonView.RPC("RPCChangePlayerTeam", RpcTarget.MasterClient, allPlayers[actualNameID - 1], i + 1);
                    panel.UpdateMemberData(j, allPlayers[actualNameID - 1].NickName);
                }

                _allTeamPanels.Add(panel);
                _teamCoresLife.Add(i, 1f);
            }

            FindObjectOfType<CoresMainPanel>().UpdateCorePanelStructure(panelsAmount);
        }
    }
}