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
    public bool showGizmos = true;
    [Tooltip("Width and height of the square sampled around the avoider.")]
    public float sampleAreaSize = 16f;
    [Tooltip("Minimum distance between Poisson-disc sample points.")]
    public float sampleSpacing = 2f;
    [Tooltip("Height of the visibility ray so it clears the floor.")]
    public float eyeHeight = 1f;

    private NavMeshAgent agent;
    private readonly List<Vector3> hiddenPoints = new List<Vector3>();
    private readonly List<Vector3> visiblePoints = new List<Vector3>();
    private Vector3 hidePoint;
    private bool hasHidePoint;
    private float nextReplanTime;

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

        DrawSamples();

        // Already behind cover, and the spot we're using is still blocked. Stay put.
        bool seen = !IsHiddenFromAvoidee(transform.position);
        bool hideSpotExposed = hasHidePoint && !IsHiddenFromAvoidee(hidePoint);
        if (!seen && !hideSpotExposed)
            return;

        // Keep walking to a spot the player still cannot see.
        bool traveling = agent.pathPending || (agent.hasPath && agent.remainingDistance > 1f);
        if (traveling && hasHidePoint && !hideSpotExposed)
            return;

        if (Time.time < nextReplanTime)
            return;

        PlanEscape();
        nextReplanTime = Time.time + 0.5f;
    }

    private void PlanEscape()
    {
        hiddenPoints.Clear();
        visiblePoints.Clear();
        hasHidePoint = false;

        if (sampleAreaSize <= 0f || sampleSpacing <= 0f)
            return;

        // Samples are local XZ offsets inside a square centered on the avoider.
        var sampler = new PoissonDiscSampler(sampleAreaSize, sampleAreaSize, sampleSpacing);
        Vector3 origin = transform.position - new Vector3(sampleAreaSize, 0f, sampleAreaSize) * 0.5f;

        float bestDistance = float.MaxValue;

        foreach (Vector2 sample in sampler.Samples())
        {
            Vector3 point = new Vector3(origin.x + sample.x, transform.position.y, origin.z + sample.y);
            if (!NavMesh.SamplePosition(point, out NavMeshHit navHit, sampleSpacing, NavMesh.AllAreas))
                continue;

            point = navHit.position;
            if (IsHiddenFromAvoidee(point))
            {
                hiddenPoints.Add(point);
                float pointDistance = Vector3.Distance(transform.position, point);
                if (pointDistance < bestDistance)
                {
                    bestDistance = pointDistance;
                    hidePoint = point;
                    hasHidePoint = true;
                }
            }
            else
            {
                visiblePoints.Add(point);
            }
        }

        if (hasHidePoint && agent.isOnNavMesh)
            agent.SetDestination(hidePoint);
    }

    private bool IsHiddenFromAvoidee(Vector3 point)
    {
        Vector3 from = avoidee.transform.position + Vector3.up * eyeHeight;
        Vector3 to = point + Vector3.up * eyeHeight;
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.05f)
            return false;

        // Hidden when something other than the player or the avoider blocks the view.
        RaycastHit[] hits = Physics.RaycastAll(from, direction / distance, distance);
        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.transform;
            if (hitTransform == avoidee.transform || hitTransform.IsChildOf(avoidee.transform))
                continue;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void DrawSamples()
    {
        if (!showGizmos)
            return;

        Vector3 from = transform.position + Vector3.up * eyeHeight;
        foreach (Vector3 point in visiblePoints)
            Debug.DrawLine(from, point + Vector3.up * eyeHeight, Color.red);

        foreach (Vector3 point in hiddenPoints)
            Debug.DrawLine(from, point + Vector3.up * eyeHeight, Color.green);
    }
}
