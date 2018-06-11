using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    public class UIController : MonoBehaviour
    {
        public static UIController controller;

        public GameObject floatTextContainer;
        public Text floatingTextPrefab;
        List<string> floatingTextQueue = new List<string>();
        private void Awake()
        {
            controller = this;
        }

        // Use this for initialization
        void Start()
        {
            StartCoroutine(FloatingTextAction());
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddTextToContainerQueue (string text)
        {
            floatingTextQueue.Add(text);
        }

        IEnumerator FloatingTextAction ()
        {
            //Writing a note for me, because the logic here broke my brain (for no apparent reason). 
            //Have timer
            //Timer increases if it hasnt reached limit
            //If timer has reached limit AND there is text to spawn, do it. 

            //Writing that (^) down actually helped me optimise it a bit! woo! ( Still looking to optimise further, but meh for now)
            float timer = 0;
            while (true)
            {
                if (timer < 1)
                {
                    timer += Time.deltaTime;
                }
                else if (timer >= 1 && floatingTextQueue.Count > 0)
                {
                    CreateFloatText();
                    timer = 0;
                }
                yield return null;
            }
            
            yield break;
        }

        public void CreateFloatText ()
        {
            Text floatingText = Instantiate(floatingTextPrefab, floatTextContainer.transform).GetComponent<Text>();
            floatingText.text = floatingTextQueue[0];
            floatingTextQueue.RemoveAt(0);
        }
    }
}

