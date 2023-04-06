using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// General handler class for headtracking.
/// -----------------------------------------------
/// Headtrack works by providing a horizontal and vertical look axis to an animation blend tree, which will handle the head rotation. 
/// This is prefered over a more programatic approach, as it gives animators more control over posing, achieving more natural results.
/// </summary>
public class HeadtrackHandler : MonoBehaviour
{
    public string HeadtrackTargetTagText = "HeadtrackTarget";
    [Space(10)]
    public PlayerAnimHandler animHandler;
    [Space(10)]
    public Transform playerRef;
    public Transform playerHeadBone;
    [Space(10)]
    public float maxHeadTrackAngle;
    public float extendedTrackingMultipler = 1.0f;
    [Space(10)]
    public float trackSpeed = 3;
    [Space(10)]
    public float headtrackTargetSampleRange = 25.0f;

    private Transform HeadtrackTarget;

    float horizDelta = 0;
    float vertDelta = 0;

    private void Start()
    {
        StartCoroutine(PickHeadtrackRoutine());
    }

    /// <summary>
    /// Calculates the angles towards the target on the X and Z axis. Normaizes them and passes them to the PlayerAnimHandler to be sent to the headtrack blend tree.
    /// </summary>
    private void Update()
    {
        float targetHorizDelta = 0;
        float targetVertDelta = 0;

        if (HeadtrackTarget != null)
        {
            Vector3 toTargetAngle = HeadtrackTarget.position - playerHeadBone.position;

            // Calculate absolute angles on each axis
            float horizAngle = Vector3.Angle(playerRef.forward, toTargetAngle.normalized);
            float vertAngle = Vector3.Angle(playerRef.up, toTargetAngle.normalized) - 90;   // Need to rotate this round 90 degrees to so we're tracking the angle from the eyes

            // If the angle is beyond our max, stop looking
            float absoluteMaxAngle = maxHeadTrackAngle * extendedTrackingMultipler;
            if (horizAngle < absoluteMaxAngle && vertAngle < absoluteMaxAngle)
            {
                // Headtrack calculation happens here ----------------
                Vector3 horizCross = Vector3.Cross(playerRef.forward, toTargetAngle);
                targetHorizDelta = Vector3.Dot(horizCross, playerRef.up);
                targetVertDelta = vertAngle / maxHeadTrackAngle;
            }
        }

        // Interpolate the actual headtrack deltas to their new targets
        horizDelta = Mathf.Lerp(horizDelta, targetHorizDelta, Time.deltaTime * trackSpeed);
        vertDelta = Mathf.Lerp(vertDelta, targetVertDelta, Time.deltaTime * trackSpeed);

        animHandler.HandleHeadtrack(new Vector2(horizDelta, vertDelta));
    }

    /// <summary>
    /// Draws a wire sphere gizmo showing the headtrack target collection radius.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Sample radius sphere
        Gizmos.DrawWireSphere(transform.position, headtrackTargetSampleRange);
    }

    /// <summary>
    /// Collects all potential headtrack targets within the allowed radius, and selects the closest reference to act as our Headtrack Target.
    /// </summary>
    void PickHeadtrackTarget()
    {
        GameObject preferedTarget = null;
        float bestDist = 0;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, headtrackTargetSampleRange);
        foreach (Collider coll in hitColliders)
        {
            if (coll.gameObject.tag == "HeadtrackTarget")
            {
                float dist = Vector3.Distance(transform.position, coll.transform.position);
                if (dist < bestDist || preferedTarget == null)
                {
                    preferedTarget = coll.gameObject;
                    bestDist = dist;
                }
            }
        }

        HeadtrackTarget = preferedTarget ? preferedTarget.transform : null;
    }

    /// <summary>
    /// Simple looping Coroutine that picks a new headtrack target every half second.
    /// This is done so we can defer headtracking calculation to a much slower tick-rate than any update function. 
    /// Headtrack target selection uses a costly sphere overlap check, and then a distance check on every valid reference that collects, hence the need for a deferal here.
    /// </summary>
    IEnumerator PickHeadtrackRoutine()
    {
        while (true)
        {
            PickHeadtrackTarget();
            yield return new WaitForSeconds(0.5f);
        }
    }
}
