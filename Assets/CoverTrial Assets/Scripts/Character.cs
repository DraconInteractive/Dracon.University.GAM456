﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cover
{
    public class Character : MonoBehaviour
    {
        public int movePoints, crouchMovePoints;
        public float movementSpeed, rotateSpeed;
        Coroutine moveRoutine;
        public Cover cover;
        public Tile currentTile;
        public GameObject floatingText;

        public LayerMask sightMask;

        public string blocked;
        public enum Faction
        {
            Player, 
            Enemy
        };

        public Faction faction;

        public bool turnFinished;

        public float headOffset;
        public float sightDist;
        public int attackStepLimit, attackDamage;
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

        public float currentHealth, maximumHealth = 100;

        public enum MoveType
        {
            Basic,
            BasicRot,
            PhysicsTeleport
        };

        public MoveType moveType;

        Rigidbody rb;
        private void Start()
        {
            CharacterStart();
        }

        public virtual void CharacterStart ()
        {
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
                /*
                Vector3 start = transform.position;
                Vector3 end = node.position;
                for (float f = 0; f < 1 * Vector3.Distance(start, end); f += Time.deltaTime * movementSpeed)
                {
                    transform.position = Vector3.Lerp(start, end, f);
                    yield return null;
                }
                transform.position = end;
                */
                if (moveType == MoveType.Basic)
                {
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
                
                Game_Controller.controller.SetNodeOccupant(node, this);
                if (faction == Faction.Player)
                {
                    Game_Controller.controller.MDTile(Game_Controller.controller.GetTileFromNode(node));
                }
                
                cover = node.nodeCover;
                currentTile = Game_Controller.controller.GetTileFromNode(node);
                if (faction == Faction.Player)
                {
                    currentTile.TileAction();
                }
                
            }
            yield break;
        }

        public virtual void StartTurn ()
        {
            StartCoroutine(DoTurn());
        }

        IEnumerator DoTurn ()
        {
            turnFinished = true;
            yield break;
        }

        public void Damage (int damage, Character origin)
        {
            UIController.controller.AddTextToContainerQueue("Character '" + gameObject.name + "' damaged for " + damage + " points by " + origin.name);
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
            
            Text fText = Instantiate(floatingText, transform.position + Vector3.up, Quaternion.identity, this.transform).GetComponentInChildren<Text>();
            fText.text = damage.ToString();

            print("Damaged: " + damage);

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Die ()
        {
            UIController.controller.AddTextToContainerQueue("Character '" + gameObject.name + "' has died");
            GetComponentInChildren<Renderer>().material.color = new Color(1, 0, 0, 0.25f);

            Game_Controller c = Game_Controller.controller;
            c.SetNodeOccupant(c.GetNodeFromWorldPos(transform.position), null);
            c.allAllies.Remove(this);

            Destroy(this);
        }

        float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n)
        {
            float angle = Vector3.Angle(a, b);
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));
            return angle * sign;
        }

        public bool CanSeeTarget (Character target)
        {
            bool canSeeTarget = false;
            float distToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distToTarget > sightDist)
            {
                blocked = "Dist greater than sight";
                return false;
            }
            Vector3 playerMod = target.transform.position + target.transform.up * target.headOffset;
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

            return canSeeTarget;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.up * headOffset, 0.15f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + transform.up * headOffset, sightDist);
        }
    }
}

