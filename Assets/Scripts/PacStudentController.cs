using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public ParticleSystem dustEffect;
    public ParticleSystem wallBumpEffect;
    private float bumpTimer= 0f;
    private bool bumpCooldown = false;

    private LevelGenerator levelGen;
    
    private Vector2 lastAnimDir = Vector2.zero;

    private static readonly Vector2 MAP_ORIGIN = new Vector2(-13.5f, 14.5f);

    // Start is called before the first frame update
    void Start()
    {
        levelGen = LevelGenerator.Instance;
        currentInput = Vector2Int.right;
        lastInput = Vector2Int.right;

        // Explicitly set starting grid coord (1,1), then place in world
        currentGridPos = new Vector2Int(1, 1);
        transform.position = GridToWorld(currentGridPos);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();

        if (!isMoving)
        {
            Move();
        }
        else
        {
            LerpMove();
        }
        UpdateAnimation();
        HandleAudioAndParticles();
    }

    void HandleInput()
    {
        // Record only when player presses a new key
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

            // Only trigger bump if cooldown expired
            if (bumpTimer <= 0f && bumpCooldown == false)
            {
                bumpCooldown = true;
                Debug.Log("Bump");

                if (audioSource != null && bumpClip != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(bumpClip);
                }

                // Play bump particles
                if (wallBumpEffect != null)
                {
                    Vector3 bumpPos = GridToWorld(currentGridPos + lastInput);
                    wallBumpEffect.transform.position = bumpPos;
                    wallBumpEffect.Play();
                }

                // Start cooldown (so the sound doesn’t spam)
                bumpTimer = 0.25f; // adjust timing as you like
            }
        }
    }

    void LerpMove()
    {
        lerpProgress += moveSpeed * Time.deltaTime / cellSize;
        transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);

        if (lerpProgress >= 1f)
        {
            transform.position = targetPos;
            currentGridPos = targetGridPos;
            isMoving = false; // stop moving cleanly
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

        // Detect when we start moving after being idle
        if (!isActuallyMoving && isMoving)
        {
            isActuallyMoving = true;
            animator.SetBool("IsMoving", true);
            Debug.Log("Anim started");
        }

        // Detect when we’ve come to a complete stop
        if (isActuallyMoving && !isMoving && !CanMoveTo(currentGridPos + currentInput))
        {
            isActuallyMoving = false;
            animator.SetBool("IsMoving", false);
            Debug.Log("Anim stopped");
        }

        // Direction changes only when input changes
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

        // Left/right tunnel wrap on the same row
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

        // Bounds
        if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= cols || gridPos.y >= rows)
            return false;

        // Direct lookup (no flip, because our worldgrid already handles Y inversion)
        int tile = map[gridPos.y, gridPos.x];

        // Walkability
        if (tile == 8) return false;                 // ghost gate blocks PacStudent
        return tile == 0 || tile == 5 || tile == 6;  // empty/pellet/power
    }

    Vector3 GridToWorld(Vector2Int g)
    {
        // Y increases downward in the grid, so subtract on world Y
        return new Vector3(
            MAP_ORIGIN.x + g.x * cellSize,
            MAP_ORIGIN.y - g.y * cellSize,
            transform.position.z
        );
    }

    Vector2Int WorldToGrid(Vector3 w)
    {
        return new Vector2Int(
            Mathf.RoundToInt((w.x - MAP_ORIGIN.x) / cellSize),
            Mathf.RoundToInt((MAP_ORIGIN.y - w.y) / cellSize) // note the inverted Y
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (levelGen == null || levelGen.fullMap == null) return;

        int[,] map = levelGen.fullMap;
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Vector3 pos = new Vector3(
                    MAP_ORIGIN.x + x * cellSize,
                    MAP_ORIGIN.y - y * cellSize,
                    0
                );

                int tile = map[y, x]; // same indexing as CanMoveTo (no flip)
                bool walkable = (tile == 0 || tile == 5 || tile == 6);

                Gizmos.color = walkable
                    ? new Color(0f, 1f, 0f, 0.15f)
                    : new Color(1f, 0f, 0f, 0.25f);

                Gizmos.DrawCube(pos, Vector3.one * (cellSize * 0.9f));
            }
        }

        // PacStudent's current tile highlight
        Gizmos.color = Color.yellow;
        Vector3 currentTilePos = GridToWorld(currentGridPos);
        Gizmos.DrawWireCube(currentTilePos, Vector3.one * (cellSize * 1.2f));
    }
#endif
}

