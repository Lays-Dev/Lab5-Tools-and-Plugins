/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Avoider : MonoBehaviour
{

#region Inspector

    [Tooltip("Place the player GameObject here.")]
    public GameObject avoidee;
    [Tooltip("How close the player can get before activating the avoider.")]
    public float range = 10f;
    [Tooltip("How fast the avoider moves away from the player")]
    public float speed = 5f;
    [Tooltip("Show development visuals")]
    public bool showGizmos = true;
    [Tooltip("Width and height of the square sampled around the avoider.")]
    public float sampleAreaSize = 16f;
    [Tooltip("Minimum distance between Poisson-disc sample points.")]
    public float sampleSpacing = 2f;
    [Tooltip("Seconds between new sample sets while the avoidee stays in range.")]
    public float replanInterval = 0.5f;
    [Tooltip("Height of the visibility ray so it does not skim the ground.")]
    public float eyeHeight = 1.5f;

#endregion
#region Variables

    private NavMeshAgent agent;
    // Last sample set, split so gizmos can color hidden spots and rejected points.
    private readonly List<Vector3> hiddenPoints = new List<Vector3>();
    private readonly List<Vector3> visiblePoints = new List<Vector3>();
    private Vector3 hidePoint;
    private bool hasHidePoint;
    // Time.time value when we are allowed to sample again.
    private float nextReplanTime;

#endregion
#region Start

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if(agent == null)
        {
            Debug.LogWarning("Avoider needs a NavMesh Agent in the inspector with a baked NavMesh.");
            return;
        }

        if(avoidee == null)
        {
            Debug.LogWarning("Avoider needs an Avoidee assigned in the inspector.");
        }

        agent.speed = speed;
        // We rotate toward the avoidee ourselves. Leave this on and the agent faces its path instead.
        agent.updateRotation = false;
    }

#endregion
#region Update

    private void Update()
    {
        if (avoidee == null || agent == null)
            return;

        agent.speed = speed;
        LookAtAvoidee();

        float distance = Vector3.Distance(transform.position, avoidee.transform.position);
        // Out of range: stop fleeing, but keep looking at the avoidee.
        if (distance > range)
        {
            if (hasHidePoint || hiddenPoints.Count > 0 || visiblePoints.Count > 0 || AgentHasPath())
                ClearEscape();
            return;
        }

        // In range: reuse the current hide spot until it is time to sample again.
        if (!NeedsNewPlan())
            return;

        PlanEscape();
        nextReplanTime = Time.time + replanInterval;
    }

    private void LookAtAvoidee()
    {
        // Player position - Avoider position will equal the direction for the avoider to look at
        Vector3 direction = avoidee.transform.position - transform.position;
        // Ignores any virticle difference. There shouldn't be any either way.
        direction.y = 0;

        // Only updates if there is a new direction to look at! Preformence~
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool NeedsNewPlan()
    {
        // Regular resample while the avoidee stays nearby.
        if (Time.time >= nextReplanTime)
            return true;

        // Last sample found nowhere to run. Wait for the timer instead of sampling every frame.
        if (!hasHidePoint)
            return false;

        // Current spot is no longer covered, or we already reached it.
        if (!IsHiddenFromAvoidee(hidePoint))
            return true;

        return HasArrived();
    }

    private bool HasArrived()
    {
        if (!AgentHasPath() || agent.pathPending)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance;
    }

    private bool AgentHasPath()
    {
        return agent.isOnNavMesh && agent.hasPath;
    }

    private void PlanEscape()
    {
        hiddenPoints.Clear();
        visiblePoints.Clear();
        hasHidePoint = false;

        if (sampleAreaSize <= 0f || sampleSpacing <= 0f)
            return;

        // Samples are local XZ offsets inside a square centered on the avoider.
        // sampleSpacing is the minimum gap between points, not the grid cell size.
        var sampler = new PoissonDiscSampler(sampleAreaSize, sampleAreaSize, sampleSpacing);
        Vector3 origin = transform.position - new Vector3(sampleAreaSize, 0f, sampleAreaSize) * 0.5f;

        float bestDistance = float.MaxValue;

        foreach (Vector2 sample in sampler.Samples())
        {
            Vector3 world = new Vector3(origin.x + sample.x, transform.position.y, origin.z + sample.y);
            // Drop points that are not on the baked NavMesh.
            if (!NavMesh.SamplePosition(world, out NavMeshHit navHit, sampleSpacing, NavMesh.AllAreas))
                continue;

            Vector3 point = navHit.position;
            // Keep the point only when the avoidee cannot see it, and remember the closest one.
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

        if (!hasHidePoint || !agent.isOnNavMesh)
            return;

        agent.SetDestination(hidePoint);
    }

    private bool IsHiddenFromAvoidee(Vector3 point)
    {
        Vector3 from = avoidee.transform.position + Vector3.up * eyeHeight;
        Vector3 to = point + Vector3.up * eyeHeight;
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.01f)
            return false;

        // A hit that is not the avoidee or the avoider means something is blocking the view.
        RaycastHit[] hits = Physics.RaycastAll(from, direction / distance, distance);
        foreach (RaycastHit hit in hits)
        {
            if (BelongsTo(hit.collider, avoidee.transform) || BelongsTo(hit.collider, transform))
                continue;

            return true;
        }

        return false;
    }

    private static bool BelongsTo(Collider collider, Transform root)
    {
        return collider.transform == root || collider.transform.IsChildOf(root);
    }

    private void ClearEscape()
    {
        hiddenPoints.Clear();
        visiblePoints.Clear();
        hasHidePoint = false;
        nextReplanTime = 0f;

        if (AgentHasPath())
            agent.ResetPath();
    }

#endregion
#region Gizmos

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Cyan is the trigger range. Red points are visible, green points are hidden, yellow is the destination.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        foreach (Vector3 point in visiblePoints)
            Gizmos.DrawSphere(point, 0.2f);

        Gizmos.color = Color.green;
        foreach (Vector3 point in hiddenPoints)
            Gizmos.DrawSphere(point, 0.25f);

        if (!hasHidePoint)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hidePoint, 0.35f);
        Gizmos.DrawLine(transform.position, hidePoint);
    }

#endregion

}
*/
