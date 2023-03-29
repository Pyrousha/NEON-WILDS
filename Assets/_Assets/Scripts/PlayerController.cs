using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("Self-References")]
    [SerializeField] private Transform raycastParent;
    [SerializeField] private Animator bodySwingAnim;
    [SerializeField] private Transform cameraTarget;

    [Header("External References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float accelSpeed_ground;
    [SerializeField] private float frictionSpeed_ground;
    [SerializeField] private float accelSpeed_air;
    [SerializeField] private float frictionSpeed_air;
    [Space(10)]
    [SerializeField] private float jumpPower;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastHeight;
    [Space(5)]
    [SerializeField] private float lerpSpeed = 0.5f;
    [SerializeField] private float gravMagnitudeCutoffForLerp_DEBUG;
    [SerializeField] private float gravMagnitudeCutoffForLerp_Normal;
    [SerializeField] private bool useDebugLerpCutoff;

    [Header("Settings")]
    [SerializeField] private float turnSpeedX;
    [SerializeField] private float turnSpeedY;

    float targHorizontalSpin;
    float targVerticalSpin;

    bool grounded = false;
    bool inOrbit;

    private Vector3 currNetGrav_Global = Vector3.down;

    List<Transform> raycastPoints = new List<Transform>();

    void Awake()
    {
        //Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();

        for (int i = 0; i < raycastParent.childCount; i++)
        {
            raycastPoints.Add(raycastParent.GetChild(i));
        }
    }

    void Update()
    {
        //Jump
        if (InputHandler.Instance.Jump.Down)
        {
            if (grounded)
                rb.velocity += transform.up * jumpPower;
        }

        if (inOrbit)
        {
            //Camera Spin horizontal
            float amountToTurn = turnSpeedX * InputHandler.Instance.Look.x * Time.deltaTime;
            transform.rotation = Quaternion.AngleAxis(amountToTurn, transform.up) * transform.rotation;

            //Camera Spin vertical
            targVerticalSpin -= turnSpeedY * InputHandler.Instance.Look.y * Time.deltaTime;
            targVerticalSpin = Mathf.Clamp(targVerticalSpin, -80.5f, 80.5f);
            cameraTarget.localRotation = Quaternion.Euler(targVerticalSpin, 0, 0);

            // //Make actual camera be facing in same direction as target
            // cameraTransform.rotation = cameraTarget.rotation;
        }
        else
        {
            //Camera Spin horizontal
            float amountToTurn = turnSpeedX * InputHandler.Instance.Look.x * Time.deltaTime;
            transform.rotation = Quaternion.AngleAxis(amountToTurn, Camera.main.transform.up) * transform.rotation;

            //Camera Spin vertical
            amountToTurn = -turnSpeedY * InputHandler.Instance.Look.y * Time.deltaTime;
            transform.rotation = Quaternion.AngleAxis(amountToTurn, transform.right) * transform.rotation;
        }

        cameraTransform.position = cameraTarget.position;

        if (faceCamera)
        {
            transform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTarget.up);
            cameraTarget.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
            cameraTransform.rotation = cameraTarget.rotation;

        //Jump
        if (InputHandler.Instance.Jump.Down)
        {
            if (grounded)
                rb.velocity += transform.up * jumpPower;
        }
    }

    private bool faceCamera = false;

    private Coroutine exitOrbitCoroutine;
    private IEnumerator OnExitOrbit()
    {
        bodySwingAnim.ResetTrigger("SwingForwards");
        bodySwingAnim.SetTrigger("SwingBack");

        //Wait for body to swing back
        yield return new WaitForSeconds(10f / 60f);

        faceCamera = true;

        //Quaternion rot = cameraTransform.rotation;
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward, Color.red, 5f);
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 0.5f, Color.blue, 5f);
        Debug.Log("Start spin!");
        transform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTarget.up);
        Debug.Log("End spin");
        yield return null;
        yield return new WaitForFixedUpdate();

        yield return new WaitForSeconds(10f / 60f);
        faceCamera = false;

        Debug.DrawRay(transform.position, transform.forward, Color.green, 5f);
        Debug.DrawRay(transform.position, transform.forward * 0.5f, Color.yellow, 5f);

        // Quaternion targRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        // transform.rotation = targRotation;
        // cameraTransform.localRotation = Quaternion.identity;

        exitOrbitCoroutine = null;
    }

    private Coroutine enterOrbitCoroutine;
    private IEnumerator OnEnterOrbit()
    {
        faceCamera = false;

        targVerticalSpin = cameraTransform.localEulerAngles.x;

        bodySwingAnim.ResetTrigger("SwingBack");
        bodySwingAnim.SetTrigger("SwingForwards");

        //Wait for body to swing back
        yield return new WaitForSeconds(10f / 60f);

        enterOrbitCoroutine = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Debug.DrawRay(transform.position, transform.forward * 1.6f / 2f, new Color(1, 0, 0), 5f);
        // Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 1.5f / 2f, new Color(0, 1, 0), 5f);

        #region Calculate net Gravity dir
        float preGravMagnitude = currNetGrav_Global.magnitude;
        currNetGrav_Global = PlanetTracker.Instance.GetNetGravityDir(transform.position);

        float gravMagnitudeCutoffForLerp;
        if (useDebugLerpCutoff)
            gravMagnitudeCutoffForLerp = gravMagnitudeCutoffForLerp_DEBUG;
        else
            gravMagnitudeCutoffForLerp = gravMagnitudeCutoffForLerp_Normal;

        bool newInOrbit;
        if (currNetGrav_Global.magnitude >= gravMagnitudeCutoffForLerp)
            newInOrbit = true;
        else
            newInOrbit = false;

        if (inOrbit && newInOrbit == false)
        {
            inOrbit = newInOrbit;

            //Exit orbit
            if (enterOrbitCoroutine != null)
                StopCoroutine(enterOrbitCoroutine);
            if (exitOrbitCoroutine != null)
                StopCoroutine(exitOrbitCoroutine);

            //Start exit coroutine
            exitOrbitCoroutine = StartCoroutine(OnExitOrbit());
        }
        else
        {
            if (inOrbit == false && newInOrbit)
            {
                inOrbit = newInOrbit;

                //Enter orbit
                if (enterOrbitCoroutine != null)
                    StopCoroutine(enterOrbitCoroutine);
                if (exitOrbitCoroutine != null)
                    StopCoroutine(exitOrbitCoroutine);

                //Start enter coroutine
                enterOrbitCoroutine = StartCoroutine(OnEnterOrbit());
            }
            else
                inOrbit = newInOrbit;
        }

        //Debug.DrawRay(transform.position, currNetGrav_Global, Color.red, 0.5f);
        #endregion


        #region determine if player is grounded or not, and calulate desired new "up" direction accordingly
        grounded = false; //number of raycasts that hit the ground 
        Vector3 compositeNormalDirection = new Vector3(); // sum of all normal directions from all raycastst that hit the ground

        foreach (Transform point in raycastPoints)
        {
            if (Physics.Raycast(point.position, -transform.up, out RaycastHit hit, raycastHeight, groundLayer))
            {
                //this ray hit the ground, save to calculate average normal direction of all rays that hit the ground
                compositeNormalDirection += hit.normal;
                grounded = true;
            }
        }

        bool doLerp = false;
        Vector3 targetUpDir = Vector3.zero; // new up direction for the player to rotate towards

        if (grounded)
        {
            //Average normals from all raycasts that hit the ground
            targetUpDir = compositeNormalDirection.normalized;
            doLerp = true;
        }
        else //no rays hit the ground, so player is not grounded
        {
            if (inOrbit)
            {
                //Gravity strong enough to lerp
                targetUpDir = -currNetGrav_Global.normalized;
                doLerp = true;
            }
        }
        #endregion


        #region lerp player to rotate towards new up direction
        if (doLerp)
        {
            //Calculate new up-direction and rotate player
            Vector3 newPlayerForward = (transform.forward - Vector3.Project(transform.forward, targetUpDir)).normalized;
            Quaternion targRotation = Quaternion.LookRotation(newPlayerForward, targetUpDir);

            //Lerp up-direction (or jump if close enough)
            float angleDiff = Quaternion.Angle(transform.rotation, targRotation);
            if (angleDiff > lerpSpeed)
            {
                //Angle between current rotation and target rotation big enough to lerp
                transform.rotation = Quaternion.Lerp(transform.rotation, targRotation, lerpSpeed * (1f / angleDiff));
            }
            else
            {
                //current and target rotation are close enough, jump value to stop lerp from going forever
                transform.rotation = targRotation;
            }
        }
        #endregion


        #region Calculate and apply Gravity
        //Apply gravity
        rb.velocity += currNetGrav_Global;
        #endregion


        #region Acceleration
        //Get gravityless velocity
        Vector3 onlyGravVelocity = Vector3.Project(rb.velocity, currNetGrav_Global.normalized);
        Vector3 noGravVelocity = rb.velocity - onlyGravVelocity;

        //Convert global velocity to local velocity
        Vector3 velocity_local = transform.InverseTransformDirection(noGravVelocity);


        //XZ Friction + acceleration
        if (inOrbit)
        {
            Vector3 currInput = new Vector3(InputHandler.Instance.MoveXZ.x, 0, InputHandler.Instance.MoveXZ.y);
            if (grounded)
            {
                //Apply ground fricion
                Vector3 velocity_local_friction = velocity_local.normalized * Mathf.Max(0, velocity_local.magnitude - frictionSpeed_ground);

                Vector3 updatedVelocity = velocity_local_friction;

                if (currInput.magnitude > 0.05f) //Pressing something, try to accelerate
                {
                    Vector3 velocity_local_input = velocity_local_friction + currInput * accelSpeed_ground;

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
            }
            else
            {
                //In-air, jetpack time!
                if (currInput.magnitude > 0.05f) //Pressing something, try to accelerate
                {
                    Vector3 velocity_local_input = currInput * accelSpeed_air;

                    //Convert local velocity to global velocity
                    rb.velocity += transform.TransformDirection(velocity_local_input);
                }
            }

            //Up/down jetpack
            Vector3 upDownInput = new Vector3(0, InputHandler.Instance.MoveY, 0);
            if (upDownInput.magnitude > 0.05f)
            {
                //Convert local velocity to global velocity
                rb.velocity += transform.TransformDirection(upDownInput * accelSpeed_air);
            }
        }
        else
        {
            Vector3 currInput = new Vector3(InputHandler.Instance.MoveXZ.x, InputHandler.Instance.MoveY, InputHandler.Instance.MoveXZ.y);

            //In-air, jetpack time!
            if (currInput.magnitude > 0.05f) //Pressing something, try to accelerate
            {
                //Convert local velocity to global velocity
                rb.velocity += cameraTransform.TransformDirection(currInput * accelSpeed_air);
            }
        }
        #endregion

        // Debug.DrawRay(transform.position, transform.forward * 1.2f / 2f, new Color(1, 1, 0), 5f);
        // Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 1.1f / 2f, new Color(0.25f, 0, 1), 5f);
    }
}
