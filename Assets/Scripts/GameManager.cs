using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameplayState
    {
        Resting,
        Combat
    }

    public static GameplayState state;
}
