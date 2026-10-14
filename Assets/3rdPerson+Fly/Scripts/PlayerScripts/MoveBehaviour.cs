using UnityEngine;
using UnityEngine.Serialization;

// MoveBehaviour inherits from GenericBehaviour. This class corresponds to basic walk and run behaviour, it is the default behaviour.
public class MoveBehaviour : GenericBehaviour
{
    public float walkSpeed = 0.5f;                  // Default walk speed.
    public float runSpeed = 2.0f;                   // Default run speed.
    public float sprintSpeed = 4.0f;                // Default sprint speed.
    public float speedDampTime = 0.1f;              // Default damp time to change the animations based on current speed.
    public float animatorSpeedMultiplier = 1.0f;    // Multiplier to scale Root Motion velocity.
    public string jumpButton = "Jump";              // Default jump button.
    public float jumpHeight = 1.5f;                 // Default jump height.
    public float jumpInertialForce = 10f;           // Default horizontal inertial force when jumping.

    private float speed;                            // Moving speed.
    private int jumpBool;                           // Animator variable related to jumping.
    private int groundedBool;                       // Animator variable related to whether or not the player is on ground.
    private bool jump;                              // Boolean to determine whether or not the player started a jump.
    private bool isColliding;                       // Boolean to determine if the player has collided with an obstacle.

    // Start is always called after any Awake functions.
    void Start()
    {
        // Set up the references.
        jumpBool = Animator.StringToHash("Jump");
        groundedBool = Animator.StringToHash("Grounded");
        behaviourManager.GetAnim.SetBool(groundedBool, true);

        // Subscribe and register this behaviour as the default behaviour.
        behaviourManager.SubscribeBehaviour(this);
        behaviourManager.RegisterDefaultBehaviour(this.behaviourCode);
    }

    // Update is used to set features regardless the active behaviour.
    void Update()
    {
        // Get jump input.
        if (!jump && Input.GetButtonDown(jumpButton) && behaviourManager.IsCurrentBehaviour(this.behaviourCode) && !behaviourManager.IsOverriding())
        {
            jump = true;
        }
    }

    // LocalFixedUpdate overrides the virtual function of the base class.
    public override void LocalFixedUpdate()
    {
        // Call the basic movement manager.
        MovementManagement(behaviourManager.GetH, behaviourManager.GetV);

        // Call the jump manager.
        JumpManagement();
    }

    // Intercepta el movimiento por raíz (Root Motion) para aplicarlo manualmente con un multiplicador de velocidad
    void OnAnimatorMove()
    {
        if (behaviourManager != null && behaviourManager.GetAnim != null && behaviourManager.IsGrounded() && !behaviourManager.GetAnim.GetBool(jumpBool))
        {
            // Toma el delta de posición calculado por el Animator debido al Root Motion y multiplícalo por tu escala
            Vector3 movementDelta = behaviourManager.GetAnim.deltaPosition * animatorSpeedMultiplier;
            movementDelta.y = behaviourManager.GetRigidBody.linearVelocity.y * Time.deltaTime; // Preserva la gravedad

            // Mueve el Rigidbody usando el desplazamiento del Root Motion modificado
            behaviourManager.GetRigidBody.MovePosition(behaviourManager.GetRigidBody.position + movementDelta);

            // Aplica la rotación del Root Motion de forma fluida
            Quaternion newRotation = behaviourManager.GetRigidBody.rotation * behaviourManager.GetAnim.deltaRotation;
            behaviourManager.GetRigidBody.MoveRotation(newRotation);
        }
    }

    // Execute the idle and walk/run jump movements.
    void JumpManagement()
    {
        // Start a new jump.
        if (jump && !behaviourManager.GetAnim.GetBool(jumpBool) && behaviourManager.IsGrounded())
        {
            jump = false;

            // Set jump related parameters.
            behaviourManager.LockTempBehaviour(this.behaviourCode);
            behaviourManager.GetAnim.SetBool(jumpBool, true);
            behaviourManager.GetAnim.SetBool(groundedBool, false);

            // Temporarily change player friction to pass through obstacles.
            GetComponent<CapsuleCollider>().material.dynamicFriction = 0f;
            GetComponent<CapsuleCollider>().material.staticFriction = 0f;

            // Set jump vertical impulse velocity.
            float jumpVelY = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);

            // Preservar la inercia horizontal natural según la velocidad y dirección actual
            Vector3 horizontalVelocity = Vector3.zero;
            if (speed > 0.1f)
            {
                horizontalVelocity = transform.forward * speed;
            }

            // Asignar velocidad inicial limpia al Rigidbody (sin aceleraciones continuas descontroladas)
            behaviourManager.GetRigidBody.linearVelocity = new Vector3(horizontalVelocity.x, jumpVelY, horizontalVelocity.z);
        }
        // Is already jumping?
        else if (behaviourManager.GetAnim.GetBool(jumpBool))
        {
            // Control en el aire suave y acotado a la velocidad máxima permitida
            if (!behaviourManager.IsGrounded() && !isColliding)
            {
                Vector2 dir = new Vector2(behaviourManager.GetH, behaviourManager.GetV);
                float maxAirSpeed = behaviourManager.IsSprinting() ? sprintSpeed : runSpeed;

                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 targetDir = Rotating(behaviourManager.GetH, behaviourManager.GetV);
                    Vector3 targetHoriz = targetDir * maxAirSpeed;
                    Vector3 curVel = behaviourManager.GetRigidBody.linearVelocity;
                    Vector3 curHoriz = new Vector3(curVel.x, 0, curVel.z);

                    // Ajuste de trayectoria en el aire sin propulsión infinita
                    Vector3 newHoriz = Vector3.MoveTowards(curHoriz, targetHoriz, maxAirSpeed * 3f * Time.deltaTime);
                    behaviourManager.GetRigidBody.linearVelocity = new Vector3(newHoriz.x, curVel.y, newHoriz.z);
                }
                else
                {
                    // Asegurar que la velocidad horizontal nunca exceda la velocidad de sprint
                    Vector3 curVel = behaviourManager.GetRigidBody.linearVelocity;
                    Vector3 curHoriz = Vector3.ClampMagnitude(new Vector3(curVel.x, 0, curVel.z), maxAirSpeed);
                    behaviourManager.GetRigidBody.linearVelocity = new Vector3(curHoriz.x, curVel.y, curHoriz.z);
                }
            }

