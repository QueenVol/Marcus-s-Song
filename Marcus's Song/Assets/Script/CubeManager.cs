using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public int size = 9;
    public int mineCount = 100;
    public GameObject facePrefab;
    public Transform cubeParent;
    public GameObject endPanel;
    public TMPro.TextMeshProUGUI resultText;
    public AudioSource backgroundMusic;
    public AudioSource loseSound;
    public AudioSource winSound;
    public bool minesGenerated = false;

    [HideInInspector] public List<CellFace> faces = new List<CellFace>();
    private Dictionary<Vector3Int, List<CellFace>> faceMap = new Dictionary<Vector3Int, List<CellFace>>();

    void Start()
    {
        GenerateCubeFaces();
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

    public void GenerateMinesExcept(CellFace firstClicked)
    {
        List<CellFace> available = new List<CellFace>(faces);

        List<CellFace> forbidden = GetNeighborFaces(firstClicked);
        forbidden.Add(firstClicked);

        foreach (CellFace f in forbidden)
            available.Remove(f);

        int placed = 0;
        while (placed < mineCount && available.Count > 0)
        {
            int index = Random.Range(0, available.Count);
            available[index].isMine = true;
            available.RemoveAt(index);
            placed++;
        }

        minesGenerated = true;
        CalculateAdjacentNumbers();
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
        Vector3 n = nInt;
        float edgeThreshold = 1.06f;
        float cornerThreshold = 1.50f;

        foreach (CellFace other in faces)
        {
            if (other == face) continue;

            Vector3Int otherNInt = Vector3Int.RoundToInt(other.normalDir);
            Vector3 otherN = otherNInt;
            Vector3 diff = other.transform.position - face.transform.position;
            float dist = diff.magnitude;

            if (otherNInt == nInt)
            {
                Vector3Int offset = other.coordinates - c;
                if (Vector3.Dot(offset, n) == 0)
                {
                    int ax = Mathf.Abs(offset.x);
                    int ay = Mathf.Abs(offset.y);
                    int az = Mathf.Abs(offset.z);
                    if (Mathf.Max(ax, Mathf.Max(ay, az)) == 1)
                        neighbors.Add(other);
                }
                continue;
            }

            if (Mathf.Approximately(Vector3.Dot(n, otherN), 0f))
            {
                if (dist <= edgeThreshold)
                {
                    neighbors.Add(other);
                }
                else if (dist <= cornerThreshold)
                {
                    neighbors.Add(other);
                }
                continue;
            }
        }

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

    public void CheckWin()
    {
        foreach (CellFace f in faces)
        {
            if (!f.isMine && !f.isRevealed)
                return;
        }

        GameOver(true);
    }

    public void GameOver(bool win)
    {
        foreach (CellFace f in faces)
        {
            if (f.isMine && !f.isRevealed)
            {
                f.text.text = "[  ]";
                f.rend.material.color = Color.red;
            }
        }

        endPanel.SetActive(true);
        backgroundMusic.Stop();
        if (win)
        {
            winSound.Play();
        }
        else
        {
            loseSound.Play();
        }
        resultText.text = win ? "You Win" : "Kaboom!";
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
