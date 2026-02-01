using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NoticeUI : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public void ToggleNotice(bool show, string interactableText) {
        textMesh.gameObject.SetActive(show);
        textMesh.SetText(interactableText);
    }
}
