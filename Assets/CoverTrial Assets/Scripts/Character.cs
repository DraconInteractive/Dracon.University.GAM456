using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    //base character statistics and functions
    public class Character : MonoBehaviour
    {
        //how many squares movable per turn, standing or crouched
        public int movePoints, crouchMovePoints;
        //Speed of movement and rotation. Rotation speed needs to be tightly conttrolled as low rot will cause overshoot
        public float movementSpeed, rotateSpeed;
        //Storage for the movement routine
        Coroutine moveRoutine;
        //The current nodes cover
        public Cover cover;
        //What tile the character is standing on
        public Tile currentTile;
        //Damage text
        public GameObject floatingText;
        //Used for visibility checks
        public LayerMask sightMask;
        //Honestly wish i could remember, important though
        public string blocked;
        //This script is shared, this enum tracks whether is player character or enemy
        public enum Faction
        {
            Player, 
            Enemy
        };

        public Faction faction;
        //Used to track turn action completion
        public bool turnFinished;
        //How much higher than the object centre is the head
        public float headOffset;
        //How far can the character see?
        public float sightDist;
        //How far away the player can attack, and how much damage they do
        public int attackStepLimit, attackDamage;
        //is the character crouching
        public bool crouching;

        public bool Crouching
        {
            get
            {
                return crouching;
            }

            set
            {
                crouching = value;
                if (value)
                {
                    transform.GetChild(0).localScale = Vector3.one * 0.25f;
                    Vector3 lp = transform.GetChild(0).localPosition;
                    lp.y = -0.75f;
                    transform.GetChild(0).localPosition = lp;

                }
                else
                {
                    transform.GetChild(0).localScale = Vector3.one * 1;
                    Vector3 lp = transform.GetChild(0).localPosition;
                    lp.y = 0;
                    transform.GetChild(0).localPosition = lp;
                }
            }
        }
        //self expanatory
        public float currentHealth, maximumHealth = 100;
        //movement types. Physics Teleport recommended with a basic rot fallback. If wished, the Basic setting could be used for an onrails enemy
        public enum MoveType
        {
            Basic,
            BasicRot,
            PhysicsTeleport
        };

        public MoveType moveType;
        //storage for player physics
        Rigidbody rb;

        private void Start()
        {
            //Do setup. separate function to allow overloading for children
            CharacterStart();
        }
        //setup functions
        public virtual void CharacterStart ()
        {
            //Set health, and if the player needs physics and doesnt have it, create it. 
            currentHealth = maximumHealth;
            if (moveType == MoveType.PhysicsTeleport)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
                
            }
        }
        //if the player clicks on the character, send a message to the main controller to select the tile
        private void OnMouseDown()
        {
            Game_Controller.controller.MDCharacter(this);
        }
        //public function to launch movement, ensuring that all characters host their own movement routines
        //Use a coroutine variable to cancel current movment routine if need be. 
        public void MoveTo (List<Node> path)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveList(path));
        }
        //Movement routine
        IEnumerator MoveList (List<Node> path)
        {
            //iterating through nodes in the path, apply control specific instructions to move player from one node to the next
            foreach (Node node in path)
            {
                if (moveType == MoveType.Basic)
                {
                    //basic movement
                    //Store the end positon. Get the estimated time of arrival as the distance * 3 (subject to change)
                    //set a timer, then move the player as long as the distance to the next node is greater than 10cm. 
                    //To move player, set position using Vector3.MoveTowards. Use movement variable for speed, and balance for framerate. 
                    //if the journey takes to long, teleport the player to the target node, and hope he does better thext time. 
                    Vector3 end = node.position;
                    float eta = (end - transform.position).magnitude * 3;
                    float timer = 0;
                    while ((end - transform.position).magnitude > 0.1f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, end, movementSpeed * Time.deltaTime);
                        timer += Time.deltaTime;
                        if (timer > eta)
                        {
                            transform.position = end;
                        }
                        yield return null;
                    }
                }

                //This mode does much the same as the player, but instead of moving toward the goal, the player moves forward, and rotates toward the goal. This can cause circling, so I adjusted the timer so that instead of teleorting straight away, it will allow greater freedom of movement in hope of solving this. If this fails, then it teleports it. 

                else if (moveType == MoveType.BasicRot)
                {
                    Vector3 end = node.position;
                    float eta = (end - transform.position).magnitude * 3;
                    float timer = 0;

                    while ((end - transform.position).magnitude > 0.1f)
                    {
                        //transform.position = Vector3.MoveTowards(transform.position, end, movementSpeed * Time.deltaTime);
                        transform.position += transform.forward * movementSpeed * Time.deltaTime;

                        Vector3 targetDir = (end - transform.position).normalized;
                        float step = 0;



                        timer += Time.deltaTime;
                        if (timer > eta)
                        {
                            step = rotateSpeed * 2 * Time.deltaTime;
                            if (timer > eta * 2)
                            {
                                transform.position = end;
                            }
                        } 
                        else
                        {
                            step = rotateSpeed * Time.deltaTime;
                        }

                        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
                        transform.rotation = Quaternion.LookRotation(newDir);
                        yield return null;
                    }
                }
                //this movement type is similar tosicRot, but requires a Rigidbody component. It then uses the rigidbody MovePosition and MoveRotation to perform collision checks,etc, as it moves. 
                else if (moveType == MoveType.PhysicsTeleport)
                {
                    Vector3 end = node.position;
                    float eta = (end - transform.position).magnitude * 3;
                    float timer = 0;

                    while ((end - transform.position).magnitude > 0.1f)
                    {
                        //transform.position = Vector3.MoveTowards(transform.position, end, movementSpeed * Time.deltaTime);
                        rb.MovePosition(transform.position + transform.forward * movementSpeed * Time.fixedDeltaTime);
                        Vector3 targetDir = (end - transform.position).normalized;
                        float step = rotateSpeed * Time.deltaTime;
                        

                        timer += Time.deltaTime;
                        if (timer > eta)
                        {
                            step = rotateSpeed * 2 * Time.deltaTime;
                            if (timer > eta * 2)
                            {
                                transform.position = end;
                            }
                        }
                        else
                        {
                            step = rotateSpeed * Time.deltaTime;
                        }
                        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
                        rb.MoveRotation(Quaternion.LookRotation(newDir));
                        yield return new WaitForFixedUpdate();
                    }
                }
                //as the character reaches the node, remove the character reference from the previous node and pass it to the new area. 
                Game_Controller.controller.SetNodeOccupant(node, this);
                //If the character is the player, select the node under it. 
                if (faction == Faction.Player)
                {
                    Game_Controller.controller.MDTile(Game_Controller.controller.GetTileFromNode(node));
                }
                //set the characters cover attribute to that of its new noe. 
                cover = node.nodeCover;
                currentTile = Game_Controller.controller.GetTileFromNode(node);
                //if the character is a player, perform the action associated with it
                if (faction == Faction.Player)
                {
                    currentTile.TileAction();
                }
                
            }
            yield break;
        }

        //an overridable function for when the controller iterates through the turn cycle
        public virtual void StartTurn ()
        {
            StartCoroutine(DoTurn());
        }
        //perform action then mark character as finished
        IEnumerator DoTurn ()
        {
            turnFinished = true;
            yield break;
        }
        //apply damage to the character
        public void Damage (int damage, Character origin)
        {
            //tell the ui controller to output the damage
            UIController.controller.AddTextToContainerQueue("Character '" + gameObject.name + "' damaged for " + damage + " points by " + origin.name);
            //if the player is begind cover, get the vector of the incoming damage and compare it to the vector of the cover. IF they match, halve the damage. 
            if (cover.enabled)
            {
                Vector3 damageVector = origin.transform.position - transform.position;
                Cover.Direction damageDirection = Cover.Direction.Up;
                float closestAngle = Mathf.Infinity;
                List<Cover.Direction> allDirections = new List<Cover.Direction>()
                {
                    Cover.Direction.Up,
                    Cover.Direction.Down,
                    Cover.Direction.Left,
                    Cover.Direction.Right,
                    Cover.Direction.Forward,
                    Cover.Direction.Backward
                };
                foreach (Cover.Direction dir in allDirections)
                {
                    switch (dir)
                    {
                        case Cover.Direction.Up:
                            float upAng = Vector3.Angle(damageVector, Vector3.up);
                            if (upAng < closestAngle)
                            {
                                closestAngle = upAng;
                                damageDirection = Cover.Direction.Up;
                            }
                            break;
                        case Cover.Direction.Down:
                            float downAng = Vector3.Angle(damageVector, Vector3.down);
                            if (downAng < closestAngle)
                            {
                                closestAngle = downAng;
                                damageDirection = Cover.Direction.Down;
                            }
                            break;
                        case Cover.Direction.Left:
                            float leftAng = Vector3.Angle(damageVector, Vector3.left);
                            if (leftAng < closestAngle)
                            {
                                closestAngle = leftAng;
                                damageDirection = Cover.Direction.Left;
                            }
                            break;
                        case Cover.Direction.Right:
                            float rightAng = Vector3.Angle(damageVector, Vector3.right);
                            if (rightAng < closestAngle)
                            {
                                closestAngle = rightAng;
                                damageDirection = Cover.Direction.Right;
                            }
                            break;
                        case Cover.Direction.Forward:
                            float forAng = Vector3.Angle(damageVector, Vector3.forward);
                            if (forAng < closestAngle)
                            {
                                closestAngle = forAng;
                                damageDirection = Cover.Direction.Forward;
                            }
                            break;
                        case Cover.Direction.Backward:
                            float backAng = Vector3.Angle(damageVector, Vector3.back);
                            if (backAng < closestAngle)
                            {
                                closestAngle = backAng;
                                damageDirection = Cover.Direction.Backward;
                            }
                            break;
                    }
                }

                print("Damaged from: " + damageDirection.ToString());
                if (cover.coverDirections.Contains(damageDirection))
                {
                    damage = Mathf.RoundToInt(damage*0.5f);
                }
            }
            //create a floating text item to tell the player about the damage
            Text fText = Instantiate(floatingText, transform.position + Vector3.up, Quaternion.identity, this.transform).GetComponentInChildren<Text>();
            fText.text = damage.ToString();

            print("Damaged: " + damage);
            //finally, subtract the damage from the characters health.
            //if the health is less than 0, kill the player
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        //used to kill the character
        public void Die ()
        {
            //report to the game ui, then turn the player red. 
            UIController.controller.AddTextToContainerQueue("Character '" + gameObject.name + "' has died");
            GetComponentInChildren<Renderer>().material.color = new Color(1, 0, 0, 0.25f);
            //tell the node there is no more character, and remove ally from tracking list. Finally, destroy this script
            Game_Controller c = Game_Controller.controller;
            c.SetNodeOccupant(c.GetNodeFromWorldPos(transform.position), null);
            c.allAllies.Remove(this);

            Destroy(this);
        }
        //i forget what a signed angle is, but this helps with rotation
        float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n)
        {
            float angle = Vector3.Angle(a, b);
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));
            return angle * sign;
        }
        //returns whether a character can see another character
        public bool CanSeeTarget (Character target)
        {
            //check the distance from character to target. if the distance is too large, then the target is out of sight range. 
            bool canSeeTarget = false;
            float distToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distToTarget > sightDist)
            {
                blocked = "Dist greater than sight";
                return false;
            }
            //modify the target position to account for head position (we aim for the head as it is the first thing visible above cover. 
            Vector3 playerMod = target.transform.position + target.transform.up * target.headOffset;
            //Do a raycast from characterhead to target head. If the cast hits the target, then they are visible. If not, then they arent. 
            Ray ray = new Ray(transform.position + transform.up * headOffset, playerMod - (transform.position + transform.up * headOffset));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, sightDist, sightMask))
            {
                if (hit.transform.tag == "Player")
                {
                    canSeeTarget = true;
                }
                else
                {
                    canSeeTarget = false;
                }
            }
            else
            {
                canSeeTarget = false;
            }
            //return our consensus. 
            return canSeeTarget;
        }

        private void OnDrawGizmos()
        {
            //Draw spheres for the characters head, and for the characters sight. 
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.up * headOffset, 0.15f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + transform.up * headOffset, sightDist);
        }
    }
}

