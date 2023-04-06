using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Class handling the base player. Only ONE of these should exist at once.
/// -----------------------------------------------
/// Handles:
///     - State machine
///     - Locomotion
///     - Player input 
///     - Flavor idles
/// -----------------------------------------------
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Crouched,
        Aiming,
        CannedAnim
    }

    [ReadOnly]
    public PlayerState currentState;
    [Space(10)]
    public float speed_Walk;
    public float speed_Run;
    public float speed_Aim;
    public float speed_Crouch;
    [Space(10)]
    [Tooltip("How fast we transition between speeds.")]
    public float speedInterpolationRate = 3f;
    [Tooltip("How fast we turn to face the cameras direction when moving.")]
    public float faceCameraTurnSpeed_Aiming;
    [Space(10)]
    [Tooltip("How far the player has to push the stick to register movement.")]
    public float inputThreshold_Move = 0.1f;
    [Tooltip("How far the player has to push the stick to be eligible to run.")]
    public float inputThreshold_Run = 0.5f;
    [Space(10)]
    public Vector2 IdleFlavourPickIntervals;
    [Space(10)]
    public PlayerAnimHandler animHandler;
    public CrosshairManager crosshairHandler;
    public CameraRig camRig;

    private Camera cam;
    private Rigidbody rb;

    private IEnumerator pickFlavorCoroutine;

    private float targetSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = camRig.QCam();
        pickFlavorCoroutine = RequestIdleFlavourRoutine();
        StartCoroutine(pickFlavorCoroutine);
    }

    /// <summary>
    /// Calls all of the constant update functions, as well as any debugging hotkeys.
    /// </summary>
    private void Update()
    {
        ProcessStateMachine();
        HandleLocomotion();
        HandleActionInput();

        // Debug - toggle combat mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.state = GameManager.state == GameManager.GameplayState.Resting ? GameManager.GameplayState.Combat : GameManager.GameplayState.Resting;
        }

        // Debug - play a canned anim
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentState = PlayerState.CannedAnim;
           camRig.PreCannedAnimNotify();
            animHandler.PlayCannedAnim("Taking Item");
        }
    }

    /// <summary>
    /// Updates the players current state in the state machine. 
    /// Doesn't actually handle any of the resulting behavior - that comes in other functions such as HandleLocomotion() or CameraRig.FixedUpdate().
    /// </summary>
    private void ProcessStateMachine()
    {
        PlayerState tempState = PlayerState.Idle;

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        float inputMagnitude = input.magnitude;

        if (currentState == PlayerState.Running ? 
            inputMagnitude > inputThreshold_Move && inputMagnitude < inputThreshold_Run :       // Exit sprint conditions
            inputMagnitude > inputThreshold_Move)                                               // Normal begin walk conditions
        {
            tempState = PlayerState.Walking;
        }

        if (Input.GetButtonDown("Crouch") || currentState == PlayerState.Crouched)
        {
            // If we specifically pressed the button down, we need to toggle our state
            // Otherwise, we just want to maintain our current state
            if (Input.GetButtonDown("Crouch"))
            {
                tempState = (currentState == PlayerState.Crouched ? PlayerState.Idle : PlayerState.Crouched);
            }
            else
            {
                tempState = PlayerState.Crouched;
            }
        }

        if ((Input.GetButtonDown("Sprint") || currentState == PlayerState.Running) && inputMagnitude >= inputThreshold_Run)
        {
            tempState = PlayerState.Running;
        }

        if (Input.GetButton("Aim"))
        {
            tempState = PlayerState.Aiming;
        }

        // Check if we need to stay in the canned anim state
        if (currentState == PlayerState.CannedAnim)
        {
            // If we're playing a canned anim, we need to stay in that state until the anim is over
            if (animHandler.isPlayingCannedAnim)
            {
                tempState = PlayerState.CannedAnim;
            }
        }

        // Handle state changes
        if (currentState != tempState)
        {
            OnStateChanged(tempState, currentState);
            currentState = tempState;
        }

        // Update anims and UI to adapt to the current state
        crosshairHandler.UpdateCrosshair(currentState == PlayerState.Aiming, inputMagnitude);
        animHandler.HandleAnimVariables(input, currentState, camRig.upDownRotatorRoot.localEulerAngles.x);
    }

    /// <summary>
    /// Handles locomotion based on the current state.
    /// -----------------------------------------------
    ///  Results per state are:
    ///     - Idle: 
    ///         Nothing, allow the rigidbody to settle naturally. Allows for some small inertia.
    ///     - Canned Anim: 
    ///         Stop all physics velocity, all of our positions are animation driven at the moment.
    ///     - Aiming, Running, Walking, Crouching:
    ///         Move based on the players current inputs, relative to the cameras forward vector.
    /// -----------------------------------------------
    /// </summary>
    private void HandleLocomotion()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                break;

            case PlayerState.CannedAnim:
                rb.velocity = Vector3.zero;
                break;

            case PlayerState.Aiming:
            case PlayerState.Running:
            case PlayerState.Walking:
            case PlayerState.Crouched:
                targetSpeed = Mathf.Lerp(targetSpeed, QCurrentSpeed(), Time.deltaTime * speedInterpolationRate);
                if (currentState == PlayerState.Aiming)
                {
                    targetSpeed = speed_Aim;    // We don't want to lerp to our aim speed, we want to snap to it
                }

                Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

                // Project camera forward vector to 2D
                Vector3 camFwd = cam.transform.forward;
                camFwd.y = 0;
                camFwd.Normalize();

                // Project camera right vector to 2D
                Vector3 camRight = cam.transform.right;
                camRight.y = 0;
                camRight.Normalize();

                // Calculate velocity based on the camera's direction
                Vector3 locoVelocity = (camRight * input.x * targetSpeed) + (camFwd * input.y * targetSpeed);
                rb.velocity = locoVelocity;

                if (currentState != PlayerState.Crouched || input.sqrMagnitude > 0.1f)
                {
                    // Orient ourselves to face the camera direction
                    float reOrientSpeed = currentState == PlayerState.Aiming ? faceCameraTurnSpeed_Aiming : targetSpeed;
                    transform.forward = Vector3.Lerp(transform.forward, camFwd, Time.deltaTime * reOrientSpeed);
                    transform.right = Vector3.Lerp(transform.right, camRight, Time.deltaTime * reOrientSpeed);
                }
                break;
        }
    }

    /// <summary>
    /// Handles state specific gameplay action inputs. Should NOT be used for state transitions, those belong in ProcessStateMachine().
    /// </summary>
    public void HandleActionInput()
    {
        switch (currentState)
        {
            case PlayerState.Aiming:
                if (Input.GetButtonDown("FireArrow"))
                {
                    // Fire arrow
                    animHandler.PlayOneShotAnim("Fire");
                }
                break;
        }
    }

    /// <summary>
    /// Callback for whenever the players current state is changed.
    /// Contains two switch statements that allow for different behavior based on the states we are entering and exiting.
    /// </summary>
    /// <param name="aeNewState">The state we're now in.</param>
    /// <param name="aeOldState">The state we just came from.</param>
    private void OnStateChanged(PlayerState aeNewState, PlayerState aeOldState)
    {
        // Do stuff for when we exit a state here
        switch (aeOldState)
        {
            case PlayerState.Idle:
                StopCoroutine(pickFlavorCoroutine);
                break;
        }

        // Do stuff for when we enter a state here
        switch (aeNewState)
        {
            case PlayerState.Idle:
                pickFlavorCoroutine = RequestIdleFlavourRoutine();
                StartCoroutine(pickFlavorCoroutine);
                break;
        }
    }

    /// <summary>
    /// Get the current speed based on the players state.
    /// </summary>
    /// <returns>The current states movement speed.</returns>
    float QCurrentSpeed()
    {
        float fOut = 0;
        switch (currentState)
        {
            case PlayerState.Walking:
                fOut = speed_Walk;
                break;
            case PlayerState.Running:
                fOut = speed_Run;
                break;
            case PlayerState.Aiming:
                fOut = speed_Aim;
                break;
            case PlayerState.Crouched:
                fOut = speed_Crouch;
                break;
        }
        return fOut;
    }

    /// <summary>
    /// Get the players current state.
    /// </summary>
    /// <returns>Players current state enum.</returns>
    public PlayerState QState() { return currentState; }

    /// <summary>
    /// Return a reference the player rigs camera bone. Used for animating the camera with canned anims.
    /// </summary>
    /// <returns>Transform for the players camera bone.</returns>
    public Transform QAnimCameraBone()
    {
        return animHandler.cameraAnimBone;
    }

    /// <summary>
    /// Coroutine for handling idle flavour animations ona  loop while in an Idle state.
    /// </summary>
    IEnumerator RequestIdleFlavourRoutine()
    {
        while (currentState == PlayerState.Idle)
        {
            yield return new WaitForSeconds(Random.Range(IdleFlavourPickIntervals.x, IdleFlavourPickIntervals.y));
            animHandler.PlayIdleFlavor();
        }
    }
}
