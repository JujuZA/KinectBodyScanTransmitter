using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to modify the camera's position and rotation based on joystick input and Player GameObject movement.
/// </summary>
public class CameraController : MonoBehaviour {

    public GameObject player;
    private Vector3 offset;
    private float horizFlip;
    private Transform ctransform;

    /// <summary>
    /// Called when CameraController GameObject is initialised by the Unity engine.
    /// 
    /// Calculates the CameraController GameObject's position offset from the Player GameObject.
    /// </summary>
    void Start () {
		this.ctransform = GetComponent<Transform>();
        this.offset = ctransform.position - player.transform.position;
        this.horizFlip = 0f;
    }

    /// <summary>
    /// Called approximately every 0.02 seconds by the Unity Engine.
    /// 
    /// Modifies the position, rotation and base direction of the camera object.
    /// </summary>
    void FixedUpdate () {
        float lookHoriz = Input.GetAxis("LookX");
        float lookVert = Input.GetAxis("LookY");

        //Button 11 (Push right joystick down) flips the base direction.
        if (Input.GetKeyDown("joystick 1 button 11"))
        {
            offset = -offset;
            horizFlip = (horizFlip+180) % 360;
        }                  

        //Offset between camera and player is maintained
        this.ctransform.position = player.transform.position + offset;
        //Right joystick temporarily shifts viewing angle (Occulus viewing angle is used as default snap-back)
        this.ctransform.rotation = Quaternion.Euler(lookVert * 90, lookHoriz * 180 + horizFlip, 0);
    }
}
