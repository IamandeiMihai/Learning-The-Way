using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    private AICharacterControl control;


    int numberOfEpisodes = 10000;
    int maxStepsPerEpisode = 50;

    float learningRate = 0.1f;
    float discountRate = 0.99f;

    [SerializeField] List<float> rewardsAllEpisodes;

    float rewardCurrentEpisode;

    public Transform start;
    public Quaternion startRotation;
    public Transform end;

    private float[][] qTable;
    public List<GameObject> states;
    private int currentState;

    private int newState;
    private float reward;
    private bool done;

    void Start()
    {
        control = GetComponent<AICharacterControl>();
        
        qTable = new float[100][];
        for (int i = 0; i < 100; ++i)
        {
            qTable[i] = new float[4];
        }

        states.AddRange(GameObject.FindGameObjectsWithTag("state"));
        currentState = 0;

        StartCoroutine(qLearning());
    }

    Action GetBestAction(int currentState)
    {
        GameObject[] neighbours = states[currentState].GetComponent<States>().NextStates();

        Action bestAction = Action.IDLE;
        float bestValue = float.MinValue;

        for (int i = 0; i < 4; ++i)
        {
            if (neighbours[i] != null)
            {
                if (qTable[currentState][i] > bestValue)
                {
                    bestValue = qTable[currentState][i];
                    bestAction = (Action)i;
                }
                else
                {
                    if (qTable[currentState][i] == bestValue)
                    {
                        if (UnityEngine.Random.Range(0, 1) == 1)
                        {
                            bestValue = qTable[currentState][i];
                            bestAction = (Action)i;
                        }
                    }
                }
            }
        }

        return bestAction;
    }

    IEnumerator DoStep(Action action)
    {
        control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
        control.moveDone = false;
        while (!control.moveDone) { }

        newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
        reward = states[newState].GetComponent<States>().reward;
        done = states[newState].transform == end;

        yield return null;

    }

    float GetMaxValue(int state)
    {
        GameObject[] neighbours = states[currentState].GetComponent<States>().NextStates();

        float maxValue = float.MinValue;

        for (int i = 0; i < 4; ++i)
        {
            if (neighbours[i] != null)
            {
                if (qTable[state][i] > maxValue)
                {
                    maxValue = qTable[state][i];
                }
            }
        }
        return maxValue;
    }
    
    IEnumerator qLearning()
    {
        for (int i = 0; i < numberOfEpisodes; ++i)
        {
            // State reset
            this.transform.position = start.position;
            this.transform.rotation = startRotation;
            done = false;
            rewardCurrentEpisode = 0;

            // Learning
            for (int step = 0; step < maxStepsPerEpisode; ++step)
            {
                // Action
                Action action = GetBestAction(currentState);
                DoStep(action);

                // Learning
                qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * (reward + discountRate * GetMaxValue(newState));

                currentState = newState;
                rewardCurrentEpisode += reward;

                if (done == true)
                {
                    break;
                }
                yield return null;
            }

            rewardsAllEpisodes.Add(rewardCurrentEpisode);
            yield return null;
        }
        yield return null;
    }
}

enum Action
{
    NORTH = 0,
    EAST = 1,
    SOUTH = 2,
    WEST = 3,
    IDLE = 4
}
