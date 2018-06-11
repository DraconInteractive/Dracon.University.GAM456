using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    public class UIFlyText : MonoBehaviour
    {
        RectTransform t;
        private void Start()
        {
            t = GetComponent <RectTransform> ();
            Destroy(this.gameObject, 3);
        }

        void Update()
        {
            t.anchoredPosition += Vector2.up * 50 * Time.deltaTime;
        }
    }
}

