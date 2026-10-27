using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BearVRMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private Transform rightHandAim;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Walking audio")]
    [SerializeField] private AudioSource walkingAudio;
    [SerializeField] private float walkInputThreshold = 0.1f;

    [Header("Pouch bounce")]
    [SerializeField] private string pouchTag = "Pouch";
    [SerializeField] private float bounceImpulse = 2f;
    [SerializeField] private float upwardBounceRatio = 0.2f;
    [SerializeField] private float movementLockout = 0.3f;
    [SerializeField] private float hitCooldown = 0.45f;

    [Header("Optional pouch impact effects")]
    [SerializeField] private Transform pouchVisual;
    [SerializeField] private AudioSource crinkleAudio;
    [Tooltip("Adjust the axis values to match your pouch's orientation.")]
    [SerializeField]
    private Vector3 squeezeMultiplier =
        new Vector3(1.03f, 1.03f, 0.82f);

    private Rigidbody body;
    private Vector2 moveInput;
    private Vector3 facingDirection;
    private float movementLockoutRemaining;
    private float nextHitTime;

    private Vector3 pouchOriginalScale;
    private Coroutine pouchPulse;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        // Keep Bear upright, but allow turning.
        body.constraints |= RigidbodyConstraints.FreezeRotationX
                          | RigidbodyConstraints.FreezeRotationZ;

        facingDirection =
            Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (facingDirection.sqrMagnitude < 0.001f)
            facingDirection = Vector3.forward;

        if (pouchVisual != null)
            pouchOriginalScale = pouchVisual.localScale;

        if (walkingAudio != null)
        {
            walkingAudio.playOnAwake = false;
            walkingAudio.loop = true;
            walkingAudio.Stop();
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        SetWalkingAudio(false);

        if (pouchPulse != null)
        {
            StopCoroutine(pouchPulse);
            pouchPulse = null;
        }

        if (pouchVisual != null)
            pouchVisual.localScale = pouchOriginalScale;
    }

    private void Update()
    {
        moveInput = moveAction != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        if (rightHandAim != null)
        {
            Vector3 handForward =
                Vector3.ProjectOnPlane(rightHandAim.forward, Vector3.up);

            if (handForward.sqrMagnitude > 0.001f)
                facingDirection = handForward.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(facingDirection, Vector3.up);

            body.MoveRotation(
                Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime
                )
            );
        }

        // Let the physics impulse move Bear before steering resumes.
        if (movementLockoutRemaining > 0f)
        {
            movementLockoutRemaining -= Time.fixedDeltaTime;
            SetWalkingAudio(false);
            return;
        }

        Vector3 rightDirection =
            Vector3.Cross(Vector3.up, facingDirection).normalized;

        Vector3 movement =
            facingDirection * moveInput.y +
            rightDirection * moveInput.x;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        // Play the loop while there is movement input.
        bool isWalking =
            movement.sqrMagnitude > walkInputThreshold * walkInputThreshold;

        SetWalkingAudio(isWalking);

        Vector3 velocity = GetVelocity();
        velocity.x = movement.x * moveSpeed;
        velocity.z = movement.z * moveSpeed;
        SetVelocity(velocity);
    }

    private void SetWalkingAudio(bool shouldPlay)
    {
        if (walkingAudio == null || walkingAudio.clip == null)
            return;

        if (shouldPlay)
        {
            if (!walkingAudio.isPlaying)
                walkingAudio.Play();
        }
        else if (walkingAudio.isPlaying)
        {
            walkingAudio.Stop();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasPouchTag(collision.collider) || Time.time < nextHitTime)
            return;

        nextHitTime = Time.time + hitCooldown;
        movementLockoutRemaining = movementLockout;

        // Push away from the nearest point on the pouch collider.
        Vector3 center = body.worldCenterOfMass;
        Vector3 away =
            center - collision.collider.ClosestPoint(center);

        away = Vector3.ProjectOnPlane(away, Vector3.up);

        // Fallback for a hit where the closest point is at Bear's center.
        if (away.sqrMagnitude < 0.001f)
        {
            Vector3 attemptedMovement =
                facingDirection * moveInput.y +
                Vector3.Cross(Vector3.up, facingDirection) * moveInput.x;

            away = -Vector3.ProjectOnPlane(attemptedMovement, Vector3.up);
        }

        if (away.sqrMagnitude < 0.001f)
            away = -facingDirection;

        away.Normalize();

        Vector3 impulseDirection =
            (away + Vector3.up * upwardBounceRatio).normalized;

        body.AddForce(
            impulseDirection * bounceImpulse,
            ForceMode.Impulse
        );

        if (crinkleAudio != null)
            crinkleAudio.Play();

        if (pouchVisual != null && pouchPulse == null)
            pouchPulse = StartCoroutine(PulsePouch());
    }

    private bool HasPouchTag(Collider other)
    {
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(pouchTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private IEnumerator PulsePouch()
    {
        Vector3 squeezed =
            Vector3.Scale(pouchOriginalScale, squeezeMultiplier);
        Vector3 overshoot = pouchOriginalScale * 1.02f;

        yield return AnimatePouchScale(pouchOriginalScale, squeezed, 0.08f);
        yield return AnimatePouchScale(squeezed, overshoot, 0.08f);
        yield return AnimatePouchScale(overshoot, pouchOriginalScale, 0.2f);

        pouchPulse = null;
    }

    private IEnumerator AnimatePouchScale(
        Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            pouchVisual.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        pouchVisual.localScale = to;
    }

    private Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }
}
