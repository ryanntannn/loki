﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using TMPro;

public class MultiplayerKeyboard : MonoBehaviour
{
    public NetworkKeyboard keyboard;
    private UserAccountInfo m_accountInfo;
    public TextMeshPro playerNameUI;
    public string Username { get { return m_accountInfo.Username; } }

    // Start is called before the first frame update
    void Start()
    {
        //keyboard = GetComponentInChildren<NetworkKeyboard>();
        //playerNameUI = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Init(List<ItemInstance> items, UserAccountInfo u)
    {
        m_accountInfo = u;
        playerNameUI.text = u.Username;
        keyboard.Init(items);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
