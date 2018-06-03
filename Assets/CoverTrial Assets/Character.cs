using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Character : MonoBehaviour
    {
        Coroutine moveRoutine;
        
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnMouseDown()
        {
            Game_Controller.controller.MDCharacter(this);
        }

        public void MoveTo (Vector3 position)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }
            
            moveRoutine = StartCoroutine(Move(position));
        }

        public void MoveTo (List<Node> path)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveList(path));
        }

        IEnumerator Move (Vector3 position)
        {
            Vector3 start = transform.position;
            Vector3 end = position;
            for (float f = 0; f < 1; f += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, end, f);
                yield return null;
            }
            transform.position = end;
            yield break;
        }

        IEnumerator MoveList (List<Node> path)
        {
            foreach (Node node in path)
            {
                Vector3 start = transform.position;
                Vector3 end = node.position;
                for (float f = 0; f < 1; f += Time.deltaTime)
                {
                    transform.position = Vector3.Lerp(start, end, f);
                    yield return null;
                }
                transform.position = end;
                Game_Controller.controller.SetNodeOccupant(node, this);
            }
            yield break;
        }
    }
}

