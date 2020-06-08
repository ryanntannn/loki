﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class GachaMenuHandler : MonoBehaviour {
    public void Gacha() {
        //call script 
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest() {
                FunctionName = "GachaGold"
            },
            (_result) => {
                Debug.Log(_result.FunctionResult.ToString());
            },
            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
        );
    }

    public void GachaCp(string _catalogVer){
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest() {
                FunctionName = "GachaScrap",
                FunctionParameter = new { CatalogVer = _catalogVer }
            },
            (_result) => {
                Debug.Log(_result.FunctionResult.ToString());
            },
            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
        );
    }
}
