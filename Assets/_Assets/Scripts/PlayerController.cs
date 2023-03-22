using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("Self-References")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform raycastParent;

    [Header("Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float accelSpeed_ground;
    [SerializeField] private float frictionSpeed_ground;
    [SerializeField] private float accelSpeed_air;
    [SerializeField] private float frictionSpeed_air;
    [Space(5)]
    [SerializeField] private float jumpPower;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastHeight;

    [Header("Settings")]
    [SerializeField] private float turnSpeedX;
    [SerializeField] private float turnSpeedY;

    [Header("Temp")]
    [SerializeField] private Vector3 currNetGrav_Global = Vector3.down;

    float targHorizontalSpin;
    float targVerticalSpin;

    bool grounded = false;

    List<Transform> raycastPoints = new List<Transform>();

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();

        for (int i = 0; i < raycastParent.childCount; i++)
        {
            raycastPoints.Add(raycastParent.GetChild(i));
        }
    }

    void Update()
    {
        if (InputHandler.Instance.Jump.Down)
        {
            if (grounded)
                rb.velocity += transform.up * jumpPower;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        #region Ground Checking
        grounded = false;
        foreach (Transform rayPoint in raycastPoints) //check ground positions
        {
            //Debug.DrawLine(rayPoint.position, rayPoint.position + Vector3.down*raycastHeight, Color.magenta, 0.1f, false);
            if (Physics.Raycast(rayPoint.position, Vector3.down, raycastHeight, groundLayer))
            {
                grounded = true;
                break;
            }
        }
        #endregion


        #region Camera Look Input
        targHorizontalSpin += turnSpeedX * InputHandler.Instance.Look.x;

        targVerticalSpin += turnSpeedY * InputHandler.Instance.Look.y;
        if (targVerticalSpin > 180)
            targVerticalSpin -= 360;
        targVerticalSpin = Utils.Clamp(targVerticalSpin, -80f, 80f);
        #endregion


        #region Apply Camera Look
        //Horizontal
        Vector3 newRot = transform.eulerAngles;
        newRot.y = Utils.Lerp360(newRot.y, targHorizontalSpin, 0.5f);
        transform.eulerAngles = newRot;

        //Vertical
        newRot = headTransform.localEulerAngles;
        newRot.x = Utils.Lerp360(newRot.x, targVerticalSpin, 0.5f);
        headTransform.localEulerAngles = newRot;
        #endregion


        #region Gravity
        //Apply gravity
        rb.velocity += currNetGrav_Global;
        #endregion


        #region Planar velocity
        //Get gravityless velocity
        Vector3 onlyGravVelocity = Vector3.Project(rb.velocity, currNetGrav_Global.normalized);
        Vector3 noGravVelocity = rb.velocity - onlyGravVelocity;

        //Convert global velocity to local velocity
        Vector3 velocity_local = transform.InverseTransformDirection(noGravVelocity);

        //Apply friction
        Vector3 velocity_local_friction;
        if (grounded)
            velocity_local_friction = velocity_local.normalized * Mathf.Max(0, velocity_local.magnitude - frictionSpeed_ground);
        else
            velocity_local_friction = velocity_local.normalized * Mathf.Max(0, velocity_local.magnitude - frictionSpeed_air);
        Vector3 updatedVelocity = velocity_local_friction;

        Vector3 currInput = new Vector3(InputHandler.Instance.Dir.x, 0, InputHandler.Instance.Dir.y);
        if (currInput.magnitude > 0.05f) //Pressing something, try to accelerate
        {
            Vector3 velocity_local_input;
            if (grounded)
                velocity_local_input = velocity_local_friction + currInput * accelSpeed_ground;
            else
                velocity_local_input = velocity_local_friction + currInput * accelSpeed_air;

            if (velocity_local_friction.magnitude <= maxSpeed)
            {
                //under max speed, accelerate towards max speed
                updatedVelocity = velocity_local_input.normalized * Mathf.Min(maxSpeed, velocity_local_input.magnitude);
            }
            else
            {
                //over max speed
                if (velocity_local_input.magnitude <= maxSpeed) //Use new direction, would go less than max speed
                {
                    updatedVelocity = velocity_local_input;
                }
                else //Would stay over max speed, use vector with smaller magnitude
                {
                    //Would accelerate more, so don't user player input
                    if (velocity_local_input.magnitude > velocity_local_friction.magnitude)
                        updatedVelocity = velocity_local_friction;
                    else
                        //Would accelerate less, user player input (input moves velocity more to 0,0 than just friciton)
                        updatedVelocity = velocity_local_input;
                }
            }
        }

        //Convert local velocity to global velocity
        rb.velocity = onlyGravVelocity + transform.TransformDirection(updatedVelocity);

        #endregion
    }
}
