using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PacStudentController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private bool isMoving = false;

    private Vector2Int lastInput;
    private Vector2Int currentInput;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress = 0f;

    public Vector2 StartingPosition = new Vector2(-12.5f, 13.5f); // default

    public float cellSize = 1f;
    public Animator animator;

    public AudioSource audioSource;
    public AudioClip moveClip;
    public AudioClip bumpClip;
    public AudioClip coinPickUpClip;
    public ParticleSystem dustEffect;
    public ParticleSystem wallBumpEffect;
    private float bumpTimer= 0f;
    private bool bumpCooldown = false;

    private LevelGenerator levelGen;
    
    private Vector2 lastAnimDir = Vector2.zero;

    private static readonly Vector2 MAP_ORIGIN = new Vector2(-13.5f, 14.5f);

    public GameObject throwablePelletPrefab;
    private bool hasPowerPellet = false;
    public UnityEngine.UI.Image batIcon;
    private Color batColorInactive = new Color(1f, 1f, 1f, 0.08f); 
    private Color batColorActive = new Color(1f, 1f, 1f, 1f);

    // Start is called before the first frame update
    void Start()
    {
        levelGen = LevelGenerator.Instance;
        currentInput = Vector2Int.right;
        lastInput = Vector2Int.right;

        currentGridPos = new Vector2Int(1, 1);
        transform.position = GridToWorld(currentGridPos);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();

        if (hasPowerPellet != GameManager.Instance.hasPowerPellet)
        {
            hasPowerPellet = GameManager.Instance.hasPowerPellet;

            if (batIcon != null)
                batIcon.color = hasPowerPellet ? batColorActive : batColorInactive;
        }

        if (!isMoving)
        {
            Move();
        }
        else
        {
            LerpMove();
        }

        if (hasPowerPellet && Input.GetKeyDown(KeyCode.Q))
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "InnovationLevel")
            {
                ThrowPellet();
            }
        }

        UpdateAnimation();
        HandleAudioAndParticles();
    }

    void ThrowPellet()
    {
        Vector2 dir = Vector2.zero;
        if (animator.GetFloat("MoveX") != 0)
        {
            dir = new Vector2(animator.GetFloat("MoveX"), 0);
        }
        else if (animator.GetFloat("MoveY") != 0)
        {
            dir = new Vector2(0, -animator.GetFloat("MoveY"));
        }
        else
        {
            dir = Vector2.right;
        }

        GameObject pellet = Instantiate(throwablePelletPrefab, transform.position, Quaternion.identity);
        pellet.GetComponent<ThrowablePellet>().Launch(dir);
        GameManager.Instance.hasPowerPellet = false;
        if (batIcon != null)
        {
            batIcon.color = batColorInactive;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            lastInput = Vector2Int.down;
        }

        else if (Input.GetKeyDown(KeyCode.S))
        {
            lastInput = Vector2Int.up;
        }

        else if (Input.GetKeyDown(KeyCode.A))
        {
            lastInput = Vector2Int.left;
        }

        else if (Input.GetKeyDown(KeyCode.D))
        {
            lastInput = Vector2Int.right;
        }
    }

    void Move()
    {
        bool moved = false;


        if (CanMoveTo(currentGridPos + lastInput))
        {
            currentInput = lastInput;
            BeginMove(currentGridPos + currentInput);
            moved = true;
            bumpCooldown = false;
        }
        else if (CanMoveTo(currentGridPos + currentInput))
        {
            BeginMove(currentGridPos + currentInput);
            moved = true;
            bumpCooldown = false;
        }
        if (!moved)
        {
            isMoving = false;

            if (bumpTimer <= 0f && bumpCooldown == false)
            {
                bumpCooldown = true;
                Debug.Log("Bump");

                if (audioSource != null && bumpClip != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(bumpClip);
                }

                if (wallBumpEffect != null)
                {
                    Vector3 bumpPos = GridToWorld(currentGridPos + lastInput);
                    wallBumpEffect.transform.position = bumpPos;
                    wallBumpEffect.Play();
                }

                bumpTimer = 0.25f; 
            }
        }
    }

    void LerpMove()
    {
        lerpProgress += moveSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);

        if (lerpProgress >= 1f)
        {
            transform.position = targetPos;
            currentGridPos = targetGridPos;
            isMoving = false;

            HandleTeleport();
        }
    }

    void HandleTeleport()
    {
        int[,] map = levelGen.fullMap;
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        if (currentGridPos.x < 0)
        {
            currentGridPos = new Vector2Int(cols - 1, currentGridPos.y);
            transform.position = GridToWorld(currentGridPos);
        }

        else if (currentGridPos.x >= cols)
        {
            currentGridPos = new Vector2Int(0, currentGridPos.y);
            transform.position = GridToWorld(currentGridPos);
        }
    }

    void BeginMove(Vector2Int newTarget)
    {
        isMoving = true;
        targetGridPos = newTarget;
        startPos = transform.position;
        targetPos = GridToWorld(targetGridPos);
        lerpProgress = 0f;
    }
    private bool isActuallyMoving = false;
    void UpdateAnimation()
    {
        if (animator == null) return;

        if (!isActuallyMoving && isMoving)
        {
            isActuallyMoving = true;
            animator.SetBool("IsMoving", true);
        }

        if (isActuallyMoving && !isMoving && !CanMoveTo(currentGridPos + currentInput))
        {
            isActuallyMoving = false;
            animator.SetBool("IsMoving", false);
        }

        if (currentInput != lastAnimDir)
        {
            animator.SetFloat("MoveX", currentInput.x);
            animator.SetFloat("MoveY", currentInput.y);
            lastAnimDir = currentInput;
        }
    }
    void HandleAudioAndParticles()
    {
        if (bumpTimer > 0f)
        {
            bumpTimer -= Time.deltaTime;
        }
        else if (isMoving)
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.clip = moveClip;
                audioSource.Play();
            }

            if (dustEffect != null && !dustEffect.isPlaying)
            {
                dustEffect.Play();
            }
        }
        else
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (dustEffect != null && dustEffect.isPlaying)
            {
                dustEffect.Stop();
            }
        }
    }

    bool CanMoveTo(Vector2Int gridPos)
    {
        int[,] map = levelGen.fullMap;
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        if (gridPos.x < 0 && gridPos.y >= 0 && gridPos.y < rows)
        {
            targetGridPos = new Vector2Int(cols - 1, gridPos.y);
            return true;
        }
        if (gridPos.x >= cols && gridPos.y >= 0 && gridPos.y < rows)
        {
            targetGridPos = new Vector2Int(0, gridPos.y);
            return true;
        }

        if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= cols || gridPos.y >= rows) return false;

        int tile = map[gridPos.y, gridPos.x];

        if (tile == 8) return false;                 // ghost gate blocks PacStudent
        return tile == 0 || tile == 5 || tile == 6;  // empty/pellet/power
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Pellet"))
        {
            audioSource.Stop();
            audioSource.PlayOneShot(coinPickUpClip);

            GameManager.Instance.OnCoinCollected();
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("PowerPellet"))
        {
            Debug.Log("PowerPellet! 50 Points!");
            GameManager.Instance.hasPowerPellet = false;
            if (batIcon != null)
            {
                batIcon.color = batColorActive;
            }
            GameManager.Instance.ActivatePowerPellet();
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("BonusCherry"))
        {
            Debug.Log("BonusCherry! 100 Points!");
            GameManager.Instance.AddScore(100);
            Destroy(collision.gameObject);
        }
    }

    Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(
            MAP_ORIGIN.x + g.x * cellSize,
            MAP_ORIGIN.y - g.y * cellSize,
            transform.position.z
        );
    }
}

