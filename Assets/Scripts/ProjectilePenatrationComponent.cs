using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component added to projectiles spawned by Projectile.Initialize()
/// -------------------------------------------------------------------
/// Handles how a projectile should react to a collision. Does it penetrate and stay stuck like an arrow, or disappear like a laser?
/// </summary>
public class ProjectilePenatrationComponent : MonoBehaviour
{
    [HideInInspector]
    public bool bPenetratesCollisions;

    private void OnTriggerEnter(Collider collider)
    {
        if (bPenetratesCollisions)
        {
            // If we penetrate, we only need to destroy any physics component, as Destroy is called upon initialization
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            Destroy(rb);
            Destroy(GetComponent<Collider>());
            return;
        }
        Destroy(gameObject);
    }
}
