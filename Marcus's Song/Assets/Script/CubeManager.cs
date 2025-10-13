using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public int size = 9;
    public int mineCount = 100;
    public GameObject facePrefab;
    public Transform cubeParent;

    [HideInInspector] public List<CellFace> faces = new List<CellFace>();
    private Dictionary<Vector3Int, List<CellFace>> faceMap = new Dictionary<Vector3Int, List<CellFace>>();

    void Start()
    {
        GenerateCubeFaces();
        PlaceSurfaceMines();
        CalculateAdjacentNumbers();
    }

    void GenerateCubeFaces()
    {
        float offset = (size - 1) / 2f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    bool isSurface = (x == 0 || x == size - 1 ||
                                      y == 0 || y == size - 1 ||
                                      z == 0 || z == size - 1);
                    if (!isSurface) continue;

                    Vector3Int coord = new Vector3Int(x, y, z);
                    Vector3 pos = new Vector3(x - offset, y - offset, z - offset);

                    if (!faceMap.ContainsKey(coord))
                        faceMap[coord] = new List<CellFace>();

                    if (x == 0) InstantiateFace(pos, coord, Vector3.left);
                    if (x == size - 1) InstantiateFace(pos, coord, Vector3.right);
                    if (y == 0) InstantiateFace(pos, coord, Vector3.down);
                    if (y == size - 1) InstantiateFace(pos, coord, Vector3.up);
                    if (z == 0) InstantiateFace(pos, coord, Vector3.back);
                    if (z == size - 1) InstantiateFace(pos, coord, Vector3.forward);
                }
    }

    void InstantiateFace(Vector3 pos, Vector3Int coord, Vector3 normal)
    {
        Vector3 worldPos = pos + normal * 0.5f;
        GameObject faceObj = Instantiate(facePrefab, worldPos, Quaternion.identity, cubeParent);

        faceObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        faceObj.transform.localScale = Vector3.one * 0.1f;

        CellFace cf = faceObj.GetComponent<CellFace>();
        cf.coordinates = coord;
        cf.normalDir = normal;

        faceObj.name = $"Face_{coord.x}_{coord.y}_{coord.z}_{normal}";

        faces.Add(cf);
        faceMap[coord].Add(cf);
    }

    void PlaceSurfaceMines()
    {
        List<CellFace> allFaces = new List<CellFace>(faces);
        int placed = 0;

        while (placed < mineCount && allFaces.Count > 0)
        {
            int index = Random.Range(0, allFaces.Count);
            allFaces[index].isMine = true;
            allFaces.RemoveAt(index);
            placed++;
        }

        Debug.Log($"放置完成：{placed} 个雷");
    }

    void CalculateAdjacentNumbers()
    {
        foreach (CellFace face in faces)
        {
            if (face.isMine) continue;

            int count = 0;
            foreach (CellFace neighbor in GetNeighborFaces(face))
            {
                if (neighbor.isMine) count++;
            }
            face.adjacentMines = count;
        }
    }

    public List<CellFace> GetNeighborFaces(CellFace face)
    {
        List<CellFace> neighbors = new List<CellFace>();
        Vector3Int c = face.coordinates;
        Vector3 nInt = Vector3Int.RoundToInt(face.normalDir);
        Vector3 n = nInt; // as Vector3 for math
        float edgeThreshold = 1.06f;   // 边的距离阈值（微调可改）
        float cornerThreshold = 1.50f; // 角的距离阈值（微调可改）

        foreach (CellFace other in faces)
        {
            if (other == face) continue;

            Vector3Int otherNInt = Vector3Int.RoundToInt(other.normalDir);
            Vector3 otherN = otherNInt;
            Vector3 diff = other.transform.position - face.transform.position;
            float dist = diff.magnitude;

            // 1) 同一面：法线一致，并且在同一平面内（平面内偏移不为0且每轴偏移在 [-1,1]）
            if (otherNInt == nInt)
            {
                Vector3Int offset = other.coordinates - c;
                // 确保在平面内（offset 与法线的点积为 0）
                if (Vector3.Dot(offset, n) == 0)
                {
                    // 只取平面内相邻格（3x3 去中间）
                    int ax = Mathf.Abs(offset.x);
                    int ay = Mathf.Abs(offset.y);
                    int az = Mathf.Abs(offset.z);
                    // 在平面内时，只有两个轴会有非0值，取 max<=1 且非全0
                    if (Mathf.Max(ax, Mathf.Max(ay, az)) == 1)
                        neighbors.Add(other);
                }
                continue;
            }

            // 2) 跨面：只考虑与当前面垂直的面（排除对面和平行面）
            if (Mathf.Approximately(Vector3.Dot(n, otherN), 0f))
            {
                // 排除“对面”的那一类（比如 n 与 otherN 朝向相反时 dot==0 不会发生，但这里保留判断）
                // 通过距离判断是边还是角：边更近，角更远
                if (dist <= edgeThreshold)
                {
                    // 共享边（或紧邻的跨面格） -> 包含
                    neighbors.Add(other);
                }
                else if (dist <= cornerThreshold)
                {
                    // 可能是共享角的那个跨面格 -> 也包含
                    neighbors.Add(other);
                }
                // 超过 cornerThreshold 的不当邻居都忽略
                continue;
            }

            // 3) 其它情况（例如对面或非常远的格子）都忽略
        }

        // 去重并返回
        HashSet<CellFace> unique = new HashSet<CellFace>(neighbors);
        return new List<CellFace>(unique);
    }

    private List<Vector3Int> GetEdgeCornerOffsets(CellFace face)
    {
        List<Vector3Int> offsets = new List<Vector3Int>();
        int x = face.coordinates.x;
        int y = face.coordinates.y;
        int z = face.coordinates.z;
        int s = size - 1;
        Vector3Int n = Vector3Int.RoundToInt(face.normalDir);

        if (n == Vector3Int.up || n == Vector3Int.down)
        {
            if (x == 0) offsets.Add(new Vector3Int(-1, 0, 0));
            if (x == s) offsets.Add(new Vector3Int(1, 0, 0));
            if (z == 0) offsets.Add(new Vector3Int(0, 0, -1));
            if (z == s) offsets.Add(new Vector3Int(0, 0, 1));

            if (x == 0 && z == 0) offsets.Add(new Vector3Int(-1, 0, -1));
            if (x == 0 && z == s) offsets.Add(new Vector3Int(-1, 0, 1));
            if (x == s && z == 0) offsets.Add(new Vector3Int(1, 0, -1));
            if (x == s && z == s) offsets.Add(new Vector3Int(1, 0, 1));
        }
        else if (n == Vector3Int.left || n == Vector3Int.right)
        {
            if (y == 0) offsets.Add(new Vector3Int(0, -1, 0));
            if (y == s) offsets.Add(new Vector3Int(0, 1, 0));
            if (z == 0) offsets.Add(new Vector3Int(0, 0, -1));
            if (z == s) offsets.Add(new Vector3Int(0, 0, 1));

            if (y == 0 && z == 0) offsets.Add(new Vector3Int(0, -1, -1));
            if (y == 0 && z == s) offsets.Add(new Vector3Int(0, -1, 1));
            if (y == s && z == 0) offsets.Add(new Vector3Int(0, 1, -1));
            if (y == s && z == s) offsets.Add(new Vector3Int(0, 1, 1));
        }
        else if (n == Vector3Int.forward || n == Vector3Int.back)
        {
            if (x == 0) offsets.Add(new Vector3Int(-1, 0, 0));
            if (x == s) offsets.Add(new Vector3Int(1, 0, 0));
            if (y == 0) offsets.Add(new Vector3Int(0, -1, 0));
            if (y == s) offsets.Add(new Vector3Int(0, 1, 0));

            if (x == 0 && y == 0) offsets.Add(new Vector3Int(-1, -1, 0));
            if (x == 0 && y == s) offsets.Add(new Vector3Int(-1, 1, 0));
            if (x == s && y == 0) offsets.Add(new Vector3Int(1, -1, 0));
            if (x == s && y == s) offsets.Add(new Vector3Int(1, 1, 0));
        }

        return offsets;
    }
}
