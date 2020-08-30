using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainAI : MonoBehaviour
{
    [SerializeField] private GameObject initialPos;
    Transform initialTarget;
    public int currentState;
    public int oldState;
    public GameObject child;
    MapStatus mapStatus;

    // Start is called before the first frame update
    void Start()
    {
        initialTarget = GetComponent<AICharacterControlVillain>().target;
        currentState = child.GetComponent<QLearning>().GetIndexState(initialPos);
        oldState = 0;
        mapStatus = GameObject.Find("Points").GetComponent<MapStatus>();
    }

    // Update is called once per frame
    void Update()
    {
        if (child.GetComponent<QLearning>().visualVillains())
        {
            if (GetComponent<AICharacterControlVillain>().moveDone == true)
            {
                oldState = currentState;
                
                GameObject[] states = GetComponent<AICharacterControlVillain>().target.GetComponent<States>().NotNullNextStates();

                int dist = mapStatus.GetDistance(currentState, child.GetComponent<AICharacterControl>().currentState);
                if (dist < 4)
                {
                    if (dist == 0)
                    {
                        GetComponent<AICharacterControlVillain>().target = states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<QLearning>().newState)].transform;
                    }
                    else
                    {
                        GetComponent<AICharacterControlVillain>().target = states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<AICharacterControl>().currentState)].transform;
                    }
                }
                else
                {
                    GetComponent<AICharacterControlVillain>().target = states[Random.Range(0, states.Length)].transform;
                }
                currentState = child.GetComponent<QLearning>().GetIndexState(this.GetComponent<AICharacterControlVillain>().target.gameObject);

                GetComponent<AICharacterControlVillain>().moveDone = false;
            }
        }
    }

    public void ChangeState()
    {
        GameObject[] states = child.GetComponent<QLearning>().states[currentState].GetComponent<States>().NotNullNextStates();
        int dist = mapStatus.GetDistance(currentState, child.GetComponent<AICharacterControl>().currentState);
        oldState = currentState;
        if (dist < 4)
        {
            if (dist <= 1)
            {
                currentState = child.GetComponent<QLearning>().states.IndexOf(states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<QLearning>().newState)]);
            }
            else
            {
                currentState = child.GetComponent<QLearning>().states.IndexOf(states[mapStatus.GetNextStateToTarget(currentState, child.GetComponent<AICharacterControl>().currentState)]);
            }
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
