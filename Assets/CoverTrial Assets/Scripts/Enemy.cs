using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Enemy : Character
    {
        public enum EnemyType
        {
            Pursuit,
            Sniper
        };

        public EnemyType type;

        Game_Controller controller;

        public float attackDist;

        bool hasSeenPlayer;
        public override void StartTurn()
        {
            blocked = "";
            turnFinished = false;
            if (controller == null)
            {
                controller = Game_Controller.controller;
            }
            StartCoroutine(DoTurnEnemy());
        }

        IEnumerator DoTurnEnemy()
        {
            //print("enemy turn");
            if (type == EnemyType.Pursuit)
            {
                Character target = GetClosestPlayer();

                if (CanSeeTarget(target) || hasSeenPlayer)
                {
                    hasSeenPlayer = true;
                    int dist = GetDistance(controller.GetNodeFromWorldPos(transform.position), controller.GetNodeFromWorldPos(target.transform.position));
                    if (dist < attackDist)
                    {
                        target.Damage(20, this);
                    }
                    else
                    {
                        currentTile.node.occupant = null;

                        List<Node> path = controller.GeneratePath(controller.GetNodeFromWorldPos(transform.position), controller.GetNodeFromWorldPos(target.transform.position));
                        if (path != null)
                        {
                            if (!crouching)
                            {
                                if (path.Count >= movePoints)
                                {
                                    List<Node> pathRange = path.GetRange(0, movePoints);
                                    MoveTo(pathRange);
                                    yield return new WaitForSeconds((1 / movementSpeed) * pathRange.Count);
                                }
                                else
                                {
                                    List<Node> pathRange = path.GetRange(0, path.Count);
                                    MoveTo(pathRange);
                                    yield return new WaitForSeconds((1 / movementSpeed) * pathRange.Count);
                                }
                            }
                            else
                            {
                                if (path.Count >= crouchMovePoints)
                                {
                                    List<Node> pathRange = path.GetRange(0, crouchMovePoints);
                                    MoveTo(pathRange);
                                    yield return new WaitForSeconds((1 / movementSpeed) * pathRange.Count);
                                }
                                else
                                {
                                    List<Node> pathRange = path.GetRange(0, path.Count);
                                    MoveTo(pathRange);
                                    yield return new WaitForSeconds((1 / movementSpeed) * pathRange.Count);
                                }
                            }
                            
                        }

                    }
                }

                yield return new WaitForSeconds(0.25f);
            }
            else if (type == EnemyType.Sniper)
            {
                Character target = GetClosestPlayer();

                if (CanSeeTarget(target) || hasSeenPlayer)
                {
                    hasSeenPlayer = true;
                    int dist = GetDistance(controller.GetNodeFromWorldPos(transform.position), controller.GetNodeFromWorldPos(target.transform.position));
                    //print("currentDist: " + dist + " attachDist: " + attackDist);
                    if (dist < attackDist)
                    {
                        target.Damage(100, this);
                    }
                }
                
                yield return new WaitForSeconds(0.25f);
            }
            turnFinished = true;
            yield break;
        }
        
        public Character GetClosestPlayer ()
        {
            float bigDist = Mathf.Infinity;
            Character target = null;

            foreach (Character c in controller.allAllies)
            {
                float dist = Vector3.Distance(transform.position, c.transform.position);
                if (dist < bigDist)
                {
                    bigDist = dist;
                    target = c;
                }
            }

            return target;
        }

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.RoundToInt(Mathf.Abs(nodeA.position.x - nodeB.position.x));
            int dstY = Mathf.RoundToInt(Mathf.Abs(nodeA.position.z - nodeB.position.z));

            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            else
            {
                return 14 * dstX + 10 * (dstY - dstX);
            }
        }

       
    }
}

