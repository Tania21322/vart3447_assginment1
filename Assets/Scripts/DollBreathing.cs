using System.Collections;
using UnityEngine;

public class DollBreathing : MonoBehaviour
{
    [SerializeField] private ParticleSystem breathParticles;
    [SerializeField] private Transform chest;
    [SerializeField] private int particlesPerExhale = 12;

    [SerializeField] private float inhaleSeconds = 1.6f;
    [SerializeField] private float exhaleSeconds = 1.0f;
    [SerializeField] private float pauseSeconds = 0.5f;
    [SerializeField] private float expansion = 0.015f;

    private IEnumerator Start()
    {
        if (chest == null)
        {
            Debug.LogError("Assign the doll's chest/torso object.", this);
            yield break;
        }

        if (breathParticles != null)
        {
            breathParticles.Play();
            Debug.Log("playParticles");
        }


        Vector3 restScale = chest.localScale;
        // Slightly widen and deepen the chest on inhale.
        Vector3 inhaleScale = Vector3.Scale(
            restScale,
            new Vector3(1f + expansion, 1f, 1f + expansion)
        );

        while (true)
        {
            yield return AnimateScale(restScale, inhaleScale, inhaleSeconds);

            if (breathParticles != null)
                breathParticles.Emit(particlesPerExhale);

            yield return AnimateScale(inhaleScale, restScale, exhaleSeconds);
            yield return new WaitForSeconds(pauseSeconds);
        }
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            chest.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        chest.localScale = to;
    }
}
