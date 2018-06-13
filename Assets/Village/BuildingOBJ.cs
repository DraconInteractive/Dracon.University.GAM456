using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Village
{
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

