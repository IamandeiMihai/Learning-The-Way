using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICharacterControl : MonoBehaviour
{
    [SerializeField] private GameObject[] villains;
    [SerializeField] public Animator m_animator;
    public bool attacked = false;
    public int currentState = 0;
    private float m_currentV = 0;
    private float m_jumpTimeStamp = 0;
    private float m_minJumpInterval = 3f;
    private readonly float m_interpolation = 10;
    public Transform target;
    public bool moveDone;
    public bool rotationDone;
    public float moveSpeed;
    public float turnSpeed;
    public bool cry = false;

    private void Awake()
    {
        if (!m_animator) { gameObject.GetComponent<Animator>(); }

        villains = GameObject.FindGameObjectsWithTag("villain");
    }

    // Update is called once per frame
    void Update()
    {
        bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

        if (this.GetComponent<QLearning>().demo_done == true && jumpCooldownOver)
        {
            m_jumpTimeStamp = Time.time;
            m_animator.SetTrigger("Wave");
        }
        MoveCharacter(target);
    }

    private void MoveCharacter(Transform target)
    {
        if (!moveDone)
        {
            if (!rotationDone)
            {
                Vector3 direction = target.position - this.transform.position;
                Quaternion toRotation = Quaternion.LookRotation(direction);
                this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, turnSpeed * Time.deltaTime);

                if (Quaternion.Angle(toRotation, this.transform.rotation) < 0.5f)
                {
                    rotationDone = true;
                }
            }
            else
            {
                this.transform.position = Vector3.MoveTowards(this.transform.position, target.position, moveSpeed * Time.deltaTime);
            }
        }

        if (Vector3.Distance(target.position, this.transform.position) < 0.1f)
        {
            moveDone = true;
            rotationDone = false;
            m_animator.SetFloat("MoveSpeed", 0);
        } else
        {
            float v = 0.5f;
            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_animator.SetFloat("MoveSpeed", m_currentV);

            moveDone = false;
        }
    }

    public bool IsAttacked()
    {
        // verificam coliziune cu inamicul -- pentru partea vizuala/demo
        if (attacked == true)
        {
            return true;
        }
        // verificam daca starea copilului e aceeasi cu starea inamicului -- pentru learning
        if (!GetComponent<QLearning>().visualLearning && !GetComponent<QLearning>().demo)
        {
            foreach (GameObject villain in villains)
            {
                if (villain.GetComponent<VillainAI>().currentState == currentState)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "villain")
        {
            attacked = true;
        }
    }
}
