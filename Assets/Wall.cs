using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
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

