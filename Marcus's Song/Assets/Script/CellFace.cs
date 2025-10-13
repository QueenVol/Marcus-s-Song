using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CellFace : MonoBehaviour
{
    public Vector3Int coordinates;
    public Vector3 normalDir;
    public bool isMine = false;
    public bool isRevealed = false;
    public bool isFlagged = false;
    public int adjacentMines = 0;

    private Renderer rend;
    public TextMeshPro text;

    private CubeManager cubeManager;

    void Start()
    {
        rend = GetComponent<Renderer>();
        cubeManager = FindObjectOfType<CubeManager>();

        text.transform.localPosition = normalDir * 0.51f;
        text.transform.localRotation = Quaternion.identity;

        if (normalDir == Vector3.up)
            text.transform.Rotate(90, 0, 0);
        else if (normalDir == Vector3.down)
            text.transform.Rotate(-90, 0, 0);
        else if (normalDir == Vector3.left)
            text.transform.Rotate(0, 90, 0);
        else if (normalDir == Vector3.right)
            text.transform.Rotate(0, -90, 0);
        else if (normalDir == Vector3.forward)
            text.transform.Rotate(0, 180, 0);
        else if (normalDir == Vector3.back)
            text.transform.Rotate(0, 0, 0);

        rend.material.color = Color.gray;
        text.text = "";
    }

    void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
            Reveal();
        else if (Input.GetMouseButtonDown(1))
            ToggleFlag();
    }

    public void Reveal()
    {
        if (isRevealed || isFlagged) return;
        isRevealed = true;

        if (isMine)
        {
            rend.material.color = Color.red;
            text.text = "💣";
            text.color = Color.black;
        }
        else
        {
            rend.material.color = Color.white;
            text.text = adjacentMines > 0 ? adjacentMines.ToString() : "";
            text.color = GetNumberColor(adjacentMines);

            if (adjacentMines == 0)
                AutoRevealNeighbors();
        }
    }

    public void ToggleFlag()
    {
        if (isRevealed) return;
        isFlagged = !isFlagged;
        rend.material.color = isFlagged ? Color.yellow : Color.gray;
    }

    void AutoRevealNeighbors()
    {
        foreach (CellFace neighbor in cubeManager.GetNeighborFaces(this))
        {
            if (!neighbor.isRevealed && !neighbor.isMine)
                neighbor.Reveal();
        }
    }

    Color GetNumberColor(int n)
    {
        switch (n)
        {
            case 1: return Color.blue;
            case 2: return Color.green;
            case 3: return Color.red;
            case 4: return new Color(0f, 0f, 0.5f);
            case 5: return new Color(0.5f, 0f, 0f);
            case 6: return Color.cyan;
            case 7: return Color.black;
            case 8: return Color.gray;
            default: return Color.black;
        }
    }
}
