using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct CameraStateProfile
{
    public Vector3 offset;
    public float chaseSpeed;
    public float fov;
}

/// <summary>
/// Controller class for the camera rig. Handles the camera state machine, as well as some extra behavior such as wall clip detection.
/// </summary>
public class CameraRig : MonoBehaviour
{
    public GameObject playerRef;

    [Space(10)]

    public CameraStateProfile RestingProfile;
    public CameraStateProfile WalkingProfile;
    public CameraStateProfile RunningProfile;
    public CameraStateProfile AimingProfile;
    public CameraStateProfile CannedAnimProfile;
    public CameraStateProfile CombatProfile;
    public CameraStateProfile CrouchedProfile;

    [Space(10)]

    [Range(0.0f, 0.5f)]
    public float ROT_EdgeSize = 0.33f;  // used to track how far the rull of thirds are defined. The player will always try to be kept in the left most ROT line

    [Header("Cam Rig")]
    public Transform rigRoot;
    public Transform upDownRotatorRoot;

    [Header("Cam Clamps")]
    public Vector2 cameraVerticalClamps;
    public LayerMask wallClipLayerMask;
    public float wallClipCorrectionSpeed = 5f;

    [Header("Interpolation")]
    public float chaseSpeedInterpolationRate = 3f;
    public float fovInterpolationRate = 3;

    [Header("Turn")]
    public float camRotationSpeed = 1;

    [Header("Animation")]
    public Animator animator;
    public string VariableName_IsRunning = "bIsRunning";
    public string AnimName_Crouch = "Cam - Crouch";

    [Header("Debug HUD")]
    [Tooltip("Draw the rule of thirds lines to help position the camera.")]
    public bool DebugModule_RuleOfThirds;
    [Tooltip("Draw the camera wall cliping rays, as well as the intersection point.")]
    public bool DebugModule_CameraWallClipping;


    [HideInInspector]
    public bool exitCannedAnim;

    private PlayerCharacter playerCharRef;
    private Camera camComp;

    private float chaseSpeed;
    private float cameraYRot;
    private float camTargetDist;

    private Vector3 debughit;
    private Vector3 cameraOffset;
    private Quaternion preCannedAnimCameraRotation;

    // Start is called before the first frame update
    void Start()
    {
        playerCharRef = playerRef.GetComponent<PlayerCharacter>();
        camComp = Camera.main;

        camTargetDist = Vector3.Distance(Vector3.zero, RestingProfile.offset);
    }

