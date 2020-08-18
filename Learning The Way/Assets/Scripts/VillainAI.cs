using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainAI : MonoBehaviour
{
    [SerializeField] private GameObject initialPos;
    Transform initialTarget;
    public int currentState;
    public GameObject child;

    // Start is called before the first frame update
    void Start()
    {
        initialTarget = GetComponent<AICharacterControlVillain>().target;
        currentState = child.GetComponent<QLearning>().GetIndexState(initialPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (child.GetComponent<QLearning>().visualVillains())
        {
            if (GetComponent<AICharacterControlVillain>().moveDone == true)
            {
                currentState = child.GetComponent<QLearning>().GetIndexState(this.GetComponent<AICharacterControlVillain>().target.gameObject);
                GameObject[] states = GetComponent<AICharacterControlVillain>().target.GetComponent<States>().NotNullNextStates();
                GetComponent<AICharacterControlVillain>().target = states[Random.Range(0, states.Length)].transform;
                GetComponent<AICharacterControlVillain>().moveDone = false;
            }
        }
    }

    public void ChangeState()
    {
        GameObject[] states = child.GetComponent<QLearning>().states[currentState].GetComponent<States>().NotNullNextStates();
        currentState = child.GetComponent<QLearning>().states.IndexOf(states[Random.Range(0, states.Length)]);
    }

    public void resetVillain()
    {
        transform.position = initialPos.transform.position;
        GetComponent<AICharacterControlVillain>().target = initialTarget;
        currentState = child.GetComponent<QLearning>().GetIndexState(initialPos);
    }
}
