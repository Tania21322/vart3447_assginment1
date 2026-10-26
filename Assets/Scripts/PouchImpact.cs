using System.Collections;
using UnityEngine;

public class PouchImpact : MonoBehaviour

{
    [Header("Assign in Inspector")]
    [SerializeField] private Transform pouchRoot;
    [SerializeField] private Transform pouchVisual; // Mesh only, not the colliders
    [SerializeField] private AudioSource crinkleAudio;

    [Header("Optional rebound")]
    [SerializeField] private bool physicallyBounceBear = false;
    [SerializeField] private float bounceImpulse = 1f;

    [Header("Impact")]
    [SerializeField] private float hitCooldown = 0.45f;

    private Rigidbody bearBody;
    private Vector3 originalScale;
    private float nextHitTime;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        bearBody = GetComponent<Rigidbody>();

        if (pouchVisual != null)
            originalScale = pouchVisual.localScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Transform hit = collision.transform;

        // Accept any of the six colliders on the pouch or its children.
        if (pouchRoot == null ||
            (hit != pouchRoot && !hit.IsChildOf(pouchRoot)) ||
            Time.time < nextHitTime)
        {
            return;
        }

        nextHitTime = Time.time + hitCooldown;

        if (crinkleAudio != null)
            crinkleAudio.Play();

        if (pouchVisual != null && pulseRoutine == null)
            pulseRoutine = StartCoroutine(PulsePouch());

        // Optional: use only if Bear is a physics-driven object,
        // not the actual VR player's body.
        if (physicallyBounceBear && bearBody != null && !bearBody.isKinematic)
        {
            Vector3 away = bearBody.worldCenterOfMass - pouchRoot.position;
            away = Vector3.ProjectOnPlane(away, Vector3.up);

            if (away.sqrMagnitude > 0.001f)
                bearBody.AddForce(
                    away.normalized * bounceImpulse,
                    ForceMode.Impulse
                );
        }
    }

    private IEnumerator PulsePouch()
    {
        // Assumes local Z is the pouch's depth; adjust for your model.
        Vector3 squeezed = Vector3.Scale(
            originalScale,
            new Vector3(1.03f, 1.03f, 0.82f)
        );

        Vector3 overshoot = originalScale * 1.02f;

        yield return AnimateScale(originalScale, squeezed, 0.08f);
        yield return AnimateScale(squeezed, overshoot, 0.08f);
        yield return AnimateScale(overshoot, originalScale, 0.2f);

        pulseRoutine = null;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
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
}
