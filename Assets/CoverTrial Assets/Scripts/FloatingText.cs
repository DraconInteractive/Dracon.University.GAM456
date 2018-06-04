using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    public class FloatingText : MonoBehaviour
    {
        public float movementSpeed;
        public Text myText;
        // Use this for initialization
        void Start()
        {
            Destroy(this.gameObject, 10);
        }

        // Update is called once per frame
        void Update()
        {
            transform.position += Vector3.up * movementSpeed * Time.deltaTime;
            //transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}

