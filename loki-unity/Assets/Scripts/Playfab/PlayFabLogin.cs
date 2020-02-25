﻿using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class PlayFabLogin : MonoBehaviour {
    public InputField inEmail, inPass;
    public Toggle rememberMe;

    private string m_userEmail;
    public string UserEmail { set { m_userEmail = value; } }
    private string m_userPassword;
    public string UserPassword { set { m_userPassword = value; } }

    public void Start() {
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId)) {
            PlayFabSettings.TitleId = "146EC";
        }
        if (PlayerPrefs.HasKey("userEmail")) {
            inEmail.text = PlayerPrefs.GetString("userEmail");
            if (PlayerPrefs.HasKey("userPassword")) {
                inPass.text = PlayerPrefs.GetString("userPassword");
            }
        }
        //auto login
        //var request = new LoginWithEmailAddressRequest { Email = m_userEmail, Password = m_userPassword };
        //PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result) {
        Debug.Log("User logged in");
        if (rememberMe.isOn) {
            PlayerPrefs.SetString("userEmail", m_userEmail);
            PlayerPrefs.SetString("userPassword", m_userPassword);
        }else {
            PlayerPrefs.DeleteKey("userEmail");
            PlayerPrefs.DeleteKey("userPassword");
        }
        PlayfabUserInfo.Instance.Start();
        GetProse.Instance.CheckForUpdate();
        FindObjectOfType<SceneChanger>().ChangeScene(1);
    }

    private void OnLoginFailure(PlayFabError error) {
        Debug.Log(error.GenerateErrorReport());
        PopupManager.Instance.ShowPopUp("Invalid credentials, register instead?", 5);
    }

    public void Login() {
        m_userEmail = inEmail.text;
        m_userPassword = inPass.text;

        LoginWithEmailAddressRequest request = new LoginWithEmailAddressRequest { Email = m_userEmail, Password = m_userPassword };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }
}
