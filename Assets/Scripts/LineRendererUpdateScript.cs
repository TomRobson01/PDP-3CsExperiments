using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple component that allows for a Line Renderer to update its positions to match an array of reference points.
/// </summary>
public class LineRendererUpdateScript : MonoBehaviour
{
    public Transform[] points;
    public LineRenderer rend;

    // Update is called once per frame
    void Update()
    {
        rend.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            rend.SetPosition(i, points[i].position);
        }
    }
}
