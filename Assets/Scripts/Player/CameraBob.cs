using Unity.VisualScripting;
using UnityEngine;

public class CameraBob : MonoBehaviour
{
    [Header("Main Bob Settings")]
    public bool shouldBob;
    public float frequencyMult = 1f;
    public float amplitudeMult = 1f;
    [Header("Fine-tune")]
    [SerializeField] private float normalFrequency;
    [SerializeField] private float normalAmplitude;
    [SerializeField] private float horizontalMult;
    [SerializeField] private float transitionSpeed;
    [SerializeField] private PlayerController pc;
    [SerializeField] private float sprintFrequency;
    [SerializeField] private float sprintAmplitude;
    private Vector3 startPos;
    private float t = Mathf.PI/2;

    private void Start()
    {
        startPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldBob && pc.isOnGround())
        {
            float currFrequency = (pc.IsInSprint() ? sprintFrequency : normalFrequency) * frequencyMult;
            float currAmplitude = (pc.IsInSprint() ? sprintAmplitude : normalAmplitude) * amplitudeMult;

            t += currFrequency * Time.deltaTime;
            float bobX = Mathf.Cos(t) * currAmplitude * horizontalMult;
            float bobY = Mathf.Abs(Mathf.Sin(t) * currAmplitude) + startPos.y;
            Vector3 targetPos = new Vector3(bobX, bobY, startPos.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, transitionSpeed * Time.deltaTime);
        }
        else if (transform.localPosition != startPos)
        {
            // return to startPos if should not bob
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, transitionSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, startPos) < 0.05)
                transform.localPosition = startPos;
        }

        if (t > Mathf.PI * 2) 
        {
            t = 0;
        }
    }
}
