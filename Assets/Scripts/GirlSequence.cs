using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GirlSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bear;
    [SerializeField] private Transform pouch;
    [SerializeField] private GameObject toBeContinue;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float turnSpeed = 180f;

    [Header("Timing")]
    [SerializeField] private float delayAfterPickStarts = 1f;
    [SerializeField] private float liftHeight = 1f;
    [SerializeField] private float liftDuration = 2f;
    [SerializeField] private float fadeDuration = 2f;

    private bool started;

    // These must match the state names on Base Layer in the Animator window.
    private static readonly int WalkState =
        Animator.StringToHash("Base Layer.Walk");

    private static readonly int PickState =
        Animator.StringToHash("Base Layer.Pick");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            Debug.LogError("No Animator found on Girl or its children.", this);
            enabled = false;
            return;
        }

        // This script moves Girl's Transform.
        animator.applyRootMotion = false;

        // Start with the UI hidden and transparent.
        if (toBeContinue != null)
        {
            CanvasGroup group = toBeContinue.GetComponent<CanvasGroup>();

            if (group != null)
                group.alpha = 0f;

            toBeContinue.SetActive(false);
        }
    }

    private void Update()
    {
        if (started || Keyboard.current == null)
            return;

        if (!Keyboard.current.kKey.wasPressedThisFrame)
            return;

        if (bear == null || pouch == null)
        {
            Debug.LogError("Assign Bear and Pouch in the GirlSequence Inspector.", this);
            return;
        }

        if (!animator.HasState(0, WalkState))
        {
            Debug.LogError(
                "Walk state not found. Check its name and that it is on Base Layer.",
                animator);
            return;
        }

        if (!animator.HasState(0, PickState))
        {
            Debug.LogError(
                "Pick state not found. Check its name and that it is on Base Layer.",
                animator);
            return;
        }

        started = true;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        animator.Play(WalkState, 0, 0f);

        yield return MoveTo(new Vector3(-421f, -82f, -145f));
        yield return TurnTo(Quaternion.Euler(0f, 90f, 0f));
        yield return MoveTo(new Vector3(-311f, -82f, -185f));

        animator.Play(PickState, 0, 0f);

        // Wait one second after Pick starts, then lift the objects.
        yield return new WaitForSeconds(delayAfterPickStarts);
        yield return LiftObjects();

        // Activate ToBeContinue after the lift, then fade it in.
        yield return ShowToBeContinue();
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        float speed = Mathf.Max(0.01f, moveSpeed);

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);

            yield return null;
        }

        transform.position = target;
    }

    private IEnumerator TurnTo(Quaternion target)
    {
        float speed = Mathf.Max(0.01f, turnSpeed);

        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                speed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = target;
    }

    private IEnumerator LiftObjects()
    {
        Vector3 bearStart = bear.position;
        Vector3 pouchStart = pouch.position;

        float duration = Mathf.Max(0.01f, liftDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 offset = Vector3.up * (liftHeight * progress);

            bear.position = bearStart + offset;
            pouch.position = pouchStart + offset;

            yield return null;
        }

        bear.position = bearStart + Vector3.up * liftHeight;
        pouch.position = pouchStart + Vector3.up * liftHeight;
    }

    private IEnumerator ShowToBeContinue()
    {
        if (toBeContinue == null)
        {
            Debug.LogWarning("Assign ToBeContinue in the GirlSequence Inspector.", this);
            yield break;
        }

        CanvasGroup group = toBeContinue.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError(
                "Add a CanvasGroup component to the ToBeContinue GameObject.",
                toBeContinue);

            toBeContinue.SetActive(true);
            yield break;
        }

        group.alpha = 0f;
        toBeContinue.SetActive(true);

        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        group.alpha = 1f;
    }
}
