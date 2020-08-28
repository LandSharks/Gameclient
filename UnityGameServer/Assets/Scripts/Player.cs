using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;

    private float moveSpeed = 5f / Constants.TICKS_PER_SECOND;
    private bool[] inputs;

    public void Initialize(int _id, string _username) {
        id = _id;
        username = _username;

        inputs = new bool[4];
    }

    public void FixedUpdate() {
        Vector2 inputDirection = Vector2.zero;
        if(inputs[0]) {
            inputDirection.y += 1;
        }
        if(inputs[1]) {
            inputDirection.y -= 1;
        }
        if(inputs[2]) {
            inputDirection.x -= 1;
        }
        if(inputs[3]) {
            inputDirection.x += 1;
        }

        Move(inputDirection);
    }

    private void Move(Vector2 inputDirection) {

        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        transform.position += moveDirection * moveSpeed;

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    public void SetInput(bool[] _inputs, Quaternion _rotation) {
        inputs = _inputs;
        transform.rotation = _rotation;
    }
}
