using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    public bool visualLearning;

    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

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

    public bool demo;
    public String fileName;

 

    void Start()
    {
        control = GetComponent<AICharacterControl>();

        states.AddRange(GameObject.FindGameObjectsWithTag("state"));
        currentState = 0;

        qTable = new float[states.Count][];
        for (int i = 0; i < states.Count; ++i)
        {
            qTable[i] = new float[4];
        }

        if (demo == true)
        {
            StreamReader reader = new StreamReader(Application.persistentDataPath + "\\" + fileName + ".txt");
            
            for (int i = 0; i < states.Count; i++)
            {
                string[] line = reader.ReadLine().Split();
                qTable[i][0] = float.Parse(line[0]);
                qTable[i][1] = float.Parse(line[1]);
                qTable[i][2] = float.Parse(line[2]);
                qTable[i][3] = float.Parse(line[3]);
            }
        }

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
        for (int episode = 1; episode <= numberOfEpisodes; ++episode)
        {
            if (visualLearning)
            {
                // State reset
                this.transform.position = start.position;
                this.transform.rotation = startRotation;
                done = false;
                rewardCurrentEpisode = 0;
                currentState = 0;
                newState = 0;
            } else
            {
                done = false;
                rewardCurrentEpisode = 0;
                currentState = UnityEngine.Random.Range(0, states.Count);
                newState = currentState;
            }

            if (visualLearning)
            {

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
                    Debug.Log(qTable[currentState][0] + " " + qTable[currentState][1] + " " + qTable[currentState][2] + " " + qTable[currentState][3]);
                    control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
                    control.moveDone = false;
                    while (!control.moveDone) { yield return null; }

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    reward = states[newState].GetComponent<States>().reward;
                    done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");

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
            } else
            {
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

                   

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    reward = states[newState].GetComponent<States>().reward;
                    done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");

                    // Learning
                    qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * (reward + attenuationFactor * GetMaxValue(newState));

                    currentState = newState;
                    rewardCurrentEpisode += reward;

                    if (done == true)
                    {
                        break;
                    }
                }
            }

            explorationRate = minExplorationRate + (maxExplorationRate - minExplorationRate) * Mathf.Exp(-explorationDecayRate * episode);

            rewardsAllEpisodes.Add(rewardCurrentEpisode);

            if (episode % 1000 == 0)
            {
                float sum = 0;
                for (int i = episode - 1000; i < episode; ++i)
                {
                    sum += rewardsAllEpisodes[i];
                }
                Debug.Log(episode + ": " + sum / 1000f);
            } 
            yield return null;
        }
        yield return null;
    }

    void OnApplicationQuit()
    {
        string path = Application.persistentDataPath;
        string name = "/QTABLE";

        for (int i = 0; i < 10; i++)
        {
            name += chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        name += ".txt";
        
        Debug.Log(path + name);

        StreamWriter writer = new StreamWriter(path + name);
        for (int i = 0; i < qTable.Length; i++)
        {
            writer.WriteLine(String.Format("{0} {1} {2} {3}", qTable[i][0], qTable[i][1], qTable[i][2], qTable[i][3]));
        }
        writer.WriteLine();
        writer.Close();
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
