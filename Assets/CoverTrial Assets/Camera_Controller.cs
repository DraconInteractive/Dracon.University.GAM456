using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Camera_Controller : MonoBehaviour
    {
        public float moveSpeed;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            input *= moveSpeed * Time.deltaTime;

            transform.position += Vector3.forward * input.z + Vector3.right * input.x;
        }
    }
}

