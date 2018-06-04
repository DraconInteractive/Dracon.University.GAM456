using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Obstruction : MonoBehaviour
    {
        public enum ObType
        {
            Blockage,
            Door
        };

        public ObType type;

        public Door door;

        private void Start()
        {
            if (type == ObType.Door)
            {
                door.Open = door.open;
                door.Locked = door.locked;
            }
        }
    }
}

