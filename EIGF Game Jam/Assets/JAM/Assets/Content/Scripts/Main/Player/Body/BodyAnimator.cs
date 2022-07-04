/*
 * BodyAnimator.cs - wirted by ThunderWire Games
 * ver. 1.0
*/

using System.Collections;
using UnityEngine;

public class BodyAnimator : MonoBehaviour
{
    private HFPS_GameManager gameManager;
    private PlayerController controller;
    private Animator anim;
    private Transform cam;

    [Header("Main")]
    public Transform MiddleSpine;
    public LayerMask InvisibleMask;

    public float TurnSmooth;
    public float AdjustSmooth;

    public float OverrideSmooth;
    public float BackOverrideSmooth;

    [Header("Right Strafe")]
    public float RightAngle;
    public float UpRightAngle;
    public float BackRightAngle;

    [Header("Left Strafe")]
    public float LeftAngle;
    public float UpLeftAngle;
    public float BackLeftAngle;

    [Header("Misc")]
    public bool enableShadows = true;
    public bool visibleToCamera = true;
    public bool proneDisableBody;

    [Header("Body Adjustment")]
    [Space(10)]
    public Vector3 originalOffset;
    [Space(5)]
    public Vector3 runningOffset;
    [Space(5)]
    public Vector3 crouchOffset;
    [Space(5)]
    public Vector3 jumpOffset;
    [Space(5)]
    public Vector3 proneOffset;
    [Space(5)]
    public Vector3 turnOffset;
    [Space(10)]
    public Vector3 bodyAngle;
    [Space(5)]
    public Vector2 spineMaxRotation;

    private Vector3 localEuler;
    private float tempArmsWeight = 0;

    private float mouseRotation;
    private float inputX;
    private float inputY;
    private float spineAngle;

    private bool m_fwd;
    private bool m_bwd;
    private bool m_lt;
    private bool m_rt;

    private Vector3 yRotation;

    private bool waitFixed = false;

