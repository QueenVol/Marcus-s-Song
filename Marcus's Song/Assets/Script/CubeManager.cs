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
        List<CellFace> result = new List<CellFace>();
        Vector3Int c = face.coordinates;

        List<Vector3Int> offsets = new List<Vector3Int>();

        if (face.normalDir == Vector3.up)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    offsets.Add(new Vector3Int(dx, 0, dz));
                }
            if (c.x == 0) offsets.AddRange(new Vector3Int[] { new Vector3Int(-1, 0, -1), new Vector3Int(-1, 0, 0), new Vector3Int(-1, 0, 1) });
            if (c.x == size - 1) offsets.AddRange(new Vector3Int[] { new Vector3Int(1, 0, -1), new Vector3Int(1, 0, 0), new Vector3Int(1, 0, 1) });
            if (c.z == 0) offsets.AddRange(new Vector3Int[] { new Vector3Int(-1, 0, -1), new Vector3Int(0, 0, -1), new Vector3Int(1, 0, -1) });
            if (c.z == size - 1) offsets.AddRange(new Vector3Int[] { new Vector3Int(-1, 0, 1), new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1) });
        }

        foreach (Vector3Int off in offsets)
        {
            Vector3Int neighborCoord = c + off;
            if (faceMap.ContainsKey(neighborCoord))
            {
                foreach (CellFace other in faceMap[neighborCoord])
                {
                    if (other.normalDir == face.normalDir)
                        result.Add(other);
                }
            }
        }

        return result;
    }
}
