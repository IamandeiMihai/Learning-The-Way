using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainAI : MonoBehaviour
{
    Vector3 initialPos;
    Transform initialTarget;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = transform.position;
        initialTarget = GetComponent<AICharacterControlVillain>().target;
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<AICharacterControlVillain>().moveDone == true)
        {
            GameObject[] states = GetComponent<AICharacterControlVillain>().target.GetComponent<States>().NotNullNextStates();
            GetComponent<AICharacterControlVillain>().target = states[Random.Range(0, states.Length)].transform;
            GetComponent<AICharacterControlVillain>().moveDone = false;
        }   
    }

    public void resetVillain()
    {
        transform.position = initialPos;
        GetComponent<AICharacterControlVillain>().target = initialTarget;
    }
}
