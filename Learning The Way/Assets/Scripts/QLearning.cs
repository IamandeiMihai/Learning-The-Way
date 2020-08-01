using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    private AICharacterControl control;


    public int numberOfEpisodes = 10000;
    public int maxStepsPerEpisode = 30;

    public float learningRate = 0.7f;
    public float attenuationFactor = 0.99f;


    float explorationRate = 1;
    float maxExplorationRate = 1;
    float minExplorationRate = 0.01f;
    public float explorationDecayRate = 0.01f;

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

    Action GetRandomAction(int currentState)
    {
        GameObject[] neighbours = states[currentState].GetComponent<States>().NextStates();
        List<int> nonNull = new List<int>();
        for (int i = 0; i < 4; ++i)
        {
            if (neighbours[i] != null)
            {
                nonNull.Add(i);
            }
        }
        return (Action)nonNull[UnityEngine.Random.Range(0, nonNull.Count)];
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
                        if (UnityEngine.Random.Range(0, 2) == 1)
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
        for (int episode = 0; episode < numberOfEpisodes; ++episode)
        {
            // State reset
            this.transform.position = start.position;
            this.transform.rotation = startRotation;
            done = false;
            rewardCurrentEpisode = 0;
            currentState = 0;
            newState = 0;

            // Learning
            for (int step = 0; step < maxStepsPerEpisode; ++step)
            {

                // Action
                float explorationRateThreshold = UnityEngine.Random.Range(0f, 1f);
                Action action;
                if (explorationRateThreshold > explorationRate)
                {
                    action = GetBestAction(currentState);   
                }
                else
                {
                    action = GetRandomAction(currentState);
                }
                control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
                control.moveDone = false;
                while (!control.moveDone) { yield return null; }

                newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                reward = states[newState].GetComponent<States>().reward;
                done = states[newState].transform == end;

                // Learning
                qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * (reward + attenuationFactor * GetMaxValue(newState));

                currentState = newState;
                rewardCurrentEpisode += reward;

                if (done == true)
                {
                    break;
                }
                yield return null;
            }

            explorationRate = minExplorationRate + (maxExplorationRate - minExplorationRate) * Mathf.Exp(-explorationDecayRate * episode);

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
