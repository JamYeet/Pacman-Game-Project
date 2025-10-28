using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }
public class GhostController : MonoBehaviour
{
    public GhostState CurrentState { get; private set; } = GhostState.Normal;

    [Header("Movement Speeds")]
    public float normalSpeed = 3f;
    public float scaredSpeed = 1.5f;
    public float deadSpeed = 5f;

    [Header("Ghost Identity")]
    public int ghostID = 1;             // 1–4
    public Transform player;            // PacStudent reference

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public Vector2 mapOrigin = new Vector2(-13.5f, 14.5f);

    private float currentSpeed;
    private Animator animator;

    private Vector2Int currentGrid;
    private Vector2Int targetGrid;
    private Vector2Int lastDir = Vector2Int.zero;
    private Vector2Int moveDir = Vector2Int.zero;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress = 0f;
    private bool isMoving = false;
    private bool returningHome = false;

    private LevelGenerator levelGen;

    private void Start()
    {
        animator = GetComponent<Animator>();
        levelGen = LevelGenerator.Instance;
        currentGrid = WorldToGrid(transform.position);
        startPos = transform.position;
        SetState(GhostState.Normal);
    }

    private void Update()
    {
        if (CurrentState == GhostState.Dead)
        {
            MoveToHome();
            return;
        }

        if (!isMoving)
        {
            ChooseNextDirection();
        }
        else
        {
            MoveLerp();
        }

        // Update animator
        animator.SetFloat("MoveX", moveDir.x);
        animator.SetFloat("MoveY", moveDir.y);
    }

    //=========================================
    // STATE MANAGEMENT
    //=========================================
    public void SetState(GhostState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        animator.SetBool("IsScared", false);
        animator.SetBool("IsRecovering", false);

        switch (newState)
        {
            case GhostState.Normal:
                currentSpeed = normalSpeed;
                break;

            case GhostState.Scared:
                currentSpeed = scaredSpeed;
                animator.SetBool("IsScared", true);
                break;

            case GhostState.Recovering:
                currentSpeed = scaredSpeed;
                animator.SetBool("IsRecovering", true);
                break;

            case GhostState.Dead:
                currentSpeed = deadSpeed;
                animator.SetBool("IsDead", true);
                returningHome = true;
                break;
        }
    }

