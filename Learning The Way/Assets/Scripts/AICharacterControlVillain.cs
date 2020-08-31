using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICharacterControlVillain : MonoBehaviour
{
    [SerializeField] public Animator m_animator;
    private float m_currentV = 0;
    private readonly float m_interpolation = 10;
    public Transform target;
    public bool moveDone;
    public bool rotationDone;
    public float moveSpeed;
    public float turnSpeed;

    private void Awake()
    {
        if (!m_animator) { gameObject.GetComponent<Animator>(); }
    }

    // Update is called once per frame
    void Update()
    {
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
        }
        else
        {
            float v = 0.5f;
            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_animator.SetFloat("MoveSpeed", m_currentV);

            moveDone = false;
        }
    }
}
