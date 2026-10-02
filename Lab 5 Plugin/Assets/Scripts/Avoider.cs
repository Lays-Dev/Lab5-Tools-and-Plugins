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

#endregion
#region Variables

    private NavMeshAgent agent;

#endregion
#region Start

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if(agent == null)
        {
            Debug.LogWarning("Avoider needs a NavMesh Agent in the inspector.");
        }

        if(avoidee == null)
        {
            Debug.LogWarning("Avoider needs an Avoidee assigned in the inspector.");
        }

        agent.speed = speed;
    }

#endregion
#region Update

    private void Update()
    {
        if(avoidee == null)
        {
            Debug.LogWarning("Avoider needs an Avoidee assigned in the inspector.");
            return;
        }

        // Player position - Avoider position will equal the direction for the avoider to look at
        Vector3 direction = avoidee.transform.position - transform.position;
        // Ignores any virticle difference. There shouldn't be any either way.
        direction.y = 0;

        // Only updates if there is a new direction to look at! Preformence~
        if (direction != Vector3.zero)
        {
           
            // Rotate this game object
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
#endregion

}
