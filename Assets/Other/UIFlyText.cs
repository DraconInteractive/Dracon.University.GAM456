using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    //Pretty fly for a UI
    //But seriously, its a floating text enabler. 
    public class UIFlyText : MonoBehaviour
    {
        //reference to the ui's text
        RectTransform t;

        private void Start()
        {
            //get the rect, then destroy this after 3 seconds
            t = GetComponent <RectTransform> ();
            Destroy(this.gameObject, 3);
        }

        void Update()
        {
            //"float" / move it upward 50 units a second. 
            t.anchoredPosition += Vector2.up * 50 * Time.deltaTime;
        }
    }
}

