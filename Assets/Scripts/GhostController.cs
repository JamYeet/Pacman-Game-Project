using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GhostState { InHouse, LeavingHouse, Normal, Scared, Recovering, Dead }
public class GhostController : MonoBehaviour
{
    public GhostState CurrentState { get; private set; } = GhostState.Normal;

    public float normalSpeed = 4.5f;
    public float scaredSpeed = 2.5f;
    public float deadSpeed = 2.5f;

    public int ghostID = 1; // 1–4
    public Transform player;

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

    private Vector3 spawnPos;
    private Vector3 homePos;

    private float lerpProgress = 0f;
    private bool isMoving = false;
    private bool returningHome = false;

    private Vector2Int exitDir;
    private Vector2Int doorPos;
    private bool reachedDoor = false;

    private LevelGenerator levelGen;

    private void Start()
    {
        animator = GetComponent<Animator>();
        levelGen = LevelGenerator.Instance;
        currentGrid = WorldToGrid(transform.position);
        spawnPos = transform.position;

        // Assign exit direction and door target based on ghost ID
        switch (ghostID)
        {
            case 1:
                exitDir = Vector2Int.up;
                doorPos = FindNearestTileOfType(8, Vector2Int.up); // top door
                homePos = spawnPos + new Vector3(0f, 0f, 0f);
                break;
            case 2:
                exitDir = Vector2Int.down;
                doorPos = FindNearestTileOfType(8, Vector2Int.down); // bottom door
                homePos = spawnPos + new Vector3(0f, 0f, 0f);
                break;
            case 3:
                exitDir = Vector2Int.up;
                doorPos = FindNearestTileOfType(8, Vector2Int.up);
                homePos = spawnPos + new Vector3(0f, 0f, 0f);
                break;
            case 4:
                exitDir = Vector2Int.down;
                doorPos = FindNearestTileOfType(8, Vector2Int.down);
                homePos = spawnPos + new Vector3(0f,  0f, 0f);
                break;
        }

        SetState(GhostState.InHouse);
        StartCoroutine(WaitThenLeave());
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case GhostState.Dead:
                MoveToHome();
                return;

            case GhostState.InHouse:
                // Wait or bounce a little before leaving
                if (!isMoving)
                    StartCoroutine(WaitThenLeave());
                return;

            case GhostState.LeavingHouse:
                MoveOutOfHouse();
                return;
        }

        // Normal movement handling
        if (!isMoving)
            ChooseNextDirection();
        else
            MoveLerp();