    void Awake()
    {
        gameManager = FindObjectOfType<HFPS_GameManager>();
        controller = transform.root.gameObject.GetComponentInChildren<PlayerController>();
        cam = transform.root.gameObject.GetComponentInChildren<ScriptManager>().MainCameraBlur.transform;
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        anim.SetBool("Idle", true);
        localEuler = transform.localEulerAngles;
        originalOffset = transform.localPosition;

        if (!enableShadows)
        {
            foreach(SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        if (!visibleToCamera)
        {
            foreach (SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.gameObject.layer = InvisibleMask;
            }
        }
    }

    void Update()
    {
        mouseRotation = Input.GetAxis("Mouse X");

        m_fwd = Input.GetKey(controller.ForwardKey);
        m_bwd = Input.GetKey(controller.BackwardKey);
        m_lt = Input.GetKey(controller.LeftKey);
        m_rt = Input.GetKey(controller.RightKey);

        inputX = controller.inputX;
        inputY = controller.inputY;

        if (controller.controllable)
        {
            if (!waitFixed)
            {
                StartCoroutine(WaitAfterControllable());
            }
            else
            {
                /* POSITIONING */
                if (controller.run && controller.state != 1 && controller.velMagnitude > 0.2f && Input.GetKey(controller.ForwardKey))
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, runningOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (!controller.IsGrounded())
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, jumpOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (controller.state == 1)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, crouchOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (controller.state == 2)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, proneOffset, Time.deltaTime * AdjustSmooth);
                }
                else if (m_lt && !m_fwd && !m_bwd || m_rt && !m_fwd && !m_bwd)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, turnOffset, Time.deltaTime * AdjustSmooth);
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
                }

                if (controller.velMagnitude > 0.2f)
                {
                    /* ROTATIONS */
                    //Right, Forward Right, Backward Right
                    if (m_rt)
                    {
                        if (m_rt && !m_fwd && !m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, RightAngle, Time.deltaTime * TurnSmooth);
                        }
                        else if (m_rt && m_fwd && !m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, UpRightAngle, Time.deltaTime * TurnSmooth);
                        }
                        else if (m_rt && !m_fwd && m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, BackRightAngle, Time.deltaTime * TurnSmooth);
                        }
                    }

                    //Left, Forward Left, Backward Left
                    if (m_lt)
                    {
                        if (m_lt && !m_fwd && !m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, LeftAngle, Time.deltaTime * TurnSmooth);
                        }
                        else if (m_lt && m_fwd && !m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, UpLeftAngle, Time.deltaTime * TurnSmooth);
                        }
                        else if (m_lt && !m_fwd && m_bwd)
                        {
                            localEuler.y = Mathf.Lerp(localEuler.y, BackLeftAngle, Time.deltaTime * TurnSmooth);
                        }
                    }
                }

                //Return to Default Rotation
                if (!m_rt && !m_lt)
                {
                    localEuler.y = Mathf.Lerp(localEuler.y, 0, Time.deltaTime * TurnSmooth);
                }

                if (!controller.IsGrounded())
                {
                    anim.SetBool("isJumping", true);
                    anim.SetBool("Idle", false);
                    tempArmsWeight = Mathf.Lerp(tempArmsWeight, 1, Time.deltaTime * OverrideSmooth);
                }
                else
                {
                    if (!controller.MoveKeyPressed())
                    {
                        anim.SetBool("Idle", true);
                    }

                    anim.SetBool("isJumping", false);
                    tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);

                    if (controller.controllable && gameManager.IsEnabled<MouseLook>())
                    {
                        if (mouseRotation > 0.1f)
                        {
                            anim.SetBool("TurningRight", true);
                            anim.SetBool("TurningLeft", false);
                        }
                        else if (mouseRotation < -0.1f)
                        {
                            anim.SetBool("TurningRight", false);
                            anim.SetBool("TurningLeft", true);
                        }
                        else if (mouseRotation == 0)
                        {
                            anim.SetBool("TurningRight", false);
                            anim.SetBool("TurningLeft", false);
                        }
                    }
                    else
                    {
                        anim.SetBool("TurningRight", false);
                        anim.SetBool("TurningLeft", false);
                    }

                    if (controller.velMagnitude > 0.2f)
                    {
                        anim.SetBool("Idle", false);
                        anim.SetBool("Run", controller.run);
                    }
                    else
                    {
                        anim.SetBool("Run", false);
                        inputY = 0;
                    }
                }

                anim.SetBool("Crouch", controller.state == 1 || controller.state == 2);

                if (controller.velMagnitude > 0.2f)
                {
                    if (m_fwd || m_bwd)
                    {
                        anim.SetBool("MoveForward", m_fwd);
                        anim.SetBool("MoveBackward", m_bwd);
                    }
                    else if (m_lt || m_rt)
                    {
                        anim.SetBool("MoveForward", true);
                        anim.SetBool("MoveBackward", false);
                    }
                    else
                    {
                        anim.SetBool("MoveForward", false);
                        anim.SetBool("MoveBackward", false);
                    }
                }
                else
                {
                    anim.SetBool("MoveForward", false);
                    anim.SetBool("MoveBackward", false);
                }
            }
        }
        else
        {
            waitFixed = false;

            anim.SetBool("MoveForward", false);
            anim.SetBool("MoveBackward", false);
            anim.SetBool("TurningRight", false);
            anim.SetBool("TurningLeft", false);

            if (controller.IsGrounded())
            {
                tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);
                anim.SetBool("isJumping", false);
                anim.SetBool("Idle", true);

                if (controller.state == 0)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
                }
            }
            else
            {
                anim.SetBool("isJumping", true);
            }

            localEuler.y = Mathf.Lerp(localEuler.y, 0, Time.deltaTime * TurnSmooth);
        }

        if (proneDisableBody)
        {
            if (transform.localPosition.y <= (proneOffset.y + 0.1) && transform.localPosition.z <= (proneOffset.z + 0.1))
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = false;
                }
            }
            else
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = true;
                }
            }
        }
        else
        {
            foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = true;
            }

        }

        anim.SetLayerWeight(anim.GetLayerIndex("Arms Layer"), tempArmsWeight);

        transform.localEulerAngles = localEuler + bodyAngle;
        Vector3 relative = transform.InverseTransformPoint(cam.position);
        spineAngle = Mathf.RoundToInt(Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg);
        spineAngle = Mathf.Clamp(spineAngle, spineMaxRotation.x, spineMaxRotation.y);
        yRotation = new Vector3(MiddleSpine.localEulerAngles.x, spineAngle, MiddleSpine.localEulerAngles.z);
    }

    IEnumerator WaitAfterControllable()
    {
        yield return new WaitForFixedUpdate();
        waitFixed = true;
    }

    void LateUpdate()
    {
        MiddleSpine.localRotation = Quaternion.Euler(yRotation);
        anim.transform.localPosition = Vector3.zero;
    }
}