    /// <summary>
    /// Draws debugging gizmos if enabled. These include:
    ///   - Rule of Thirds lines: 
    ///         Draws lines at each third of the screen.
    ///         Used to enable designers and artists to set their camera offsets according to the rule of thirds, providing more cinematic shots
    ///   - Camera Wall Clipping:
    ///         Draws a sphere at the point of intersection.
    /// </summary>
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // Draw the rule of thirds lines to help position the camera
        if (DebugModule_RuleOfThirds)
        {
            // Left Line
            Rect lrect = new Rect(Screen.width * ROT_EdgeSize, 0, 1, Screen.height);
            // Right Line
            Rect rrect = new Rect(Screen.width - Screen.width * ROT_EdgeSize, 0, 1, Screen.height);
            UnityEditor.Handles.BeginGUI();
            UnityEditor.Handles.DrawSolidRectangleWithOutline(lrect, Color.black, Color.red);
            UnityEditor.Handles.DrawSolidRectangleWithOutline(rrect, Color.black, Color.red);
            UnityEditor.Handles.EndGUI();
        }
        if (DebugModule_CameraWallClipping)
        {
            if (debughit != Vector3.zero)
            {
                Gizmos.DrawWireSphere(debughit, 0.25f);
            }
        }
    }

    /// <summary>
    /// Sets the cameras current position, FOV, and offset based on the players current state.
    /// -----------------------------------------------
    /// Called at a fixed interval once per frame. Handles all of the cameras behavior that needs updating once per tick. Handles:
    ///  - Chasing
    ///  - Wall clip prevention
    ///  - Camera offset
    ///  - FOV
    ///  - Rotation
    ///  - Sending camera animation updates
    ///  -----------------------------------------------
    /// </summary>
    void FixedUpdate()
    {
        PlayerCharacter.PlayerState currentPlayerState = playerCharRef.QState();

        if (currentPlayerState != PlayerCharacter.PlayerState.CannedAnim)
        {
            exitCannedAnim = false;
            rigRoot.localEulerAngles = new Vector3(0, rigRoot.localEulerAngles.y, 0);   // Reset camera back to true 0 - slerp will get us close but could cause tilting if not fully reset
        }

        // Chase system
        float targetChaseSpeed = RestingProfile.chaseSpeed;
        switch (currentPlayerState)
        {
            case PlayerCharacter.PlayerState.Idle:
            case PlayerCharacter.PlayerState.Walking:
            case PlayerCharacter.PlayerState.Crouched:
                targetChaseSpeed = RestingProfile.chaseSpeed;
                break;
            case PlayerCharacter.PlayerState.Running:
                targetChaseSpeed = RunningProfile.chaseSpeed;
                break;
            case PlayerCharacter.PlayerState.Aiming:
                targetChaseSpeed = AimingProfile.chaseSpeed;
                break;
            case PlayerCharacter.PlayerState.CannedAnim:
                targetChaseSpeed = CannedAnimProfile.chaseSpeed;
                break;
        }
        chaseSpeed = Mathf.Lerp(chaseSpeed, targetChaseSpeed, Time.deltaTime * chaseSpeedInterpolationRate);
        Vector3 targetPosition = currentPlayerState == PlayerCharacter.PlayerState.CannedAnim && !exitCannedAnim ? playerCharRef.QAnimCameraBone().position : playerRef.transform.position;
        rigRoot.position = Vector3.Lerp(rigRoot.position, targetPosition, Time.deltaTime * chaseSpeedInterpolationRate);

        // Wall clipping prevention
        RaycastHit hit;
        if (Physics.Raycast(playerRef.transform.position, (transform.position - playerRef.transform.position).normalized, out hit, camTargetDist, wallClipLayerMask))
        {
            // Camera is clipping into a wall
            if (DebugModule_CameraWallClipping)
            {
                Debug.DrawRay(playerRef.transform.position, (transform.position - playerRef.transform.position).normalized * camTargetDist, Color.yellow);
                debughit = hit.point;
            }
            transform.position = Vector3.Lerp(transform.position, hit.point, Time.deltaTime * wallClipCorrectionSpeed);
        }
        else
        {
            // Camera is not clipping
            if (DebugModule_CameraWallClipping)
            {
                Debug.DrawRay(playerRef.transform.position, (transform.position - playerRef.transform.position).normalized * camTargetDist, Color.green);
                debughit = Vector3.zero;
            }

            // Calculate our camera offset and then lerp towards it
            Vector3 tempCamOffset = cameraOffset;
            if (GameManager.state == GameManager.GameplayState.Combat)
            {
                tempCamOffset = CombatProfile.offset;
                if (currentPlayerState == PlayerCharacter.PlayerState.Aiming)
                {
                    tempCamOffset = AimingProfile.offset;
                }
            }
            else
            {
                switch (currentPlayerState)
                {
                    case PlayerCharacter.PlayerState.Idle:
                    case PlayerCharacter.PlayerState.Walking:
                    case PlayerCharacter.PlayerState.Running:
                        tempCamOffset = RestingProfile.offset;
                        break;
                    case PlayerCharacter.PlayerState.Aiming:
                        tempCamOffset = AimingProfile.offset;
                        break;
                    case PlayerCharacter.PlayerState.CannedAnim:
                        tempCamOffset = exitCannedAnim ? RestingProfile.offset : Vector3.zero;
                        break;
                    case PlayerCharacter.PlayerState.Crouched:
                        tempCamOffset = CrouchedProfile.offset;
                        break;
                }
            }
            if (cameraOffset != tempCamOffset)
            {
                // We've changed our cam offset, we need to recalculate our wall check range
                camTargetDist = Vector3.Distance(Vector3.zero, RestingProfile.offset);
            }
            cameraOffset = tempCamOffset;
            transform.localPosition = Vector3.Lerp(transform.localPosition, cameraOffset, Time.deltaTime * chaseSpeedInterpolationRate);
        }

        // FOV system
        float targetFOV = RestingProfile.fov;

        if (GameManager.state == GameManager.GameplayState.Combat)
        {
            targetFOV = CombatProfile.fov;
            if (currentPlayerState == PlayerCharacter.PlayerState.Aiming)
            {
                targetFOV = AimingProfile.fov;
            }
        }
        else
        {
            switch (currentPlayerState)
            {
                case PlayerCharacter.PlayerState.Idle:
                case PlayerCharacter.PlayerState.Walking:
                    targetFOV = RestingProfile.fov;
                    break;
                case PlayerCharacter.PlayerState.Running:
                    targetFOV = RunningProfile.fov;
                    break;
                case PlayerCharacter.PlayerState.Aiming:
                    targetFOV = AimingProfile.fov;
                    break;
                case PlayerCharacter.PlayerState.Crouched:
                    targetFOV = CrouchedProfile.fov;
                    break;
            }
        }
        camComp.fieldOfView = Mathf.Lerp(camComp.fieldOfView, targetFOV, Time.deltaTime * fovInterpolationRate);

        // Rotation
        if (currentPlayerState == PlayerCharacter.PlayerState.CannedAnim)
        {
            // Match main 
            rigRoot.rotation = Quaternion.Slerp(rigRoot.rotation, exitCannedAnim ? preCannedAnimCameraRotation : playerCharRef.QAnimCameraBone().rotation, Time.deltaTime * 4f);
        }
        else
        {
            // Player driven controls
            rigRoot.Rotate(Vector3.up, Input.GetAxis("CamRotationX") * camRotationSpeed);
            cameraYRot += Input.GetAxis("CamRotationY") * camRotationSpeed;
            cameraYRot = Mathf.Clamp(cameraYRot, cameraVerticalClamps.x, cameraVerticalClamps.y);
            upDownRotatorRoot.localRotation = Quaternion.Euler(cameraYRot, 0, 0);
        }

        // Camera animations
        animator.SetBool(VariableName_IsRunning, currentPlayerState == PlayerCharacter.PlayerState.Running);
    }

    /// <summary>
    /// Ask the camera animator to play an animation.
    /// </summary>
    /// <param name="asAnimName">Name of the animation state as it appears in the Animator.</param>
    public void RequestCameraAnimation(string asAnimName)
    {
        animator.Play(asAnimName);
    }

    /// <summary>
    /// Stores the current rotation of the rig's root node. We will return to this rotation when we exit our canned animation.
    /// </summary>
    public void PreCannedAnimNotify()
    {
        preCannedAnimCameraRotation = rigRoot.rotation;
    }

    /// <summary>
    /// Notify the state machine that we wish to exit our canned anim state.
    /// </summary>
    public void CannedAnimExitNotify()
    {
        exitCannedAnim = true;
    }

    /// <summary>
    /// Returns the camera component held in this rig. Faster than every object that needs it calling Camer.Main on start.
    /// </summary>
    public Camera QCam() { return camComp; }
}
