using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public float speed;

    private Rigidbody rb;

    /// <summary>
    /// Called when the Player GameObject is initialised by the Unity engine.
    /// 
    /// Obtains a reference to the players rigid body component.
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Called approximately every 0.02 seconds by the Unity Engine.
    /// 
    /// Sets movement and visibility of the sphere that is the Player object. (Invisible by default, visibility for debugging purposes)
    /// </summary>
    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        //Button 10 (Push left joystick down) toggles visibility of the sphere.
        if (Input.GetKeyDown("joystick 1 button 10"))
        {
            gameObject.GetComponent<Renderer>().enabled = !gameObject.GetComponent<Renderer>().enabled;
        }

        //Button 11 (Push right joystick down) toggles base direction of movement.
        if (Input.GetKeyDown("joystick 1 button 11"))
        {
            this.speed *= -1;
        }

        //Force is applied to sphere based on joystick left stick input.
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        if (movement == Vector3.zero) rb.velocity = new Vector3(rb.velocity.x/2, rb.velocity.y, rb.velocity.z / 2);
        else rb.AddForce(movement * speed);
    }
}
