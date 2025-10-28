using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public AudioSource musicSource;
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostDeadMusic;

    public TMP_Text ghostTimerText;

    private float scaredTimer = 0f;
    private bool ghostsAreScared = false;

    public int score = 0;
    public TMP_Text scoreText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (musicSource != null && normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    void Update()
    {
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
    public void ActivatePowerPellet()
    {
        AddScore(50);
        scaredTimer = 10f;
        if (!ghostsAreScared)
        {
            if (musicSource != null && scaredMusic != null)
            {
                musicSource.Stop();
                musicSource.clip = scaredMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
            
            foreach (GhostController ghost in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            {
                ghost.SetState(GhostState.Scared);
            }

        }

        // start timer
        scaredTimer = 10f;
        ghostsAreScared = true;
    }

    private void EndScaredState()
    {
        ghostsAreScared = false;

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
}