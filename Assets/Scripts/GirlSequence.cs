using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GirlSequence : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float turnSpeed = 180f;

    private bool started;

    // These must match the state names and layer in the Animator window.
    private static readonly int WalkState =
        Animator.StringToHash("Base Layer.Walk");

    private static readonly int PickState =
        Animator.StringToHash("Base Layer.Pick");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("No Animator found on Girl or its children.", this);
            enabled = false;
            return;
        }

        // The script controls the character's movement.
        animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (started || Keyboard.current == null)
            return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (!animator.HasState(0, WalkState))
            {
                Debug.LogError(
                    "Walk state not found. Check that the Animator state is named Walk on Base Layer.",
                    animator);
                return;
            }

            if (!animator.HasState(0, PickState))
            {
                Debug.LogError(
                    "Pick state not found. Check that the Animator state is named Pick on Base Layer.",
                    animator);
                return;
            }

            started = true;
            StartCoroutine(RunSequence());
        }
    }

    private IEnumerator RunSequence()
    {
        animator.Play(WalkState, 0, 0f);

        // Move from (-421, -82, 16) toward (-421, -82, -145).
        yield return MoveTo(new Vector3(-421f, -82f, -145f));

        // Turn to world rotation Y = 90 degrees.
        yield return TurnTo(Quaternion.Euler(0f, 90f, 0f));

        // Move to (-311, -82, -145).
        yield return MoveTo(new Vector3(-311f, -82f, -180f));

        animator.Play(PickState, 0, 0f);
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = target;
    }

    private IEnumerator TurnTo(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = target;
    }
}