        // Update animator
        animator.SetFloat("MoveX", moveDir.x);
        animator.SetFloat("MoveY", moveDir.y);
    }

    public IEnumerator WaitThenLeave()
    {
        isMoving = true; // prevent multiple coroutines
        yield return new WaitForSeconds(Random.Range(1f, 2f)); // add variety
        SetState(GhostState.LeavingHouse);
        isMoving = false;
    }

    private void MoveOutOfHouse()
    {
        if (reachedDoor == false)
        {
            // Move toward door
            Vector3 doorWorld = GridToWorld(doorPos);
            transform.position = Vector3.MoveTowards(transform.position, doorWorld, normalSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, doorWorld) < 0.05f)
            {
                reachedDoor = true;

            }
        }
        else
        {
            // Move out through the door direction (up or down)
            Vector3 outPos = transform.position + (Vector3)((Vector2)exitDir * cellSize * 1.5f);
            transform.position = Vector3.MoveTowards(transform.position, outPos, normalSpeed * Time.deltaTime);

            // Check if we’re now standing on a walkable corridor (tile == 0)
            Vector2Int newGrid = WorldToGrid(transform.position);
            int tile = levelGen.fullMap[newGrid.y, newGrid.x];

            if (tile == 0 || tile == 5 || tile == 6)
            {
                currentGrid = newGrid;
                reachedDoor = false;
                SetState(GhostState.Normal);
            }
        }
    }

    private Vector2Int FindNearestTileOfType(int tileType, Vector2Int direction)
    {
        int[,] map = levelGen.fullMap;
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        int bestDist = int.MaxValue;
        Vector2Int bestPos = currentGrid;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (map[y, x] == tileType)
                {
                    Vector2Int tile = new Vector2Int(x, y);
                    // Filter by direction (so up ghosts don’t pick bottom doors)
                    if ((direction == Vector2Int.up && tile.y < currentGrid.y) ||
                        (direction == Vector2Int.down && tile.y > currentGrid.y))
                    {
                        int dist = Mathf.Abs(tile.x - currentGrid.x) + Mathf.Abs(tile.y - currentGrid.y);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestPos = tile;
                        }
                    }
                }
            }
        }
        return bestPos;
    }

    //=========================================
    // STATE MANAGEMENT
    //=========================================
    public void SetState(GhostState newState)
    {
        if (CurrentState == newState) return;

        if ((CurrentState == GhostState.InHouse || CurrentState == GhostState.LeavingHouse) &&
        (newState == GhostState.Scared || newState == GhostState.Recovering))
        {
            Debug.Log("Ignored");
            // Just ignore this request
            return;
        }

        CurrentState = newState;

        animator.SetBool("IsScared", false);
        animator.SetBool("IsRecovering", false);

        switch (newState)
        {
            case GhostState.InHouse:
                currentSpeed = 0f;
                break;

            case GhostState.LeavingHouse:
                currentSpeed = normalSpeed;
                break;

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
    public void TeleportHome()
    {
        transform.position = homePos;
        ResetMovement();
        SetState(GhostState.InHouse);
    }

    private void MoveToHome()
    {
        // Move toward ghost home position ignoring walls
        transform.position = Vector3.MoveTowards(transform.position, homePos, deadSpeed * Time.deltaTime);
        Debug.Log("Returning Home!");

        if (Vector3.Distance(transform.position, homePos) < 0.1f)
        {
            Debug.Log("I'm Home!");
            returningHome = false;
            Respawn();
        }
    }

    public void Respawn()
    {
        transform.position = homePos;
        ResetMovement();
        animator.SetBool("IsDead", false);

        // Start recovering briefly before leaving again
        StartCoroutine(RecoverAndLeaveHouse());
    }

    private IEnumerator RecoverAndLeaveHouse()
    {
        SetState(GhostState.Recovering);
        yield return new WaitForSeconds(2f); // stay recovering briefly

        // Re-enter house logic like at game start
        SetState(GhostState.InHouse);
        StartCoroutine(WaitThenLeave());
    }

    public void ResetMovement()
    {
        isMoving = false;
        lerpProgress = 0f;
        lastDir = Vector2Int.zero;
        moveDir = Vector2Int.zero;
        currentGrid = WorldToGrid(homePos);
        targetGrid = currentGrid;
        startPos = homePos;
        targetPos = homePos;
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
        // Determine the right direction based on current movement
        Vector2Int right = new Vector2Int(moveDir.y, -moveDir.x);
        Vector2Int left = new Vector2Int(-moveDir.y, moveDir.x);
        Vector2Int forward = moveDir;
        Vector2Int back = -moveDir;

        // Priority order (clockwise wall-following)
        Vector2Int[] priorities = { right, forward, left, back };

        foreach (var dir in priorities)
        {
            if (validDirs.Contains(dir))
                return dir;
        }

        // Fallback
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

        // Bounds check
        if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= cols || gridPos.y >= rows)
            return false;

        int tile = map[gridPos.y, gridPos.x];
        bool isWalkable = (tile == 0 || tile == 5 || tile == 6);
        bool isDoor = (tile == 8);

        // Ghosts leaving house or dead can cross doors
        if (CurrentState == GhostState.LeavingHouse || CurrentState == GhostState.Dead)
            return isWalkable || isDoor;

        return isWalkable;
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
        if (!collision.CompareTag("Player")) return;

        // Dead ghosts: ignore collisions
        if (CurrentState == GhostState.Dead) return;

        if (CurrentState == GhostState.Normal)
        {
            GameManager.Instance.PlayerDied();
            Debug.Log($"{name} hit the player!");
        }
        else if (CurrentState == GhostState.Scared || CurrentState == GhostState.Recovering)
        {
            Debug.Log($"{name} was eaten!");
            GameManager.Instance.AddScore(300);
            GameManager.Instance.PlayGhostDeadMusic();

            SetState(GhostState.Dead);
        }
    }
}
