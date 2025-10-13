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
        Vector3Int n = Vector3Int.RoundToInt(face.normalDir);

        // ----------- 第 1 步：同面 8 邻居 -----------
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3Int offset = new Vector3Int(dx, dy, dz);
                    if (offset == Vector3Int.zero) continue;
                    if (Vector3.Dot(offset, n) != 0) continue; // 必须在同一平面内

                    Vector3Int neighborCoord = c + offset;
                    if (!faceMap.ContainsKey(neighborCoord)) continue;

                    foreach (CellFace other in faceMap[neighborCoord])
                        if (other.normalDir == n)
                            neighbors.Add(other);
                }

        // ----------- 第 2 步：检测是否在边或角上 -----------
        int s = size - 1;
        bool onXEdge = (c.x == 0 || c.x == s);
        bool onYEdge = (c.y == 0 || c.y == s);
        bool onZEdge = (c.z == 0 || c.z == s);

        int edgeCount = (onXEdge ? 1 : 0) + (onYEdge ? 1 : 0) + (onZEdge ? 1 : 0);

        // ----------- 第 3 步：如果是边或角，添加相邻面的邻居 -----------
        // 共享边 → 与另一面的法线不同
        if (edgeCount >= 2) // 在角上（同时接触3个面）
        {
            // 检查所有法线方向的组合
            foreach (Vector3Int dir in new Vector3Int[]
            {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
            })
            {
                if (Vector3.Dot(dir, n) != 0) continue; // 排除自己和对面

                Vector3Int neighborCoord = c + dir; // 共享边或角方向
                if (!faceMap.ContainsKey(neighborCoord)) continue;

                foreach (CellFace other in faceMap[neighborCoord])
                {
                    if (Vector3.Dot(other.normalDir, n) < 0) continue; // 排除对面
                    neighbors.Add(other);
                }
            }
        }
        else if (edgeCount == 2) // 在边上
        {
            // 找出与当前面垂直的那一维
            List<Vector3Int> dirs = new List<Vector3Int>();

            if (!onXEdge) dirs.Add(Vector3Int.right);
            if (!onYEdge) dirs.Add(Vector3Int.up);
            if (!onZEdge) dirs.Add(Vector3Int.forward);

            // 只对在边上的两个方向添加跨面连接
            foreach (Vector3Int dir in new Vector3Int[]
            {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
            })
            {
                if (Vector3.Dot(dir, n) != 0) continue;

                Vector3Int neighborCoord = c + dir;
                if (!faceMap.ContainsKey(neighborCoord)) continue;

                foreach (CellFace other in faceMap[neighborCoord])
                {
                    if (Vector3.Dot(other.normalDir, n) < 0) continue;
                    neighbors.Add(other);
                }
            }
        }

        // ----------- 第 4 步：去重 -----------
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
