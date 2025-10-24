using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button level1Button;
    public Button level2Button;

    [Header("Audio")]
    public AudioSource backgroundMusic;

    [Header("Animated Border Groups")]
    public GameObject dotsA;
    public GameObject dotsB;

    [Header("Border Animation Settings")]
    public float blinkInterval = 0.3f; 

    private float timer = 0f;
    private bool showingA = true;

    void Start()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.loop = true;
            backgroundMusic.Play();
        }

        if (level1Button != null)
            level1Button.onClick.AddListener(() => LoadScene("ManualLevel"));
        if (level2Button != null)
            level2Button.onClick.AddListener(() => LoadScene("InnovationScene"));

        if (dotsA != null) dotsA.SetActive(true);
        if (dotsB != null) dotsB.SetActive(false);
    }

    void Update()
    {
        AnimateBorder();
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