    private void MoveToHome()
    {
        // Move toward spawn ignoring walls
        transform.position = Vector3.MoveTowards(transform.position, startPos, deadSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startPos) < 0.1f)
        {
            returningHome = false;
            Respawn();
        }
    }

    public void Respawn()
    {
        transform.position = startPos;
        animator.SetBool("IsDead", false);
        SetState(GhostState.Normal);
    }

    //=========================================
    // MOVEMENT
    //=========================================
    private void MoveLerp()
    {
        lerpProgress += Time.deltaTime * currentSpeed / cellSize;
        transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);

        if (lerpProgress >= 1f)
        {
            transform.position = targetPos;
            currentGrid = targetGrid;
            isMoving = false;
            lerpProgress = 0f;
        }
    }

    private void ChooseNextDirection()
    {
        List<Vector2Int> possibleDirs = new List<Vector2Int>()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        // No backtracking unless no choice
        if (lastDir != Vector2Int.zero)
            possibleDirs.Remove(-lastDir);

        List<Vector2Int> validDirs = new List<Vector2Int>();

        foreach (var dir in possibleDirs)
        {
            Vector2Int check = currentGrid + dir;
            if (CanMoveTo(check))
                validDirs.Add(dir);
        }

        if (validDirs.Count == 0)
            validDirs.Add(-lastDir);

        Vector2Int chosen = DecideDirection(validDirs);
        StartMove(chosen);
    }

    private void StartMove(Vector2Int dir)
    {
        lastDir = dir;
        moveDir = dir;
        startPos = transform.position;
        targetGrid = currentGrid + dir;
        targetPos = GridToWorld(targetGrid);
        isMoving = true;
    }

    //=========================================
    // DECISION LOGIC
    //=========================================
    private Vector2Int DecideDirection(List<Vector2Int> validDirs)
    {
        if (player == null)
            return validDirs[Random.Range(0, validDirs.Count)];

        Vector2 ghostPos = transform.position;
        Vector2 playerPos = player.position;
        float currentDist = Vector2.Distance(ghostPos, playerPos);

        switch (CurrentState)
        {
            case GhostState.Scared:
            case GhostState.Recovering:
                return ChooseDirectionAvoidingPlayer(validDirs, ghostPos, playerPos, currentDist);

            case GhostState.Normal:
                switch (ghostID)
                {
                    case 1:
                        return ChooseDirectionAvoidingPlayer(validDirs, ghostPos, playerPos, currentDist);
                    case 2:
                        return ChooseDirectionChasingPlayer(validDirs, ghostPos, playerPos, currentDist);
                    case 3:
                        return validDirs[Random.Range(0, validDirs.Count)];
                    case 4:
                        return ChooseClockwise(validDirs);
                }
                break;
        }

        return validDirs[Random.Range(0, validDirs.Count)];
    }

    private Vector2Int ChooseDirectionAvoidingPlayer(List<Vector2Int> validDirs, Vector2 ghostPos, Vector2 playerPos, float currentDist)
    {
        List<Vector2Int> furtherDirs = new List<Vector2Int>();
        foreach (var dir in validDirs)
        {
            Vector2 newPos = ghostPos + (Vector2)dir * cellSize;
            if (Vector2.Distance(newPos, playerPos) >= currentDist)
                furtherDirs.Add(dir);
        }
        if (furtherDirs.Count > 0)
            return furtherDirs[Random.Range(0, furtherDirs.Count)];
        return validDirs[Random.Range(0, validDirs.Count)];
    }

    private Vector2Int ChooseDirectionChasingPlayer(List<Vector2Int> validDirs, Vector2 ghostPos, Vector2 playerPos, float currentDist)
    {
        List<Vector2Int> closerDirs = new List<Vector2Int>();
        foreach (var dir in validDirs)
        {
            Vector2 newPos = ghostPos + (Vector2)dir * cellSize;
            if (Vector2.Distance(newPos, playerPos) <= currentDist)
                closerDirs.Add(dir);
        }
        if (closerDirs.Count > 0)
            return closerDirs[Random.Range(0, closerDirs.Count)];
        return validDirs[Random.Range(0, validDirs.Count)];
    }

    private Vector2Int ChooseClockwise(List<Vector2Int> validDirs)
    {
        Vector2Int[] clockwise = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var dir in clockwise)
        {
            if (validDirs.Contains(dir))
                return dir;
        }
        return validDirs[Random.Range(0, validDirs.Count)];
    }

    //=========================================
    // GRID HELPERS
    //=========================================
    private bool CanMoveTo(Vector2Int gridPos)
    {
        int[,] map = levelGen.fullMap;
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= cols || gridPos.y >= rows)
            return false;

        int tile = map[gridPos.y, gridPos.x];
        // Only walkable tiles
        return (tile == 0 || tile == 5 || tile == 6);
    }

    private Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(
            mapOrigin.x + g.x * cellSize,
            mapOrigin.y - g.y * cellSize,
            transform.position.z
        );
    }

    private Vector2Int WorldToGrid(Vector3 w)
    {
        return new Vector2Int(
            Mathf.RoundToInt((w.x - mapOrigin.x) / cellSize),
            Mathf.RoundToInt((mapOrigin.y - w.y) / cellSize)
        );
    }

    //=========================================
    // COLLISIONS
    //=========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (CurrentState == GhostState.Normal)
            {
                // GameManager.Instance.PlayerDied();
            }
            else if (CurrentState == GhostState.Scared || CurrentState == GhostState.Recovering)
            {
                Debug.Log($"{name} was eaten!");
                GameManager.Instance.AddScore(300);
                SetState(GhostState.Dead);
            }
        }
    }
}
