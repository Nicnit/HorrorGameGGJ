using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        // Check for left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    Debug.Log($"<color=green>Clicked UI Element:</color> <b>{result.gameObject.name}</b> " +
                              $"| Layer: {result.gameObject.layer} " +
                              $"| Sort Order: {result.sortingOrder}", result.gameObject);
                }
            }
            else
            {
                Debug.Log("<color=yellow>No UI element detected under mouse.</color>");
            }
        }
    }
}