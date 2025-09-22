using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

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

    void Start()
    {
        Destroy(manualLevelRoot.gameObject);
        GenerateLevel();
    }

    void GenerateLevel()
    {
        BuildQuadrant(levelMap, 0f, 0f);

        
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        int[,] right = MirrorHorizontal(levelMap);
        BuildQuadrant(right, cols * cellSize, 0f);

        int[,] bottomL = MirrorVertical(levelMap, skipCenterRowOnVerticalMirror);
        float bottomYOffset = -(bottomL.GetLength(0)) * cellSize - cellSize;
        BuildQuadrant(bottomL, 0f, bottomYOffset);
  
        int[,] bottomR = MirrorVertical(right, skipCenterRowOnVerticalMirror);
        BuildQuadrant(bottomR, cols * cellSize, bottomYOffset);
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
                        Instantiate(emptyPrefab, position, Quaternion.identity, wallsRoot);

                    Instantiate(tileType == 5 ? pelletPrefab : powerPelletPrefab, position, Quaternion.identity, pelletsRoot);
                    continue;
                }

                if (tileType == 0 && emptyPrefab == null) continue;

                Quaternion rot = DetermineRotation(tileType, row, col, map, rows, cols);
                GameObject prefab = PrefabFor(tileType);
                if (prefab == null) continue;

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

    int GetTile(int[,] map, int rows, int cols, int r, int c)
    {
        if (r < 0 || c < 0 || r >= rows || c >= cols)
            return -1;
        return map[r, c];
    }

    Quaternion DetermineRotation(int code, int r, int c, int[,] map, int rows, int cols)
    {
        if (code <= 0) return Quaternion.identity;

        bool IsConn(int v) => (v == 1 || v == 2 || v == 3 || v == 4 || v == 7 || v == 8);

        int up = GetTile(map, rows, cols, r - 1, c);
        int down = GetTile(map, rows, cols, r + 1, c);
        int left = GetTile(map, rows, cols, r, c - 1);
        int right = GetTile(map, rows, cols, r, c + 1);

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
                    int lr = (L ? 1 : 0) + (R ? 1 : 0);
                    int ud = (U ? 1 : 0) + (D ? 1 : 0);
                    rot = (ud > lr) ? 90f : 0f;
                    break;
                }

            case 1:
            case 3:
                if (U && L) rot = 0f;
                else if (U && R) rot = 270f;
                else if (D && R) rot = 180f;
                else if (D && L) rot = 90f;
                break;

            default:
                rot = 0f;
                break;
        }

        return Quaternion.Euler(0f, 0f, rot);
    }


    int[,] MirrorHorizontal(int[,] src)
    {
        int rows = src.GetLength(0);
        int cols = src.GetLength(1);
        int[,] dst = new int[rows, cols];

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
                dst[row, col] = src[row, cols - 1 - col];

        return dst;
    }

    int[,] MirrorVertical(int[,] src, bool skipFirstRowOfMirrored)
    {
        int rows = src.GetLength(0);
        int cols = src.GetLength(1);
        int outRows = skipFirstRowOfMirrored ? rows - 1 : rows;

        int[,] dst = new int[outRows, cols];

        // Mirror rows top->bottom
        // If skipping: drop the first row of the mirrored result to avoid doubling the middle row
        int startR = skipFirstRowOfMirrored ? 1 : 0;

        for (int i = 0; i < outRows; i++)
        {
            int srcR = rows - 1 - (i + startR);
            for (int col = 0; col < cols; col++)
                dst[i, col] = src[srcR, col];
        }
        return dst;
    }
}