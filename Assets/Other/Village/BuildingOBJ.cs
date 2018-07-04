using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Note, not part of main assignment. 
namespace Village
{
    //Used to identify buildings. 
    public class BuildingOBJ : MonoBehaviour
    {
        public Building building;

        private void Awake()
        {
            building.position = transform.position;
            building.GO = this.gameObject;
        }
    }
}

