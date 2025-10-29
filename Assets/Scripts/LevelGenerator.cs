using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;

[DefaultExecutionOrder(-100)]
public class LevelGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform manualLevelRoot;

    public Transform generatedRoot;
    public Transform wallsRoot;
    public Transform pelletsRoot;
    public Transform specialRoot;

    public GameObject emptyPrefab;          // 0
    public GameObject outsideCornerPrefab;  // 1
    public GameObject outsideWallPrefab;    // 2
    public GameObject insideCornerPrefab;   // 3
    public GameObject insideWallPrefab;     // 4
    public GameObject pelletPrefab;         // 5
    public GameObject powerPelletPrefab;    // 6
    public GameObject tJunctionPrefab;      // 7
    public GameObject ghostExitPrefab;      // 8


    public float cellSize = 1f;
    public bool skipCenterRowOnVerticalMirror = false;

    public RuntimeAnimatorController powerPelletAnimatorController;

    public Button exitButton;

    private int[,] levelMap = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
};

    public static LevelGenerator Instance { get; private set; }
    public int[,] fullMap;

    void Start()
    {
        Instance = this;
        Destroy(manualLevelRoot.gameObject);
        GenerateLevel();
        GenerateFullMap();

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => SceneManager.LoadScene("StartScene"));
        }
    }

    void GenerateFullMap()
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        int[,] right = MirrorHorizontal(levelMap);
        int[,] bottomL = MirrorVertical(levelMap);
        int[,] bottomR = MirrorVertical(right);

        int bottomRows = bottomL.GetLength(0);  
        int fullRows = rows + bottomRows;
        int fullCols = cols * 2;

        fullMap = new int[fullRows, fullCols];

        // TL
        CopyQuadrant(levelMap, 0, 0);
        // TR
        CopyQuadrant(right, 0, cols);
        // BL
        CopyQuadrant(bottomL, rows, 0);
        // BR
        CopyQuadrant(bottomR, rows, cols);

        Debug.Log("==== FULL MAP ====");
        string mapOutput = "";
        int fullaRows = fullMap.GetLength(0);
        int fullaCols = fullMap.GetLength(1);

        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                mapOutput += fullMap[r, c].ToString();
            }
            mapOutput += "\n";
        }
        Debug.Log(mapOutput);

    }

    void CopyQuadrant(int[,] src, int destRow, int destCol)
    {
        int rMax = src.GetLength(0);
        int cMax = src.GetLength(1);
        for (int r = 0; r < rMax; r++)
            for (int c = 0; c < cMax; c++)
                fullMap[destRow + r, destCol + c] = src[r, c];
    }

    void GenerateLevel()
    {
        float baseX = -13.5f;
        float baseY = 14.5f;

        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        BuildQuadrant(levelMap, baseX, baseY);

        int[,] right = MirrorHorizontal(levelMap);
        BuildQuadrant(right, baseX + cols * cellSize, baseY);

        int[,] bottomL = MirrorVertical(levelMap);
        float bottomYOffset = baseY - (bottomL.GetLength(0) * cellSize + cellSize);
        BuildQuadrant(bottomL, baseX, bottomYOffset);

        int[,] bottomR = MirrorVertical(right);
        BuildQuadrant(bottomR, baseX + cols * cellSize, bottomYOffset);

        float centerX = baseX + (cols * cellSize);
        float centerY = baseY - (rows * cellSize) + 1f;
        Camera.main.transform.position = new Vector3(centerX, centerY, Camera.main.transform.position.z);
    }

    void BuildQuadrant(int[,] map, float offsetX, float offsetY)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int tileType = map[row, col];
                Vector3 position = new Vector3(offsetX + col * cellSize, offsetY - row * cellSize, 0f);

                if (tileType == 5 || tileType == 6)
                {
                    if (emptyPrefab != null)
                    {
                        Instantiate(emptyPrefab, position, Quaternion.identity, wallsRoot);
                    }

                    GameObject pellet;

                    if (tileType == 5)
                    {
                        pellet = Instantiate(pelletPrefab, position, Quaternion.identity, pelletsRoot);
                        pellet.tag = "Pellet";
                    }
                    else
                    {
                        pellet = Instantiate(powerPelletPrefab, position, Quaternion.identity, pelletsRoot);
                        pellet.transform.localScale = new Vector3(2f, 2f, 2f);
                        pellet.tag = "PowerPellet";

                        Animator animator = pellet.GetComponent<Animator>();
                        if (animator == null)
                            animator = pellet.AddComponent<Animator>();

                        if (powerPelletAnimatorController != null)
                            animator.runtimeAnimatorController = powerPelletAnimatorController;
                    }

                    pellet.GetComponent<SpriteRenderer>().sortingOrder = 1;
                    continue;
                }


                if (tileType == 0 && emptyPrefab == null)
                {
                    continue;
                }

                Quaternion rot = DetermineRotation(tileType, row, col, map, rows, cols);
                GameObject prefab = PrefabFor(tileType);
                if (prefab == null)
                {
                    continue;
                }

                Transform parent = ParentFor(tileType);
                Instantiate(prefab, position, rot, parent);
            }
        }
    }
    GameObject PrefabFor(int index)
    {
        switch (index)
        {
            case 0: return emptyPrefab;
            case 1: return outsideCornerPrefab;
            case 2: return outsideWallPrefab;
            case 3: return insideCornerPrefab;
            case 4: return insideWallPrefab;
            case 5: return pelletPrefab;
            case 6: return powerPelletPrefab;
            case 7: return tJunctionPrefab;
            case 8: return ghostExitPrefab;
            default: return null;
        }
    }

    Transform ParentFor(int code)
    {
        switch (code)
        {
            case 5:
            case 6:
                return pelletsRoot;
            case 8:
                return specialRoot;
            default:
                return wallsRoot;
        }
    }

    int GetTile(int[,] map, int rows, int cols, int row, int col)
    {
        if (row < 0 || col < 0 || row >= rows || col >= cols)
            return -1;
        return map[row, col];
    }

    Quaternion DetermineRotation(int code, int row, int col, int[,] map, int rows, int cols)
    {
        if (code <= 0)
        {
            return Quaternion.identity;
        }

        bool IsConn(int v)
        {
            if (v == 1) return true;
            if (v == 2) return true;
            if (v == 3) return true;
            if (v == 4) return true;
            if (v == 7) return true;
            if (v == 8) return true;;
            return false;
        }

        int up = GetTile(map, rows, cols, row - 1, col);
        int down = GetTile(map, rows, cols, row + 1, col);
        int left = GetTile(map, rows, cols, row, col - 1);
        int right = GetTile(map, rows, cols, row, col + 1);

        bool U = IsConn(up);
        bool D = IsConn(down);
        bool L = IsConn(left);
        bool R = IsConn(right);

        float rot = 0f;

        switch (code)
        {
            case 2:
            case 4:
            case 8:
                {
                    int leftRight = 0;
                    if (L) leftRight += 1;
                    if (R) leftRight += 1;

                    int UpDown = 0;
                    if (U) UpDown += 1;
                    if (D) UpDown += 1;

                    if (UpDown > leftRight)
                    {
                        rot = 90f;
                    }
                    else
                    {
                        rot = 0f;
                    }
                    break;
                }

            case 1:
            case 3:
                {
                    if (U && L)
                    {
                        rot = 0f;
                    }
                    else if (U && R)
                    {
                        rot = 270f;
                    }
                    else if (D && R)
                    {
                        rot = 180f;
                    }
                    else if (D && L)
                    {
                        rot = 90f;
                    }
                    break;
                }

            default:
                {
                    rot = 0f;
                    break;
                }
        }

        return Quaternion.Euler(0f, 0f, rot);
    }


    int[,] MirrorHorizontal(int[,] map)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        int[,] distance = new int[rows, cols];

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
                distance[row, col] = map[row, cols - 1 - col];

        return distance;
    }

    int[,] MirrorVertical(int[,] map)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        int outRows = rows - 1;
        int[,] distance = new int[outRows, cols];

        int startR = 1;

        for (int i = 0; i < outRows; i++)
        {
            int mapRow = rows - 1 - (i + startR);
            for (int col = 0; col < cols; col++)
            {
                distance[i, col] = map[mapRow, col];
            }
        }
        return distance;
    }
}