            // Has landed? (solo cuando comienza a descender o toca el suelo)
            if ((behaviourManager.GetRigidBody.linearVelocity.y <= 0f) && behaviourManager.IsGrounded())
            {
                behaviourManager.GetAnim.SetBool(groundedBool, true);
                // Change back player friction to default.
                GetComponent<CapsuleCollider>().material.dynamicFriction = 0.6f;
                GetComponent<CapsuleCollider>().material.staticFriction = 0.6f;
                // Set jump related parameters.
                jump = false;
                behaviourManager.GetAnim.SetBool(jumpBool, false);
                behaviourManager.UnlockTempBehaviour(this.behaviourCode);
            }
        }
    }

    // Deal with the basic player movement
    void MovementManagement(float horizontal, float vertical)
    {
        // On ground, obey gravity.
        if (behaviourManager.IsGrounded())
            behaviourManager.GetRigidBody.useGravity = true;

        // Avoid takeoff when reached a slope end.
        else if (!behaviourManager.GetAnim.GetBool(jumpBool) && behaviourManager.GetRigidBody.linearVelocity.y > 0)
        {
            RemoveVerticalVelocity();
        }

        // Call function that deals with player orientation.
        Rotating(horizontal, vertical);

        // Set proper speed based strictly on input direction and keyboard states.
        Vector2 dir = new Vector2(horizontal, vertical);
        float inputMagnitude = Vector2.ClampMagnitude(dir, 1f).magnitude;

        if (inputMagnitude > 0.1f)
        {
            speed = walkSpeed;
        }
        else
        {
            speed = 0f;
        }

        if (inputMagnitude > 0.5f)
        {
            speed = runSpeed;
        }

        if (behaviourManager.IsSprinting())
        {
            speed = sprintSpeed;
        }

        // Envía el valor al Blend Tree para cambiar la animación visual
        behaviourManager.GetAnim.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);
    }

    // Remove vertical rigidbody velocity.
    private void RemoveVerticalVelocity()
    {
        Vector3 horizontalVelocity = behaviourManager.GetRigidBody.linearVelocity;
        horizontalVelocity.y = 0;
        behaviourManager.GetRigidBody.linearVelocity = horizontalVelocity;
    }

    // Rotate the player to match correct orientation, according to camera and key pressed.
    Vector3 Rotating(float horizontal, float vertical)
    {
        // Get camera forward direction, without vertical component.
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);

        // Player is moving on ground, Y component of camera facing is not relevant.
        forward.y = 0.0f;
        forward = forward.normalized;

        // Calculate target direction based on camera forward and direction key.
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 targetDirection = forward * vertical + right * horizontal;

        // Lerp current direction to calculated target direction.
        if ((behaviourManager.IsMoving() && targetDirection != Vector3.zero))
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Quaternion newRotation = Quaternion.Slerp(behaviourManager.GetRigidBody.rotation, targetRotation, behaviourManager.turnSmoothing);
            behaviourManager.GetRigidBody.MoveRotation(newRotation);
            behaviourManager.SetLastDirection(targetDirection);
        }
        // If idle, Ignore current camera facing and consider last moving direction.
        if (!(Mathf.Abs(horizontal) > 0.9 || Mathf.Abs(vertical) > 0.9))
        {
            behaviourManager.Repositioning();
        }

        return targetDirection;
    }

    // Collision detection.
    private void OnCollisionStay(Collision collision)
    {
        isColliding = true;
        // Slide on vertical obstacles
        if (behaviourManager.IsCurrentBehaviour(this.GetBehaviourCode()) && collision.GetContact(0).normal.y <= 0.1f)
        {
            GetComponent<CapsuleCollider>().material.dynamicFriction = 0f;
            GetComponent<CapsuleCollider>().material.staticFriction = 0f;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        GetComponent<CapsuleCollider>().material.dynamicFriction = 0.6f;
        GetComponent<CapsuleCollider>().material.staticFriction = 0.6f;
    }
}