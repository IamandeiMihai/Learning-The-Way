using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class GemsLogic : MonoBehaviour
{
    private List<GameObject> states;
    public List<GameObject> legalStates;
    public GameObject gemState;
    public bool pickUpGem = false;
    private QLearning qLearning;
    private AICharacterControl aicc;


    public void Initialize()
    {
        states = GameObject.Find("Child").GetComponent<QLearning>().states;
        foreach (GameObject state in states)
        {
            if (state.transform.parent.name.Equals("Intersections"))
            {
                legalStates.Add(state);
            }
        }
        gemState = legalStates[UnityEngine.Random.Range(0, legalStates.Count)];
        this.transform.position = gemState.transform.position;
        qLearning = GameObject.Find("Child").GetComponent<QLearning>();
        aicc = GameObject.Find("Child").GetComponent<AICharacterControl>();
    }

    public void ResetEpisode()
    {
        gemState = legalStates[UnityEngine.Random.Range(0, legalStates.Count)];
        this.transform.position = gemState.transform.position;
        pickUpGem = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate Gem
        this.transform.RotateAround(transform.position, transform.up, Time.deltaTime * 90f);
    }

    public bool isCollectingGem()
    {
        if (qLearning.visualLearning || qLearning.demo)
        {
            if (pickUpGem == true)
            {
                return true;
            }
        } else
        {
            if (aicc.currentState == states.IndexOf(gemState))
            {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "child")
        {
            pickUpGem = true;
        }
    }
}
