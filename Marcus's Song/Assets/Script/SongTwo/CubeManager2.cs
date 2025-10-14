using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager2 : MonoBehaviour
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
    public TMPro.TextMeshProUGUI mineCountText;
    public float rotationDuration = 0.3f;
    [HideInInspector] public bool isRotating = false;

    private int remainingMines;

    [HideInInspector] public List<CellFace2> faces = new List<CellFace2>();
    private Dictionary<Vector3Int, List<CellFace2>> faceMap = new Dictionary<Vector3Int, List<CellFace2>>();

    void Start()
    {
        GenerateCubeFaces();
        remainingMines = mineCount;
        UpdateMineCounter();
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
                        faceMap[coord] = new List<CellFace2>();

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

        CellFace2 cf = faceObj.GetComponent<CellFace2>();
        cf.coordinates = coord;
        cf.normalDir = normal;

        faces.Add(cf);
        faceMap[coord].Add(cf);
    }

    public void GenerateMinesExcept(CellFace2 firstClicked)
    {
        List<CellFace2> available = new List<CellFace2>(faces);

        List<CellFace2> forbidden = GetNeighborFaces(firstClicked);
        forbidden.Add(firstClicked);

        foreach (CellFace2 f in forbidden)
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

    public void CalculateAdjacentNumbers()
    {
        foreach (CellFace2 face in faces)
        {
            if (face.isMine) continue;

            int count = 0;
            foreach (CellFace2 neighbor in GetNeighborFaces(face))
            {
                if (neighbor.isMine) count++;
            }
            face.adjacentMines = count;
        }
    }

    public List<CellFace2> GetNeighborFaces(CellFace2 face)
    {
        List<CellFace2> neighbors = new List<CellFace2>();
        Vector3Int c = face.coordinates;
        Vector3 n = face.normalDir;
        float edgeThreshold = 1.06f;
        float cornerThreshold = 1.50f;

        foreach (CellFace2 other in faces)
        {
            if (other == face) continue;

            Vector3 diff = other.transform.position - face.transform.position;
            float dist = diff.magnitude;

            if (Mathf.Approximately(Vector3.Dot(n, other.normalDir), 0f))
            {
                if (dist <= edgeThreshold) neighbors.Add(other);
                else if (dist <= cornerThreshold) neighbors.Add(other);
            }
            else if (other.normalDir == n)
            {
                Vector3Int offset = other.coordinates - c;
                int ax = Mathf.Abs(offset.x);
                int ay = Mathf.Abs(offset.y);
                int az = Mathf.Abs(offset.z);
                if (Mathf.Max(ax, Mathf.Max(ay, az)) == 1)
                    neighbors.Add(other);
            }
        }

        HashSet<CellFace2> unique = new HashSet<CellFace2>(neighbors);
        return new List<CellFace2>(unique);
    }

    public void ManualRotate(CellFace2 cell, Vector3 axis, bool clockwise)
    {
        if (isRotating) return;

        int axisIndex = 0;
        int layerIndex = 0;
        if (axis == Vector3.right) { axisIndex = 0; layerIndex = cell.coordinates.x; }
        else if (axis == Vector3.up) { axisIndex = 1; layerIndex = cell.coordinates.y; }
        else { axisIndex = 2; layerIndex = cell.coordinates.z; }

        StartCoroutine(RotateLayerCoroutine(axisIndex, layerIndex, clockwise));
    }

    private IEnumerator RotateLayerCoroutine(int axis, int layerIndex, bool clockwise)
    {
        isRotating = true;

        GameObject rotationGroup = new GameObject("RotationGroup");
        rotationGroup.transform.parent = cubeParent;

        List<CellFace2> layer = new List<CellFace2>();
        foreach (CellFace2 f in faces)
        {
            if ((axis == 0 && f.coordinates.x == layerIndex) ||
                (axis == 1 && f.coordinates.y == layerIndex) ||
                (axis == 2 && f.coordinates.z == layerIndex))
            {
                layer.Add(f);
                f.transform.SetParent(rotationGroup.transform, true);
            }
        }

        Vector3 rotAxis = (axis == 0) ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);
        float targetAngle = clockwise ? -90f : 90f;
        Quaternion startRot = rotationGroup.transform.rotation;
        Quaternion endRot = startRot * Quaternion.AngleAxis(targetAngle, rotAxis);

        float t = 0;
        while (t < rotationDuration)
        {
            t += Time.deltaTime;
            rotationGroup.transform.rotation = Quaternion.Slerp(startRot, endRot, t / rotationDuration);
            yield return null;
        }

        rotationGroup.transform.rotation = endRot;

        foreach (CellFace2 f in layer)
        {
            f.transform.SetParent(cubeParent, true);
        }

        ApplyRotationLogic(axis, layerIndex, clockwise);
        Destroy(rotationGroup);
        isRotating = false;
    }

    private static readonly Vector3Int[] CardinalAxes = {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        Vector3Int.forward, Vector3Int.back
    };

    private Vector3Int SnapToCardinal(Vector3 v)
    {
        Vector3Int best = Vector3Int.up;
        float bestDot = -1f;
        foreach (var ax in CardinalAxes)
        {
            float d = Vector3.Dot(v.normalized, ax);
            if (d > bestDot) { bestDot = d; best = ax; }
        }
        return best;
    }

    private void ApplyRotationLogic(int axis, int layerIndex, bool clockwise)
    {
        Vector3 rotAxis = (axis == 0) ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);
        float angle = clockwise ? 90f : -90f;
        Quaternion rot = Quaternion.AngleAxis(angle, rotAxis);

        int s = size - 1;
        float offset = (size - 1) / 2f;

        foreach (CellFace2 f in faces)
        {
            Vector3Int c = f.coordinates;
            if (!((axis == 0 && c.x == layerIndex) ||
                  (axis == 1 && c.y == layerIndex) ||
                  (axis == 2 && c.z == layerIndex))) continue;

            Vector3Int newCoord;
            if (axis == 0) newCoord = clockwise ? new Vector3Int(c.x, c.z, s - c.y) : new Vector3Int(c.x, s - c.z, c.y);
            else if (axis == 1) newCoord = clockwise ? new Vector3Int(s - c.z, c.y, c.x) : new Vector3Int(c.z, c.y, s - c.x);
            else newCoord = clockwise ? new Vector3Int(c.y, s - c.x, c.z) : new Vector3Int(s - c.y, c.x, c.z);

            f.coordinates = newCoord;
            f.normalDir = SnapToCardinal(rot * f.normalDir);

            Vector3 basePos = new Vector3(newCoord.x - offset, newCoord.y - offset, newCoord.z - offset);
            Vector3 centerDir = basePos.normalized;
            if (Vector3.Dot(f.normalDir, centerDir) < 0)
                f.normalDir = -f.normalDir;

            f.transform.localPosition = basePos + f.normalDir * 0.5f;
            f.transform.rotation = Quaternion.FromToRotation(Vector3.up, f.normalDir);
            f.RefreshTextTransform();
        }

        CalculateAdjacentNumbers();
        foreach (CellFace2 f in faces)
        {
            if (f.isRevealed && !f.isMine)
            {
                f.text.text = f.adjacentMines > 0 ? f.adjacentMines.ToString() : "";
                f.text.color = f.GetNumberColor(f.adjacentMines);
            }
        }
    }

    public void UpdateMineCounter()
    {
        if (mineCountText != null)
            mineCountText.text = $"Mines: {remainingMines}";
    }

    public void AdjustMineCount(bool flagged)
    {
        if (flagged) remainingMines--;
        else remainingMines++;
        UpdateMineCounter();
    }

    public void CheckWin()
    {
        foreach (var f in faces)
        {
            if (!f.isMine && !f.isRevealed) return;
        }
        GameOver(true);
    }

    public void GameOver(bool win)
    {
        foreach (var f in faces)
        {
            if (f.isMine)
            {
                f.text.text = "[  ]";
                f.rend.material.color = Color.red;
            }
        }
        endPanel.SetActive(true);
        backgroundMusic.Stop();
        if (win) winSound.Play(); else loseSound.Play();
        resultText.text = win ? "You Win!" : "Kaboom!";
    }
}
