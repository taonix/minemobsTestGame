﻿using System;
using Mirror;
using UnityEngine;
using NetworkRigidbody = Mirror.Experimental.NetworkRigidbody;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NetworkRigidbody))]
public class PlayerNetwork : NetworkBehaviour
{
    
    public CharacterController characterController;
    public CapsuleCollider capsuleCollider;
    public NetworkRigidbody networkRigidbody;

    private Camera cam;
    
    void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();
        if (networkRigidbody == null)
            networkRigidbody = GetComponent<NetworkRigidbody>();
    }

    void Start()
    {
        capsuleCollider.enabled = isServer;
    }
    
    public override void OnStartLocalPlayer()
    {
        characterController.enabled = true;

        Camera.main.orthographic = false;
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0f, 0f, 0f);
        Camera.main.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
        cam = Camera.main;
        headRotation = 0;
    }
    
    void OnDisable()
    {
        if (isLocalPlayer && Camera.main != null)
        {
            Camera.main.orthographic = true;
            Camera.main.transform.SetParent(null);
            Camera.main.transform.localPosition = new Vector3(0f, 0f, 0f);
            Camera.main.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        }
    }
    
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float turnSensitivity = 5f;
    public float maxTurnSpeed = 150f;

    [Header("Diagnostics")]
    public float horizontal;
    public float vertical;
    public float turn;
    public float jumpSpeed;
    public bool isGrounded = true;
    public bool isFalling;
    public Vector3 velocity;
    public float headRotation;

    private void Update()
    {
        if (!isLocalPlayer || !characterController.enabled)
            return;

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Q and E cancel each other out, reducing the turn to zero
        /*if (Input.GetKey(KeyCode.A))
            turn = Mathf.MoveTowards(turn, -maxTurnSpeed, turnSensitivity);
        if (Input.GetKey(KeyCode.E))
            turn = Mathf.MoveTowards(turn, maxTurnSpeed, turnSensitivity);
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.E))
            turn = Mathf.MoveTowards(turn, 0, turnSensitivity);
        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.E))
            turn = Mathf.MoveTowards(turn, 0, turnSensitivity);*/
        
        float x = Input.GetAxis("Mouse X") * turnSensitivity * Time.deltaTime;
        float y = Input.GetAxis("Mouse Y") * turnSensitivity * Time.deltaTime * -1f;

        transform.Rotate(0f, x, 0f);

        headRotation += y;
        
        cam.transform.localEulerAngles = new Vector3(headRotation, 0f,0f);

        if (isGrounded)
            isFalling = false;

        if ((isGrounded || !isFalling) && jumpSpeed < 1f && Input.GetKey(KeyCode.Space))
        {
            jumpSpeed = Mathf.Lerp(jumpSpeed, 1f, 1f);
        }
        else if (!isGrounded)
        {
            isFalling = true;
            jumpSpeed = 0;
        }
    }
    
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer || characterController == null)
            return;

        transform.Rotate(0f, turn * Time.fixedDeltaTime, 0f);

        Vector3 direction = new Vector3(horizontal, jumpSpeed, vertical);
        direction = Vector3.ClampMagnitude(direction, 1f);
        direction = transform.TransformDirection(direction);
        direction *= moveSpeed;

        if (jumpSpeed > 0)
            characterController.Move(direction * Time.fixedDeltaTime);
        else
            characterController.SimpleMove(direction);

        isGrounded = characterController.isGrounded;
        velocity = characterController.velocity;
    }
}
