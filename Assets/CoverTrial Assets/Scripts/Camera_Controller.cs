using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //lets the player control camera with keyboard
    public class Camera_Controller : MonoBehaviour
    {
        //spped
        public float moveSpeed;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //every tick, get the players keyboard input
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            //multiply by speed and balance for framerate
            input *= moveSpeed * Time.deltaTime;
            //move camera on the world z and x axies
            transform.position += Vector3.forward * input.z + Vector3.right * input.x;
        }
    }
}

