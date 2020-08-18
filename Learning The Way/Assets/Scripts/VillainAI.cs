using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainAI : MonoBehaviour
{
    [SerializeField] private GameObject initialPos;
    Transform initialTarget;
    public int currentState;
    public GameObject child;
    MapStatus mapStatus;

    // Start is called before the first frame update
    void Start()
    {
        initialTarget = GetComponent<AICharacterControlVillain>().target;
        currentState = child.GetComponent<QLearning>().GetIndexState(initialPos);
        mapStatus = GameObject.Find("Points").GetComponent<MapStatus>();
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

                if (mapStatus.GetDistance(currentState, child.GetComponent<AICharacterControl>().currentState) < 4)
                {
                    GetComponent<AICharacterControlVillain>().target = states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<AICharacterControl>().currentState)].transform;
                }
                else
                {
                    GetComponent<AICharacterControlVillain>().target = states[Random.Range(0, states.Length)].transform;
                }


                GetComponent<AICharacterControlVillain>().moveDone = false;
            }
        }
    }

    public void ChangeState()
    {
        GameObject[] states = child.GetComponent<QLearning>().states[currentState].GetComponent<States>().NotNullNextStates();
        if (mapStatus.GetDistance(currentState, child.GetComponent<AICharacterControl>().currentState) < 4)
        {
            currentState = child.GetComponent<QLearning>().states.IndexOf(states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<AICharacterControl>().currentState)]);
        }
        else
        {
            currentState = child.GetComponent<QLearning>().states.IndexOf(states[Random.Range(0, states.Length)]);
        }
    }

    public void resetVillain()
    {
        transform.position = initialPos.transform.position;
        GetComponent<AICharacterControlVillain>().target = initialTarget;
        currentState = child.GetComponent<QLearning>().GetIndexState(initialPos);
    }
}
