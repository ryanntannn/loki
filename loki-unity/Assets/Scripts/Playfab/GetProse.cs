﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.AdminModels;
using UnityEngine.Networking;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GetProse : Singleton<GetProse> {
    private List<Paragraph> m_prosesAvaliable = new List<Paragraph>();

    private enum UpdateMode { Checking, NeedsUpdate, NeedsRedownload, Updating, Done }
    private UpdateMode m_currentUpdateMode;

    public void CheckForUpdate() {
        DontDestroyOnLoad(gameObject);
        name = "GET_PROSE";

        m_currentUpdateMode = UpdateMode.Checking;
        StartCoroutine(CheckVersion());
    }

    private IEnumerator CheckVersion() {
        PlayFabClientAPI.GetTitleData(
            new PlayFab.ClientModels.GetTitleDataRequest(),
            (_result) => {
                //compare playerpref update and my update
                m_currentUpdateMode = !PlayerPrefs.GetString("VersionInfo").Equals(_result.Data["Version"]) ?
                         _result.Data["RedownloadNeeded"].Equals("true") ?
                            UpdateMode.NeedsRedownload : UpdateMode.NeedsUpdate
                        : UpdateMode.Done;
                PlayerPrefs.SetString("VersionInfo", _result.Data["Version"]);
                Debug.Log("New version: " + PlayerPrefs.GetString("VersionInfo"));
                StartCoroutine(CheckDownload());
            },
            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
        );

        while (m_currentUpdateMode != UpdateMode.Done) {
            yield return null;
        }

        Debug.Log("Done!");
    }

    private IEnumerator CheckDownload() {
        switch (m_currentUpdateMode) {
            case UpdateMode.Done:
            case UpdateMode.NeedsUpdate:
                StartCoroutine(UpdateGame());
                break;
            case UpdateMode.NeedsRedownload:
                StartCoroutine(RedownloadGame());
                break;
        }

        while(m_currentUpdateMode != UpdateMode.Done) {
            yield return null;
        }
    }

    private IEnumerator RedownloadGame() {
        Debug.Log("Redownloading...");
        bool done = false;
        HashSet<string> prosesToLoad = new HashSet<string>();

        //check if got words folder
        Directory.CreateDirectory(Application.persistentDataPath + "/Words");

        GetContentListRequest listReq = new GetContentListRequest();
        PlayFabAdminAPI.GetContentList(
            listReq,
            (_result) => {
                foreach (ContentInfo content in _result.Contents.Where(x => x.Key.StartsWith("Words"))) {
                    //delete file
                    File.Delete(Application.persistentDataPath + "/" + content.Key);

                    GetContentDownloadUrlRequest dlReq = new GetContentDownloadUrlRequest();
                    dlReq.Key = content.Key;
                    PlayFabClientAPI.GetContentDownloadUrl(
                        dlReq,
                        (__result) => { StartCoroutine(DownloadData(__result.URL, content.Key)); },
                        (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
                    );
                }

                m_currentUpdateMode = UpdateMode.Done;
                done = true;
            },
            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
        );

        while (!done) {
            m_currentUpdateMode = UpdateMode.Updating;
            yield return null;
        }
    }

    private IEnumerator UpdateGame() {
        Debug.Log("Updating...");
        bool done = false;
        HashSet<string> prosesToLoad = new HashSet<string>();

        //check if got words folder
        Directory.CreateDirectory(Application.persistentDataPath + "/Words");

        GetContentListRequest listReq = new GetContentListRequest();
        PlayFabAdminAPI.GetContentList(
            listReq,
            (_result) => {
                foreach (ContentInfo content in _result.Contents.Where(x => x.Key.StartsWith("Words"))) {
                    //check if we have it
                    if (!File.Exists(Application.persistentDataPath + "/" + content.Key)) {
                        Debug.Log("We don't have " + content.Key + "... downloading it");
                        GetContentDownloadUrlRequest dlReq = new GetContentDownloadUrlRequest();
                        dlReq.Key = content.Key;
                        PlayFabClientAPI.GetContentDownloadUrl(
                            dlReq,
                            (__result) => { StartCoroutine(DownloadData(__result.URL, content.Key)); },
                            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
                        );
                    } else {
                        BinaryFormatter bf = new BinaryFormatter();
                        FileStream fs = File.Open(Application.persistentDataPath + "/" + content.Key, FileMode.Open);
                        m_prosesAvaliable.Add(JsonUtility.FromJson<Paragraph>(bf.Deserialize(fs) as string));
                        fs.Close();
                    }

                    m_currentUpdateMode = UpdateMode.Done;
                    done = true;
                }
            },
            (_error) => { Debug.LogError(_error.GenerateErrorReport()); }
        );

        while (!done) {
            m_currentUpdateMode = UpdateMode.Updating;
            yield return null;
        }
    }

    public Paragraph GetRandomProse() {
        if(m_prosesAvaliable.Count <= 0) {
            return null;
        }

        return m_prosesAvaliable[Random.Range(0, m_prosesAvaliable.Count - 1)];

        //DEBUGING SHIT
        //Debug.Log("Getting cursed prose");
        //bool xd = false;
        //foreach (Paragraph p in m_prosesAvaliable) {
        //    if (!p.Author.Equals("JK Rowling")) {
        //        if (!xd) {
        //            xd = true;
        //            continue;
        //        }
        //        return p;
        //    }
        //}
        //return m_prosesAvaliable[2];
    }

    private IEnumerator DownloadData(string _url, string _key) {
        using (UnityWebRequest webReq = UnityWebRequest.Get(_url)) {
            yield return webReq.SendWebRequest();
            if (webReq.isNetworkError) {
                Debug.LogError("Network error: " + webReq.error);
            } else {
                // Sometimes the json files have a 3 byte BOM infront of it
                // Thus, I cannot parse the .text into a json file
                // Hence, I use a try catch
                // If it does have the 3 byte BOM, I just truncate it
                Paragraph proseAvaliable;
                try {
                    proseAvaliable = JsonUtility.FromJson<Paragraph>(webReq.downloadHandler.text);
                } catch (System.ArgumentException) {
                    proseAvaliable = JsonUtility.FromJson<Paragraph>(Encoding.UTF8.GetString(webReq.downloadHandler.data, 3, webReq.downloadHandler.data.Length - 3));
                }

                m_prosesAvaliable.Add(proseAvaliable);

                //cache data
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = File.Create(Application.persistentDataPath + "/" + _key);
                bf.Serialize(fs, JsonUtility.ToJson(proseAvaliable));
                fs.Close();
            }
        }
    }
}
