using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public AudioSource musicSource;
    public AudioClip normalMusic;
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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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

            // Recovering phase (3 seconds left)
            if (scaredTimer <= 3f)
            {
                foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
                {
                    if (ghost.CurrentState == GhostState.Scared)
                        ghost.SetState(GhostState.Recovering);
                }
            }

            // End scared state
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
                countdownText.text = count.ToString();

            yield return new WaitForSeconds(1f);
            count--;
        }

        // One last frame showing "1" before hiding
        yield return new WaitForSeconds(0.5f);

        if (countdownHUD != null)
            countdownHUD.SetActive(false);

        EnableGameplay();
    }

    private void DisableGameplay()
    {
        // Stop timer
        isTiming = false;

        // Disable ghosts
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            ghost.enabled = false;

        // Disable player movement
        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
            player.enabled = false;
    }

    private void EnableGameplay()
    {
        if (musicSource != null && normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
        // Start timer
        isTiming = true;

        // Enable ghosts
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            ghost.enabled = true;

        // Enable player
        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
            player.enabled = true;
    }
    public void OnCoinCollected()
    {
        collectedCoins++;
        AddScore(10); // optional: add points for each coin

        if (collectedCoins >= totalCoins)
        {
            StartCoroutine(GameOverSequence());
        }
    }
    public void ActivatePowerPellet()
    {
        AddScore(50);
        scaredTimer = 10f;
        if (musicSource != null && scaredMusic != null)
        {
           musicSource.Stop();
           musicSource.clip = scaredMusic;
           musicSource.loop = true;
           musicSource.Play();
        }
            
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            if (ghost.CurrentState != GhostState.Dead &&
            ghost.CurrentState != GhostState.InHouse &&
            ghost.CurrentState != GhostState.LeavingHouse)
            {
                ghost.SetState(GhostState.Scared);
            }
        }


        // start timer
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

    private void EndScaredState()
    {
        ghostsAreScared = false;
        ghostDeadMusicActive = false;

        // Revert music
        if (musicSource != null && normalMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Reset all ghosts to normal, except dead ones
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            if (ghost.CurrentState != GhostState.Dead)
                ghost.SetState(GhostState.Normal);
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText)
            scoreText.text = score.ToString();
    }

    public void PlayerDied()
    {
        if (playerIsDead) return; // prevent multiple triggers
        playerIsDead = true;
        isTiming = false;

        Debug.Log("Player has died!");

        // Stop all music immediately
        if (musicSource != null)
            musicSource.Stop();

        // Disable ghosts
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghost.enabled = false;
        }

        // Disable player
        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = false;
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
                anim.SetBool("IsDead", true);
        }

        // Decrease life and update UI
        playerLives--;
        UpdateLivesUI();

        // Check for Game Over
        if (playerLives <= 0)
        {
            StartCoroutine(GameOverSequence());
        }
        else
        {
            // Pause before resetting if player still has lives
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
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f); // 2-digit ms

        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
    }

    private int highScore;
    private float bestTime;
    private void CheckAndSaveHighScore()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string scoreKey = currentScene + "_HighScore";
        string timeKey = currentScene + "_BestTime";

        int storedScore = PlayerPrefs.GetInt(scoreKey, 0);
        float storedTime = PlayerPrefs.GetFloat(timeKey, Mathf.Infinity);

        bool isNewHighScore = false;

        if (score > storedScore)
            isNewHighScore = true;
        else if (score == storedScore && elapsedTime < storedTime)
            isNewHighScore = true;

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
            musicSource.Stop();

        if (gameOverHUD != null)
            gameOverHUD.SetActive(true);
        
        CheckAndSaveHighScore();
        yield return new WaitForSeconds(3f);

        // Load StartScene after 3 seconds
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

        // Resume normal background music
        if (musicSource != null && normalMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Reset ghosts instantly to home
        foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghost.enabled = true;
            ghost.TeleportHome();
            StartCoroutine(ghost.WaitThenLeave());
        }

        PacStudentController player = FindFirstObjectByType<PacStudentController>();
        if (player != null)
        {
            player.enabled = true;
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
                anim.SetBool("IsDead", false); ;
        }

        ghostsAreScared = false;
        ghostDeadMusicActive = false;
        playerIsDead = false;
        isTiming = true;
    }
}