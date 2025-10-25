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

    public Vector2 StartingPosition = Vector2.zero;

    public float cellSize = 1f;
    public Animator animator;

    public AudioSource audioSource;
    public AudioClip moveClip;

    public ParticleSystem dustEffect;

    // Start is called before the first frame update
    void Start()
    {
        currentInput = Vector2Int.right; 
        lastInput = Vector2Int.right;
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
            lastInput = Vector2Int.up;
        }

        else if (Input.GetKeyDown(KeyCode.S))
        {
            lastInput = Vector2Int.down;
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
        if (CanMoveTo(currentGridPos + lastInput))
        {
            currentInput = lastInput;
            BeginMove(currentGridPos + currentInput);
        }
        else if (CanMoveTo(currentGridPos + currentInput))
        {
            BeginMove(currentGridPos + currentInput);

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
            isMoving = false;
        }
    }

    void BeginMove(Vector2Int newTarget)
    {
        isMoving = true; targetGridPos = newTarget; 
        startPos = transform.position; 
        targetPos = new Vector3(StartingPosition.x + targetGridPos.x * cellSize, StartingPosition.y + targetGridPos.y * cellSize, transform.position.z); 
        lerpProgress = 0f;
    }

    private Vector2 lastAnimDir = Vector2.zero;
    void UpdateAnimation()
    {
        if (animator == null) return;

        if (currentInput != lastAnimDir)
        {
            if (currentInput.x != 0  || currentInput.y != 0)
            {
                animator.SetBool("IsMoving", isMoving);
            }

            animator.SetFloat("MoveX", currentInput.x);
            animator.SetFloat("MoveY", currentInput.y);
            lastAnimDir = currentInput;
        }

    }

    void HandleAudioAndParticles()
    {
        if (isMoving)
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

    bool CanMoveTo(Vector2Int gridPos) { return true; }
}

