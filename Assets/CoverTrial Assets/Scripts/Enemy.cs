using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //Child class of the base character system. Purely AI, so no player intervention. 
    public class Enemy : Character
    {
        //Enemy type. Pursuit chases the character, sniper stays in one spot and shoots. 
        public enum EnemyType
        {
            Pursuit,
            Sniper
        };

        public EnemyType type;

        Game_Controller controller;
        //How far can the enemy attacK?
        public float attackDist;
        //has the enemy seen the player? Changes movement and attack settings.
        bool hasSeenPlayer;
        //Called during enemy turn. 
        public override void StartTurn()
        {
            //Cant remember, but i think it was for debug...
            blocked = "";
            //set turn to not finished so that the controller waits for it. 
            turnFinished = false;
            //If we dont have the controller, get it. 
            if (controller == null)
            {
                controller = Game_Controller.controller;
            }
            StartCoroutine(DoTurnEnemy());
        }
        //Counts how many times the enemy has been given a null path. Used for stuck detection. 
        int nullCounter = 0;
        //Enemy turn logic.
        IEnumerator DoTurnEnemy()
        {
            //print("enemy turn");
            if (type == EnemyType.Pursuit)
            {
                //Find the closest target on the opposite faction. Doesnt really do much now, but its support for when i put in multiple players. 
                Character target = GetClosestPlayer();
                //If it can see the player, or it has seen the player on a previous turn...
                if (CanSeeTarget(target) || hasSeenPlayer)
                {
                    //Get distance to player.If dist is less than attack dist, attack. 
                    hasSeenPlayer = true;
                    int dist = GetDistance(controller.GetNodeFromWorldPos(transform.position), controller.GetNodeFromWorldPos(target.transform.position));
                    if (dist < attackDist)
                    {
                        target.Damage(attackDamage, this);
                    }
                    else
                    {
                        //If distance is greate, move to target. Refer to base character notes for this. 
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
                        else
                        {
                            nullCounter++;
                            if (nullCounter > 3)
                            {
                                nullCounter = 0;
                                transform.position = Game_Controller.controller.GetNodeFromWorldPos(transform.position).position;
                            }
                        }

                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
            //If the enemy is a sniper, do a simple "can i shoot player. Yes? Cool, im shooting player"
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
                
                yield return new WaitForSeconds(0.1f);
            }
            turnFinished = true;
            yield break;
        }
        //Cycle through players, and compare distances and return the smallest. 
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
        //Aforementioned griddy distance getter
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

