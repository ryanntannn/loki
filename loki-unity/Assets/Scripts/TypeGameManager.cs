﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TypeGameManager : Singleton<TypeGameManager>
{
    //String for player to type
    public string wordsString;
    public bool useRandomWords = true;

    //String that the player has typed
    string inputString = "";

    string awardedString = "";

    //Current word that the player has typed
    string inputWord = "";

    public TextMeshProUGUI inputTextMesh;
    public List<TRWord> words;
    public List<TRWord> mistakeWords;
    public int wordIndex;
    public int charIndex;
    char nextChar;

    public int score;

    public int combo;
    public int maxCombo;
    float comboTimer;
    public float maxComboTimer;

    public GameObject readyGO;
    public GameObject gameGO;
    public TextMeshProUGUI countDownText;

    public enum GameState
    {
        Ready, Countdown, Playing, Analytics
    }

    public GameState gameState;

    private void Start()
    {
        comboTimer = maxComboTimer;

        if (useRandomWords) {
            wordsString = GetProse.Instance.GetRandomProse().Prose;
        }
        ConvertStringToTRWords(wordsString);
    }

    private void Update()
    {
        comboTimer -= Time.deltaTime;
        if(comboTimer <= 0)
        {
            comboTimer = 0;
            combo = 0;
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        PersistantCanvas.Instance.ChangeScene(2);
    }

    public float GetComboTimer()
    {
        return comboTimer / maxComboTimer;
    }

    void ConvertStringToTRWords(string s)
    {
        char[] ncwords = s.ToCharArray();
        List<char> nextTRWord = new List<char>();
        for(int i = 0; i < ncwords.Length; i++)
        {
            nextTRWord.Add(ncwords[i]);
            if(ncwords[i].Equals(' ') || i == ncwords.Length - 1) //Make new TRWord when theres a space or is last char
            {
                Debug.Log(new string(nextTRWord.ToArray()));
                TRWord nextTRWordSO = ScriptableObject.CreateInstance<TRWord>();
                nextTRWordSO.word = nextTRWord.ToArray();
                words.Add(nextTRWordSO);
                nextTRWord.Clear();
            }
        }
    }

    void UpdateTextMesh()
    {
        if (words[wordIndex].CompareWords(inputWord.ToCharArray()))
        {
            inputTextMesh.color = new Color(1, 1, 1);
        }
        else
        {
            inputTextMesh.color = new Color(1, 0.5f, 0.5f);
            if (!mistakeWords.Contains(words[wordIndex]))
            {
                mistakeWords.Add(words[wordIndex]);
                combo = 0;
            }
        }
        inputTextMesh.text = inputWord;
    }

    IEnumerator CountDown(int count)
    {
        countDownText.text = count.ToString();
        ButtonChime.Instance.PlayChime();
        yield return new WaitForSeconds(0.8f);
        count--;
        if (count == 0)
        {
            gameState = GameState.Playing;
            countDownText.gameObject.SetActive(false);
            gameGO.SetActive(true);
        }
        else
        {
            StartCoroutine(CountDown(count));
        }
    }

    public void AddCharacterToInputString(char character)
    {
        if(gameState == GameState.Ready && character == ' ')
        {
            gameState = GameState.Countdown;
            readyGO.SetActive(false);
            countDownText.gameObject.SetActive(true);
            StartCoroutine(CountDown(3));
        }

        if (gameState == GameState.Playing) {
            //Update input strings
            inputString += character;
            inputWord += character;

            //Check to move on to the next word
            if (character == ' ' && words[wordIndex].CompareWords(inputWord.ToCharArray()))
            {
                NextWord();
            }

            if (inputString == wordsString)
            {
                Complete();
            }

            //Update the textMesh
            UpdateTextMesh();

            if (words[wordIndex].CompareWords(inputWord.ToCharArray()) && inputString.Length > awardedString.Length)
            {
                awardedString += character;
                combo++;
                float scoreTimeScale = Mathf.Pow(GetComboTimer() * 10.0f, 2);
                score += (int)(scoreTimeScale * combo);

                comboTimer = maxComboTimer;

                if (combo > maxCombo)
                {
                    maxCombo = combo;
                }
            }
            SendMessage("UpdateInput", SendMessageOptions.DontRequireReceiver);
        }
    }

    void NextWord()
    {
        inputWord = "";
        wordIndex++;

        if (wordIndex.Equals(words.Count))
        {
            Complete();
        }
    }

    void Complete()
    {
        Debug.Log("Complete");
        SendMessage("GameComplete", SendMessageOptions.DontRequireReceiver);
        gameState = GameState.Analytics;
    }

    public void BackSpacePressed()
    {
        combo = 0;
        if (inputWord.Length != 0)
        {
            inputString = inputString.Substring(0, inputString.Length - 1);
            inputWord = inputWord.Substring(0, inputWord.Length - 1);
        }

        UpdateTextMesh();
    }
}
