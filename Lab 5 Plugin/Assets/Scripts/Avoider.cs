using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Avoider : MonoBehaviour
{
    [Tooltip("Place the player GameObject here.")]
    public GameObject avoidee;
    [Tooltip("How fast the avoider moves.")]
    public float speed = 5f;
    [Tooltip("Draw the sample lines while playing.")]
    public bool showLines = true;
    [Tooltip("Width and height of the square sampled around the avoider.")]
    public float sampleAreaSize = 16f;
    [Tooltip("Minimum distance between Poisson-disc sample points.")]
    public float sampleSpacing = 2f;
    [Tooltip("Layer the avoider is on so sight checks can see through it.")]
    public LayerMask avoiderLayer;

    private NavMeshAgent agent;
    private readonly List<Vector2> samplePositions = new List<Vector2>();
    private bool hasSamples;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning("Avoider needs a NavMesh Agent in the inspector with a baked NavMesh.");
            return;
        }

        if (avoidee == null)
            Debug.LogWarning("Avoider needs an Avoidee assigned in the inspector.");

        agent.speed = speed;
    }

    private void Update()
    {
        if (avoidee == null || agent == null)
            return;

        // Sample again only after we arrive, not on every frame of the path.
        if (agent.remainingDistance > 1f)
            return;

        if (!hasSamples)
        {
            CollectSamples();
            hasSamples = true;
            return;
        }

        Vector3 moveTo = Vector3.zero;
        float bestDistance = float.MaxValue;
        bool found = false;

        foreach (Vector2 point in samplePositions)
        {
            Vector3 samplePosition = new Vector3(
                transform.position.x + point.x - sampleAreaSize * 0.5f,
                transform.position.y,
                transform.position.z + point.y - sampleAreaSize * 0.5f);

            Vector3 toAvoidee = avoidee.transform.position - samplePosition;
            if (!Physics.Raycast(samplePosition, toAvoidee, out RaycastHit hit, Mathf.Infinity, ~avoiderLayer))
                continue;

            bool hidden = hit.transform != avoidee.transform;
            if (showLines)
                Debug.DrawLine(transform.position, samplePosition, hidden ? Color.green : Color.red);

            if (!hidden)
                continue;

            float pointDistance = Vector3.Distance(transform.position, samplePosition);
            if (pointDistance < bestDistance)
            {
                bestDistance = pointDistance;
                moveTo = samplePosition;
                found = true;
            }
        }

        // Stay put unless the player can see this object.
        Vector3 toPlayer = avoidee.transform.position - transform.position;
        if (!Physics.Raycast(transform.position, toPlayer, out RaycastHit playerHit) || playerHit.transform != avoidee.transform)
            return;

        if (!found)
        {
            CollectSamples();
            return;
        }

        agent.SetDestination(moveTo);
        hasSamples = false;
    }

    private void CollectSamples()
    {
        samplePositions.Clear();

        if (sampleAreaSize <= 0f || sampleSpacing <= 0f)
            return;

        var sampler = new PoissonDiscSampler(sampleAreaSize, sampleAreaSize, sampleSpacing);
        foreach (Vector2 point in sampler.Samples())
            samplePositions.Add(point);
    }
}
