using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// General handler class for player animations
/// -----------------------------------------------
/// Mostly updates the Animation Controller with data passed from PlayerCharacter.cs, but also handles animation events sent during clips.
/// Any oneshot animations MUST be sent through this class to ensure the proper state flow is followed.
/// </summary>
public class PlayerAnimHandler : MonoBehaviour
{
    public Animator anim;
    public float animVarInterpSpeed_Input;
    [Space(10)]
    public string VariableName_InputX = "InputX";
    public string VariableName_InputY = "InputY";
    public string VariableName_Aiming = "bIsAiming";
    public string VariableName_Running = "bIsRunning";
    public string VariableName_Crouching = "bIsCrouching";
    public string VariableName_AimAngle = "AimAngle";
    public string VariableName_HeadtrackX = "Headtrack_X";
    public string VariableName_HeadtrackY = "Headtrack_Y";
    [Space(5)]
    public string[] StateName_IdleFlavors;
    [Space(10)]
    public Transform cameraAnimBone;
    public CameraRig camRig;
    [Space(10)]
    public Vector2 camUpDownEulerClamps;
    public int aimLayerIndex = 1;
    public int headtrackLayerIndex = 2;
    [Space(10)]
    public Transform firePoint;
    public Transform firePointAngled;
    [Space(10)]
    public GameObject[] props;

    [HideInInspector]
    public bool isPlayingCannedAnim;

    private Vector2 animVar_Input;
    private bool aiming;

    /// <summary>
    /// General function to handle all anim varibale updates. 
    /// </summary>
    /// <param name="avInput">Vector2 containing the X/Y inputs froms the player.</param>
    /// <param name="aePlayerState">Enum pertaining to the players current state.</param>
    /// <param name="afCamEuler">The cam rigs up/down rotator bones local euler X.</param>
    public void HandleAnimVariables(Vector2 avInput, PlayerCharacter.PlayerState aePlayerState, float afCamEuler)
    {
        // Store corrent states in seperate bools. Helps readability going forwards.
        bool bRunning = aePlayerState == PlayerCharacter.PlayerState.Running;
        bool bCrouching = aePlayerState == PlayerCharacter.PlayerState.Crouched;
        aiming = aePlayerState == PlayerCharacter.PlayerState.Aiming;

        animVar_Input = Vector2.Lerp(animVar_Input, avInput, Time.deltaTime * animVarInterpSpeed_Input);
        anim.SetFloat(VariableName_InputX, animVar_Input.x);
        anim.SetFloat(VariableName_InputY, animVar_Input.y);
        // Aiming check
        bool bHasChangedAimState = anim.GetBool(VariableName_Aiming) != aiming;
        if (bHasChangedAimState)
        {
            if (aiming)
            {
                EnableProp(0);  // Arrow is always first in the prop list
            }
            else
            {
                DisableProp(0);
            }
        }
        anim.SetBool(VariableName_Aiming, aiming);
        anim.SetBool(VariableName_Running, bRunning);
        anim.SetBool(VariableName_Crouching, bCrouching);

        // Calculate the characters aim angle based on their up/down cam euler
        float handledCamX = afCamEuler > 180 ? afCamEuler - 360 : afCamEuler;                                  // Quick and dirty conversion from negative numbers to handled euler angles.
        float aimAngle = handledCamX / Mathf.Max(camUpDownEulerClamps.x, camUpDownEulerClamps.y);
        anim.SetFloat(VariableName_AimAngle, aimAngle);
        // Interpolate to the proper aim layer weight
        anim.SetLayerWeight(aimLayerIndex, Mathf.Lerp(anim.GetLayerWeight(aimLayerIndex), aiming ? 1 : 0, Time.deltaTime * 5f));
    }

    /// <summary>
    /// Passes the headtrack variables to the animator and handles the layer weighting. This is sent from HeadtrackHandler.cs, rather than PlayerCharacter.cs, hence the seperate function.
    /// </summary>
    /// <param name="avDeltas">Vector containing the current X/Y headtrack magnitudes to be passed to the animator's blend tree.</param>
    public void HandleHeadtrack(Vector2 avDeltas)
    {
        anim.SetFloat(VariableName_HeadtrackX, avDeltas.x);
        anim.SetFloat(VariableName_HeadtrackY, avDeltas.y);

        anim.SetLayerWeight(headtrackLayerIndex, Mathf.Lerp(anim.GetLayerWeight(headtrackLayerIndex), aiming ? 0 : 1, Time.deltaTime * 5f));
    }

    /// <summary>
    /// Plays a single animation as a one-off.
    /// </summary>
    /// <param name="asAnimName">Name of the animation state as it appears in the Animator.</param>
    public void PlayOneShotAnim(string asAnimName)
    {
        anim.Play(asAnimName);
    }

    /// <summary>
    /// Similar to PlayOneSHotAnim(), but resets our state ready to play a canned animation.
    /// </summary>
    /// <param name="asAnimName">Name of the animation state as it appears in the Animator.</param>
    public void PlayCannedAnim(string asAnimName)
    {
        isPlayingCannedAnim = true;
        // Reset anim state
        animVar_Input = Vector2.zero;
        anim.SetBool(VariableName_Aiming, false);
        // Play anim
        anim.Play(asAnimName);
    }

    /// <summary>
    /// If we can, pick a random idle flavor animation and play it.
    /// </summary>
    public void PlayIdleFlavor()
    {
        if (StateName_IdleFlavors.Length > 0)
        {
            anim.Play(StateName_IdleFlavors[Random.Range(0, StateName_IdleFlavors.Length)]);
        }
        else 
        {
            Debug.LogWarning("PlayIdleFlavor | No idles to pick from.");
        }
    }


    // ------- Animation Events -----------
    public void FireProjectile(Projectile apProjectile)
    {
        apProjectile.Initialize(firePoint, firePointAngled, camRig.QCam());
    }

    public void EnableProp(int aiID)
    {
        props[aiID].SetActive(true);
    }

    public void DisableProp(int aiID)
    {
        props[aiID].SetActive(false);
    }

    /// <summary>
    /// Notify the camera that we are exiting a canned anim. Allows the camera to transition back to a gameplay driven behavior.
    /// </summary>
    public void ExitCannedAnimCameraNotify()
    {
        camRig.exitCannedAnim = true;
    }

    public void ExitCannedAnim()
    {
        isPlayingCannedAnim = false;
    }
}
