using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //Attach to walls for see throughability when moused over. 
    public class Wall : MonoBehaviour
    {
        Renderer r;
        // Use this for initialization
        void Start()
        {
            r = GetComponent<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {

        }
        //Mouse go in: set alpha to 0.1. Mouse go out, set alpha to 1. 
        private void OnMouseEnter()
        {
            Color c = r.material.color;
            c.a = 0.1f;
            r.material.color = c;
        }

        private void OnMouseExit()
        {
            Color c = r.material.color;
            c.a = 1f;
            r.material.color = c;
        }
    }
}

