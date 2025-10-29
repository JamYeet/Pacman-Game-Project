using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    public Button level1Button;
    public Button level2Button;

    public AudioSource backgroundMusic;

    public GameObject dotsA;
    public GameObject dotsB;

    public float blinkInterval = 0.3f; 

    private float timer = 0f;
    private bool showingA = true;

    public TMP_Text level1ScoreText;
    public TMP_Text level1TimeText;
    public TMP_Text level2ScoreText;
    public TMP_Text level2TimeText;


    void Start()
    {
        LoadHighScores();

        if (backgroundMusic != null)
        {
            backgroundMusic.loop = true;
            backgroundMusic.Play();
        }

        if (level1Button != null)
        { 
            level1Button.onClick.AddListener(() => LoadScene("ManualLevel"));
        }
        if (dotsA != null) dotsA.SetActive(true);
        if (dotsB != null) dotsB.SetActive(false);
    }

    void Update()
    {
        AnimateBorder();
    }

    void LoadHighScores()
    {
        // LEVEL 1
        int l1Score = PlayerPrefs.GetInt("ManualLevel_HighScore", 0);
        float l1Time = PlayerPrefs.GetFloat("ManualLevel_BestTime", Mathf.Infinity);

        if (level1ScoreText != null)
            level1ScoreText.text = $"{l1Score}";

        if (level1TimeText != null)
        {
            if (l1Time == Mathf.Infinity)
                level1TimeText.text = "--:--:--";
            else
            {
                int m = Mathf.FloorToInt(l1Time / 60f);
                int s = Mathf.FloorToInt(l1Time % 60f);
                int ms = Mathf.FloorToInt((l1Time * 100f) % 100f);
                level1TimeText.text = $"{m:00}:{s:00}:{ms:00}";
            }
        }

        // LEVEL 2
        int l2Score = PlayerPrefs.GetInt("Level2_HighScore", 0);
        float l2Time = PlayerPrefs.GetFloat("Level2_BestTime", Mathf.Infinity);

        if (level2ScoreText != null)
            level2ScoreText.text = $"{l2Score}";

        if (level2TimeText != null)
        {
            if (l2Time == Mathf.Infinity)
                level2TimeText.text = "--:--:--";
            else
            {
                int m = Mathf.FloorToInt(l2Time / 60f);
                int s = Mathf.FloorToInt(l2Time % 60f);
                int ms = Mathf.FloorToInt((l2Time * 100f) % 100f);
                level2TimeText.text = $"Best Time: {m:00}:{s:00}:{ms:00}";
            }
        }
    }

    void AnimateBorder()
    {
        timer += Time.deltaTime;
        if (timer >= blinkInterval)
        {
            timer = 0f;
            showingA = !showingA;

            if (dotsA != null) dotsA.SetActive(showingA);
            if (dotsB != null) dotsB.SetActive(!showingA);
        }
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}