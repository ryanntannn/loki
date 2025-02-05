﻿using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

//for photon networking
public class NetworkingStuff : MonoBehaviour, IPunCallbacks {
    [Header("Room stuff")]
    public InputField inCreateRoom;
    public InputField inJoinRoom;
    public InputField inCreateRoomPassword;
    public InputField inJoinRoomPassword;
    public Image masterPlayerIcon;

    [Header("Gameplay")]
    public GameObject goToPlayGameScene;

    // Start is called before the first frame update
    void Start() {
        PrintToConsole("Player connection state: " + PhotonNetwork.connectionState);
        LobbyUIManager.Instance.UpdateRooms();

        if (PhotonNetwork.room != null) {
            OnJoinedRoom();
        }
    }

    // Update is called once per frame
    void Update() {
        goToPlayGameScene.SetActive(PlayfabUserInfo.CurrentUserState == PlayfabUserInfo.UserState.ReadyToType);
        PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() {
            {"PlayerState", PlayfabUserInfo.CurrentUserState }
        });

        if (PlayfabUserInfo.CurrentUserState == PlayfabUserInfo.UserState.WaitingForOpponent) {
            StartGame();
        }
    }

    private void StartGame() {
        //if ((PlayfabUserInfo.UserState)m_opponent.CustomProperties["PlayerState"] == PlayfabUserInfo.UserState.WaitingForOpponent) {
        if (PhotonNetwork.otherPlayers.All(x => { return (PlayfabUserInfo.UserState)x.CustomProperties["PlayerState"] == PlayfabUserInfo.UserState.WaitingForOpponent; })) {
            if (PhotonNetwork.isMasterClient) {
                Paragraph para = GetProse.Instance.GetRandomProse();

                //set opponents
                foreach (PhotonPlayer player in PhotonNetwork.otherPlayers) {
                    player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() {
                    { "Score", 0 },
                    {"PlayerState", PlayfabUserInfo.UserState.InMatch },
                });
                }

                //setself
                PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() {
                    { "Score", 0 },
                    {"PlayerState", PlayfabUserInfo.UserState.InMatch },
                    { "ProseToWrite", para.Prose }
                });

                //set room
                PhotonNetwork.room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() {
                        { "Paragraph", para.Prose },
                        { "Round number", 0 },
                        { "ReadyToStart", true },
                        { "ProsesUsed", para.Prose }
                });

                //change scene
                GetComponent<SceneChanger>().ChangeScene(5);
            } else {
                //wait for readytostart
                if ((bool)PhotonNetwork.room.CustomProperties["ReadyToStart"]) {
                    //change scene
                    GetComponent<SceneChanger>().ChangeScene(5);
                }
            }
        }
    }

    public void WhenStartGame() {
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.WaitingForOpponent);
    }

    private void PrintToConsole(string _message) {
        try {
            GameplayConsole.Log(_message);
        } catch (Exception) { }
    }

    public void WhenCreateRoom() {
        //check inCreate for text
        if (inCreateRoom.text.Equals("")) {
            //keep polling to make sure it creates a room
            while (!PhotonNetwork.CreateRoom(Helper.GenerateRandomWord(), new RoomOptions() {
                MaxPlayers = 8, IsVisible = true, IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() {
                    { "ReadyToStart", false },
                    { "Password", inCreateRoomPassword.text }
                },
                CustomRoomPropertiesForLobby = new string[] { "Password" }
            }, TypedLobby.Default)) ;
        } else {
            if (!PhotonNetwork.CreateRoom(inCreateRoom.text, new RoomOptions() {
                MaxPlayers = 8, IsVisible = true, IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() {
                    { "ReadyToStart", false },
                    { "Password", inCreateRoomPassword.text }
                },
                CustomRoomPropertiesForLobby = new string[] { "Password" }
            }, TypedLobby.Default)) {
                PopupManager.Instance.ShowPopUp("Room already exists, join it instead?", 5.0f);
            } else {
                PopupManager.Instance.ShowPopUp(string.Format("Room {0} successfully created!", PhotonNetwork.room.Name), 3.0f);
            }
        }
    }

    public void WhenJoinRoom() {
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InQueue);
        if (inJoinRoom.text.Equals("")) {
            PhotonNetwork.JoinRandomRoom();
        } else {
            PhotonNetwork.JoinRoom(inJoinRoom.text);
        }
    }

    public void WhenLeaveRoom() {
        if (PhotonNetwork.room != null) {
            PhotonNetwork.LeaveRoom();
        }

        GetComponent<SceneChanger>().ChangeScene(1);
    }

    #region Photon callbacks
    public void OnConnectedToPhoton() {

    }

    public void OnLeftRoom() {
        PrintToConsole("Left room");
        PlayfabUserInfo.UpdatePlayerRoom("NotInRoom");
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
        LobbyUIManager.Instance.LeftRoom();
    }

    public void OnMasterClientSwitched(PhotonPlayer newMasterClient) {

    }

    public void OnPhotonCreateRoomFailed(object[] codeAndMsg) {
        PrintToConsole("Creation of room failed");
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
    }

    public void OnPhotonJoinRoomFailed(object[] codeAndMsg) {
        PrintToConsole("Join room failed");
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
    }

    public void OnCreatedRoom() {
        PopupManager.Instance.ShowPopUp(string.Format("Room {0} successfully created!", PhotonNetwork.room.Name), 3.0f);
        PrintToConsole("Creation of room succeeded");
        PrintToConsole("Password: " + inCreateRoomPassword.text);
        PrintToConsole(string.Format("Currently in room: {0}", PhotonNetwork.room.Name));
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InQueue);
    }

    public void OnJoinedLobby() {

    }

    public void OnLeftLobby() {

    }

    public void OnFailedToConnectToPhoton(DisconnectCause cause) {
        PrintToConsole(string.Format("Failed to connect to photon: {0}", cause));
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
    }

    public void OnConnectionFail(DisconnectCause cause) {
        PrintToConsole(string.Format("Failed to connect: {0}", cause));
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
    }

    public void OnDisconnectedFromPhoton() {
        PrintToConsole("Disconnected from photon");
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.Offline);
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {

    }

    public void OnReceivedRoomListUpdate() {
        PrintToConsole("Room list updated");
        LobbyUIManager.Instance.UpdateRooms();
    }

    public void OnJoinedRoom() {
        if (PhotonNetwork.room != null) {
            PrintToConsole("Player is in room: " + PhotonNetwork.room.Name);
            PlayfabUserInfo.SetUserState(PhotonNetwork.room.PlayerCount == 1 ? PlayfabUserInfo.UserState.InQueue : PlayfabUserInfo.UserState.ReadyToType);

            PlayfabUserInfo.UpdatePlayerRoom(PhotonNetwork.room.Name);
            LobbyUIManager.Instance.JoinedRoom();
        } else {
            PrintToConsole("Failed to create/join room");
            PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
        }
    }

    public void OnPhotonPlayerConnected(PhotonPlayer newPlayer) {
        if (PhotonNetwork.room != null) {
            PrintToConsole("Player joined room: " + newPlayer.NickName);
            PlayfabUserInfo.SetUserState(PhotonNetwork.room.PlayerCount == 1 ? PlayfabUserInfo.UserState.InQueue : PlayfabUserInfo.UserState.ReadyToType);

            LobbyUIManager.Instance.UpdateDetailedRoomInfo();
        } else {
            PrintToConsole("Failed to create/join room");
            PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
        }
    }

    public void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer) {
        if (PhotonNetwork.room != null) {
            PrintToConsole("Player left room: " + otherPlayer.NickName);
            PlayfabUserInfo.SetUserState(PhotonNetwork.room.PlayerCount == 1 ? PlayfabUserInfo.UserState.InQueue : PlayfabUserInfo.UserState.ReadyToType);

            LobbyUIManager.Instance.UpdateDetailedRoomInfo();
        } else {
            PrintToConsole("Failed to create/join room");
            PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
        }
    }

    public void OnPhotonRandomJoinFailed(object[] codeAndMsg) {
        PrintToConsole("Failed to join random room");
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
    }

    public void OnConnectedToMaster() {

    }

    public void OnPhotonMaxCccuReached() {
        PlayfabUserInfo.SetUserState(PlayfabUserInfo.UserState.InLobby);
        Debug.LogError("PHOTON MAX PLAYERS REACHED!!");
        Application.Quit();
    }

    public void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged) {

    }

    public void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps) {

    }

    public void OnUpdatedFriendList() {

    }

    public void OnCustomAuthenticationFailed(string debugMessage) {

    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) {

    }

    public void OnWebRpcResponse(OperationResponse response) {

    }

    public void OnOwnershipRequest(object[] viewAndPlayer) {

    }

    public void OnLobbyStatisticsUpdate() {

    }

    public void OnPhotonPlayerActivityChanged(PhotonPlayer otherPlayer) {

    }

    public void OnOwnershipTransfered(object[] viewAndPlayers) {

    }
    #endregion
}
