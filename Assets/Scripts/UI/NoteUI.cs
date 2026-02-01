using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteUI : MonoBehaviour
{
    public GameObject noteObject;
    protected InputAction isInteracting;
    public bool isReaded = false;

    protected void Awake()
    {
        isInteracting = InputSystem.actions.FindAction("ItemInteract");
        isInteracting.Enable();
    }

    protected virtual void Update()
    {
        // If UI showing and player uses Interact button, do interaction
        // Also if no other UI is showing
        if (isInteracting != null && isInteracting.triggered) // TODO check if MAIN UI is activated via UI manager
        {
            //
            //
        }
    }

    public void ShowNote() {
        noteObject.SetActive(true);
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        
        noteObject.GetComponentInChildren<TextMeshProUGUI>().SetText(pc.NoteList[pc.CurrentNote]);
        pc.CurrentNote++;

        NoticeUI notice = FindFirstObjectByType<NoticeUI>();
        notice.ToggleNotice(false, "");

        StartCoroutine(WaitForInteract());
    }

    private IEnumerator WaitForInteract()
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => isInteracting.triggered);

        noteObject.SetActive(false);
    }
}
