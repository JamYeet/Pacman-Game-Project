using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public CherryManager cherryManager;

    public AudioSource musicSource;
    public AudioClip normalMusic;
    public AudioClip playerDeathClip;
    public AudioClip introMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostDeadMusic;

    public TMP_Text ghostTimerText;

    private float scaredTimer = 0f;
    private bool ghostsAreScared = false;
    private bool ghostDeadMusicActive = false;

    public GameObject countdownHUD;
    public TMP_Text countdownText;

    public GameObject[] lifeIcons;
    public GameObject gameOverHUD; 
    private int playerLives = 3;

    public int score = 0;
    public TMP_Text scoreText;

    private int totalCoins;
    private int collectedCoins;

    public TMP_Text timerText;
    private float elapsedTime = 0f;
    private bool isTiming = true;

    public float deathPauseDuration = 3f;
    private bool playerIsDead = false;

    public bool hasPowerPellet = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicSource != null && introMusic != null)
        {
            musicSource.clip = introMusic;
            musicSource.loop = false;
            musicSource.Play();
        }

        countdownHUD.SetActive(true);

        isTiming = false;
        DisableGameplay();

        totalCoins = GameObject.FindGameObjectsWithTag("Pellet").Length;
        collectedCoins = 0;

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        if (isTiming)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }

        if (ghostsAreScared)
        {
            scaredTimer -= Time.deltaTime;
            ghostTimerText.text = Mathf.CeilToInt(scaredTimer).ToString();

            if (scaredTimer <= 3f)
            {
                foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
                {
                    if (ghost.CurrentState == GhostState.Scared)
                    { 
                        ghost.SetState(GhostState.Recovering);
                    }
                }
            }

            if (scaredTimer <= 0f)
            {
                EndScaredState();
                ghostTimerText.text = "RUN";
            }
        }
    }

    private IEnumerator StartCountdown()
    {
        int count = 3;
        while (count > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = count.ToString();
            }
            yield return new WaitForSeconds(1f);
            count--;
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
        }

        yield return new WaitForSeconds(1f);

        if (countdownHUD != null)
        {
            countdownHUD.SetActive(false);
        }
        EnableGameplay();
    }

    private void DisableGameplay()
    {
        isTiming = false;

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            ghost.enabled = false;

        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = false;
        }

        if (cherryManager != null)
        {
            cherryManager.enabled = false;
        }
    }

    private void EnableGameplay() 
    {
        if (musicSource != null && normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        isTiming = true;

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            ghost.enabled = true;

        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = true;
        }

        if (cherryManager != null)
        {
            cherryManager.enabled = true;
        }
    }
    public void OnCoinCollected()
    {
        collectedCoins++;
        AddScore(10);

        if (collectedCoins >= totalCoins)
        {
            StartCoroutine(GameOverSequence());
        }
    }
    public void ActivatePowerPellet()
    {
        AddScore(50);
        scaredTimer = 10f;
        hasPowerPellet = true;

        if (musicSource != null && scaredMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = scaredMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            if (ghost.CurrentState != GhostState.Dead && ghost.CurrentState != GhostState.InHouse && ghost.CurrentState != GhostState.LeavingHouse)
            {
                ghost.SetState(GhostState.Scared);
            }
        }

        scaredTimer = 10f;
        ghostsAreScared = true;
    }

    public void PlayGhostDeadMusic()
    {
        if (ghostDeadMusicActive) return;

        ghostDeadMusicActive = true;

        if (musicSource != null && ghostDeadMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = ghostDeadMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    public void ForceGhostsIntoRecovery()
    {
        scaredTimer = 3f;
        ghostsAreScared = true;

        if (ghostTimerText != null)
        {
            ghostTimerText.text = Mathf.CeilToInt(scaredTimer).ToString();
        }

    }
    public void EndScaredState() 
    {
        ghostsAreScared = false;
        ghostDeadMusicActive = false;
        hasPowerPellet = false;

        if (musicSource != null && normalMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            if (ghost.CurrentState != GhostState.Dead)
            {
                ghost.SetState(GhostState.Normal);
            }
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("D6");
        }
    }

    public void PlayerDied()
    {
        if (playerIsDead) return;
        playerIsDead = true;
        isTiming = false;

        Debug.Log("Player has died!");

        if (musicSource != null)
        {
            musicSource.Stop();

        }

        if (musicSource != null && playerDeathClip != null)
        {
            musicSource.clip = playerDeathClip;
            musicSource.loop = false;
            musicSource.Play();
        }

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghost.enabled = false;
        }

        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = false;
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsDead", true);
            }
        }

        playerLives--;
        UpdateLivesUI();

        if (playerLives <= 0)
        {
            StartCoroutine(GameOverSequence());
        }
        else
        {
            StartCoroutine(RestartAfterDelay());
        }
    }

    private void UpdateLivesUI()
    {
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            lifeIcons[i].SetActive(i < playerLives);
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);

        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }

    }

    private void CheckAndSaveHighScore()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string scoreKey = currentScene + "_HighScore";
        string timeKey = currentScene + "_BestTime";

        int storedScore = PlayerPrefs.GetInt(scoreKey, 0);
        float storedTime = PlayerPrefs.GetFloat(timeKey, Mathf.Infinity);

        bool isNewHighScore = false;

        if (score > storedScore)
        {
            isNewHighScore = true;
        }

        else if (score == storedScore && elapsedTime < storedTime)
        {
            isNewHighScore = true;
        }

        if (isNewHighScore)
        {
            PlayerPrefs.SetInt(scoreKey, score);
            PlayerPrefs.SetFloat(timeKey, elapsedTime);
            PlayerPrefs.Save();

            Debug.Log($"New high score for {currentScene}! {score} in {elapsedTime:F2}s");
        }
    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("GAME OVER!");
        DisableGameplay();

        if (musicSource != null)
        {
            musicSource.Stop();
        }


        if (gameOverHUD != null)
        {
            gameOverHUD.SetActive(true);
        }
        
        CheckAndSaveHighScore();
        yield return new WaitForSeconds(3f);

        SceneManager.LoadScene("StartScene");
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(deathPauseDuration);
        ResetRound();
    }

    private void ResetRound()
    {
        Debug.Log("Resetting round...");

        if (musicSource != null && normalMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghost.enabled = true;
            ghost.TeleportHome();
            StartCoroutine(ghost.WaitThenLeave());

            Animator anim = ghost.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsDead", false);
                anim.SetBool("IsRecovering", false);
            }

            ghost.SetState(GhostState.Normal);
        }

        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = true;
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsDead", false); ;
            }
        }

        
        ghostsAreScared = false;
        ghostDeadMusicActive = false;
        playerIsDead = false;
        isTiming = true;
    }
}