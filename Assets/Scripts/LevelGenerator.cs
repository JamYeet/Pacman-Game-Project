using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    public bool skipCenterRowOnVerticalMirror = true;


    private int[,] levelMap = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7,},      // 15 cols
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4,},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4,},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4,},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3,},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5,},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4,},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3,},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4,},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4,},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3,},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0,},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8,},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0,},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0,},
    };

    void Start()
    {
        Destroy(manualLevelRoot.gameObject);

        //GenerateLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GenerateLevel()
    {
        BuildQuadrant(levelMap, 0f, 0f);

        
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        //
        //int[,] right = MirrorHorizontal(levelMap);
        //BuildQuadrant(right, cols * cellSize, 0f);

        //int[,] bottomL = MirrorVertical(levelMap, skipCenterRowOnVerticalMirror);
        //float bottomYOffset = -(bottomL.GetLength(0)) * cellSize;
        //BuildQuadrant(bottomL, 0f, bottomYOffset);
    
        //int[,] bottomR = MirrorVertical(right, skipCenterRowOnVerticalMirror);
        //BuildQuadrant(bottomR, cols * cellSize, bottomYOffset);
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
                GameObject prefab = PrefabFor(tileType);

                if (prefab != null)
                {
                    // Calculate world position
                    Vector3 position = new Vector3(offsetX + col * cellSize, offsetY - row * cellSize, 0f);

                    GameObject tile = Instantiate(prefab, position, Quaternion.identity);
                    Transform parent = GetParentForTileType(tileType);
                    if (parent != null)
                    {
                        tile.transform.SetParent(parent);
                    }
                    else
                    {
                        tile.transform.SetParent(generatedRoot);
                    }
                }
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

    Transform GetParentForTileType(int tileType)
    {
        switch (tileType)
        {
            case 0: // empty
                return generatedRoot;
            case 1: // outside corner
            case 2: // outside wall
            case 3: // inside corner
            case 4: // inside wall
            case 7: // t-junction
                return wallsRoot;
            case 5: // pellet
            case 6: // power pellet
                return pelletsRoot;
            case 8: // ghost exit
                return specialRoot;
            default:
                return generatedRoot;
        }
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