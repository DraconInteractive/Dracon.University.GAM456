using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Character : MonoBehaviour
    {
        public int movePoints;
        public float movementSpeed;
        Coroutine moveRoutine;
        public Cover cover;
        
        public enum Faction
        {
            Player, 
            Enemy
        };

        public Faction faction;
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

        public void MoveTo (List<Node> path)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveList(path));
        }

        IEnumerator MoveList (List<Node> path)
        {
            foreach (Node node in path)
            {
                Vector3 start = transform.position;
                Vector3 end = node.position;
                for (float f = 0; f < 1; f += Time.deltaTime * movementSpeed)
                {
                    transform.position = Vector3.Lerp(start, end, f);
                    yield return null;
                }
                transform.position = end;
                Game_Controller.controller.SetNodeOccupant(node, this);
                Game_Controller.controller.MDTile(Game_Controller.controller.GetTileFromNode(node));
                cover = node.nodeCover;
            }
            yield break;
        }
    }
}

