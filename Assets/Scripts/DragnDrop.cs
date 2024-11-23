using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragnDrop : MonoBehaviour
{
    Vector3 offset;
    Vector3 truePos;
    float gridSize = 1;
    
    private void OnMouseDown()
    {
        offset = transform.position - MouseworldPosition();
    }
    private void OnMouseDrag()
    {
        //transform.position = (MouseworldPosition() + offset);
        truePos.x = Mathf.Round((MouseworldPosition() + offset).x)+0.5f;
        truePos.y = Mathf.Round((MouseworldPosition() + offset).y);
        transform.position = truePos;
    }
    Vector3 MouseworldPosition()
    {
        var mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}
