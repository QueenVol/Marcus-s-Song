using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationController : MonoBehaviour
{
    public CubeManager2 cubeManager;

    private CellFace2 selectedCell;

    void Update()
    {
        if (cubeManager == null || cubeManager.isRotating) return;

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            CellFace2 cell = hit.collider.GetComponent<CellFace2>();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (selectedCell == cell)
                {
                    selectedCell.rend.material.color = selectedCell.isRevealed ? Color.white : Color.gray;
                    selectedCell = null;
                    return;
                }

                if (selectedCell != null)
                {
                    selectedCell.rend.material.color = selectedCell.isRevealed ? Color.white : Color.gray;
                    selectedCell = null;
                }

                if (cell != null)
                {
                    selectedCell = cell;
                    cell.rend.material.color = Color.green;
                }
            }
        }

        if (selectedCell == null) return;

        if (Input.GetKeyDown(KeyCode.W))
            cubeManager.ManualRotate(selectedCell, Vector3.right, false);
        else if (Input.GetKeyDown(KeyCode.S))
            cubeManager.ManualRotate(selectedCell, Vector3.right, true);
        else if (Input.GetKeyDown(KeyCode.A))
            cubeManager.ManualRotate(selectedCell, Vector3.up, false);
        else if (Input.GetKeyDown(KeyCode.D))
            cubeManager.ManualRotate(selectedCell, Vector3.up, true);
    }
}
