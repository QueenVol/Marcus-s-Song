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

    public Renderer rend;
    public TextMeshPro text;

    private CubeManager cubeManager;

    void Start()
    {
        rend = GetComponent<Renderer>();
        cubeManager = FindObjectOfType<CubeManager>();

        text.transform.SetParent(null);
        text.transform.position = transform.position + normalDir * 0.05f;
        SetTextRotation();

        rend.material.color = Color.gray;
        text.text = "";
    }

    void SetTextRotation()
    {
        if (normalDir == Vector3.up) text.transform.rotation = Quaternion.Euler(90, 0, 0);
        else if (normalDir == Vector3.down) text.transform.rotation = Quaternion.Euler(-90, 0, 0);
        else if (normalDir == Vector3.left) text.transform.rotation = Quaternion.Euler(0, 90, 0);
        else if (normalDir == Vector3.right) text.transform.rotation = Quaternion.Euler(0, -90, 0);
        else if (normalDir == Vector3.forward) text.transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (normalDir == Vector3.back) text.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0)) Reveal();
        else if (Input.GetMouseButtonDown(1)) ToggleFlag();
    }

    public void Reveal()
    {
        if (isRevealed || isFlagged) return;
        isRevealed = true;

        if (isMine)
        {
            rend.material.color = Color.red;
            text.text = "[  ]";
            text.color = Color.black;

            cubeManager.GameOver(false);
            return;
        }
        else
        {
            rend.material.color = Color.white;
            text.text = adjacentMines > 0 ? adjacentMines.ToString() : "";
            text.color = GetNumberColor(adjacentMines);

            cubeManager.CheckWin();

            if (adjacentMines == 0)
            {
                foreach (CellFace neighbor in cubeManager.GetNeighborFaces(this))
                    if (!neighbor.isRevealed && !neighbor.isMine)
                        neighbor.Reveal();
            }
        }
    }

    public void ToggleFlag()
    {
        if (isRevealed) return;
        isFlagged = !isFlagged;
        rend.material.color = isFlagged ? Color.yellow : Color.gray;
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
