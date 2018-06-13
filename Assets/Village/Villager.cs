﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Village
{
    public class Villager : MonoBehaviour
    {
        public float heightOffset = 1;
        Coroutine movementRoutine;

        enum State
        {
            Stopped,
            Moving
        };

        State state = State.Stopped;

        private void Update()
        {
            if (state == State.Stopped)
            {
                VillageController c = VillageController.controller;
                List<Node> path = c.GeneratePath(c.GetNodeFromWorldPos(transform.position), c.GetNodeFromWorldPos(c.GetRandomBuilding().position));
                if (path != null)
                {
                    MoveTo(path);
                }
                
            }   
        }


        public void MoveTo (List<Node> path)
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
            }

            movementRoutine = StartCoroutine(DoMove(path));
        }

        IEnumerator DoMove (List<Node> path)
        {
            state = State.Moving;
            foreach (Node n in path)
            {
                Vector3 start = transform.position;
                Vector3 end = n.position + Vector3.up * heightOffset;
                for (float f = 0; f < 1; f += Time.deltaTime)
                {
                    transform.position = Vector3.Lerp(start, end, f);
                    yield return null;
                }
            }
            state = State.Stopped;
            yield break;
        }

        private void OnDrawGizmos()
        {
            if (state == State.Moving)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, 0.1f);
        }
    }
}

