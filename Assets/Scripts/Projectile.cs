using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scriptable Object for Projectile objects.
/// -----------------------------------------------
/// Contians all data that may need tweaking for different behaviors, as well as an initialization function to spawn and fire the arrow from a single call.
/// </summary>
[CreateAssetMenu(fileName = "New Projectile", menuName = "Data/Projectile")]
public class Projectile : ScriptableObject
{
    [Header("Gameplay")]
    public float speed;
    public string heirarchyName;
    public bool penetrates;
    public int physicsLayerID;

    [Header("3D")]
    public Mesh model;
    public Material material;
    public Vector3 rotation;
    public Vector3 colliderBounds;
    public Vector3 colliderBoundsOffset;

    /// <summary>
    /// Creates and performs a setup process on a new projectile, based on the data assigned in this scriptable object. Also fires said projectile.
    /// </summary>
    /// <param name="apOrigin">The point we wish to fire thir projectile from - usually the "Fire Node" on the player rig.</param>
    /// <param name="apShooter">The transform for the GameObject shooting this projectile.</param>
    /// <param name="acFireCam">Optional: The camera we wish to fire this projectile from. Used to ensure that player-fired projectiles always hit their mark.</param>
    public void Initialize(Transform apOrigin, Transform apShooter, Camera acFireCam = null)
    {
        // Create new gameobject
        GameObject newProj = new GameObject(heirarchyName);
        newProj.transform.position = apOrigin.position;
        newProj.transform.forward = apShooter.forward;

        // Setup the 3D
        GameObject meshObj = new GameObject("3D");
        meshObj.transform.parent = newProj.transform;
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localEulerAngles = rotation;
        meshObj.AddComponent<MeshFilter>().mesh = model;
        meshObj.AddComponent<MeshRenderer>().material = material;

        // Setup physics
        newProj.layer = physicsLayerID;
        Rigidbody rb = newProj.AddComponent<Rigidbody>();
        BoxCollider collider = newProj.AddComponent<BoxCollider>();
        collider.size = colliderBounds;
        collider.center = colliderBoundsOffset;
        collider.isTrigger = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (acFireCam)
        {
            // If we have a camera, we should try to fire to wherever the center of the screen is
            Ray ray = acFireCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                newProj.transform.LookAt(hit.point);
                rb.velocity = newProj.transform.forward * speed;
            }
            else
            {
                rb.velocity = apShooter.forward * speed;
            }
        }
        else
        {
            // If we don't have a cam, fallback and fire from our fire point
            rb.velocity = apShooter.forward * speed;
        }
        newProj.AddComponent<ProjectilePenatrationComponent>().bPenetratesCollisions = penetrates;

        // Cleanup
        Destroy(newProj, 90);   // New projectiles shouldn't need to live longer than 90 seconds
    }
}
