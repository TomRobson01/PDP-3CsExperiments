using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager class for updating the crosshairs Animator.
/// </summary>
public class CrosshairManager : MonoBehaviour
{
    public Animator anim;
    public string VariableName_IsAiming = "bIsAiming";
    public string VariableName_MoveDelta = "MoveDelta";

    /// <summary>
    /// Sends current values to the crosshair UIs Animator.
    /// </summary>
    /// <param name="abIsAiming">Whether or not the player is aiming. This decides if the crosshair is visible or not.</param>
    /// <param name="afInputMagnitude">The current magnitude of the players input. Determines how wide the crosshair is.</param>
    public void UpdateCrosshair(bool abIsAiming, float afInputMagnitude)
    {
        anim.SetBool(VariableName_IsAiming, abIsAiming);
        anim.SetFloat(VariableName_MoveDelta, afInputMagnitude);
    }
}
