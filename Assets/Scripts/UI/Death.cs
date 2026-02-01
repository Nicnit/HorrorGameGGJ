using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
{
    public Animator DeathAnimation;
    public TextMeshProUGUI textMeshProUGUI;

    IEnumerator WaitForAnimation(Animator animator, int layer = 0)
    {

        // Wait until the animator enters the state
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime > 0f
        );

        // Wait until the animation finishes
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime >= 1f
            && !animator.IsInTransition(layer)
        );

        textMeshProUGUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(5f);

        SceneManager.LoadScene("MainMenu");
    }

    public void PlayDeath()
    {
        DeathAnimation.Play("Death");
        StartCoroutine(WaitForAnimation(DeathAnimation));

    }

}
