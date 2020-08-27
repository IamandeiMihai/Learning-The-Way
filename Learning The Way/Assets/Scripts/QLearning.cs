using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    class FeatureState {
        public Feature value;
        public float weight;

        public FeatureState(Feature value, float weight)
        {
            this.value = value;
            this.weight = weight;
        }
    };

    delegate float Feature(Action action);

    List<FeatureState> features;

    [Range(1.0f, 50.0f)]
    public float timeSpeed;

    public bool visualLearning;

    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

    private AICharacterControl control;

    public int numberOfEpisodes;
    public int maxStepsPerEpisode;

    public float learningRate;
    public float attenuationFactor;


    [SerializeField] float explorationRate;
    float maxExplorationRate = 1;
    float minExplorationRate = 0.01f;
    public float explorationDecayRate;

    //List<float> rewardsAllEpisodes;

    List<bool> visited;

    float rewardCurrentEpisode;

    public Transform start;
    public Quaternion startRotation;
    public Transform end;

    private float[][] qTable;
    public List<GameObject> states;
    private int currentState;

    public int newState;
    private float reward;
    private bool done;

    public bool demo;
    public bool demo_done = false;
    public String fileName;

    int winRatio;

    public GameObject playerCamera;
    public GameObject minimapCamera;

    private GameObject[] villains;

    MapStatus mapStatus;


    float Bias(Action action)
    {
        return 1f;
    }

    float DistanceToFinish(Action action)
    {
        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        if (nextPosition != null)
        {
            int nextState = states.IndexOf(nextPosition);
            if (nextPosition.name.Equals(end.name))
            {
                return 1;
            }
            return 1 / Mathf.Pow(mapStatus.GetDistance(nextState, states.IndexOf(end.gameObject)), 1);
        }

        return 0;
    }


    float OldManNearby(Action action)
    {
        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        if (nextPosition != null)
        {
            int nextState = states.IndexOf(nextPosition);
            foreach (GameObject villain in villains)
            {
                int dist = mapStatus.GetDistance(nextState, villain.GetComponent<VillainAI>().currentState);
                if (dist == 0)
                {
                    dist = mapStatus.GetDistance(nextState, villain.GetComponent<VillainAI>().oldState);
                }
                if (dist < 2)
                {
                    return 1;
                }
            }
        }
        return 0;
    }

    float IsOnEnd(Action action)
    {
        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        return nextPosition.transform.parent.name.Equals("Ends") ? 1 : 0;
    }

    void Start()
    {
        features = new List<FeatureState>
        {
            new FeatureState(Bias, 0),
            new FeatureState(DistanceToFinish, 0),
            new FeatureState(OldManNearby, 0),
            new FeatureState(IsOnEnd, 0)
        };


        //rewardsAllEpisodes = new List<float>();

        control = GetComponent<AICharacterControl>();

        states.AddRange(GameObject.FindGameObjectsWithTag("state"));
        mapStatus = GameObject.Find("Points").GetComponent<MapStatus>();
        mapStatus.Initialze();
        villains = GameObject.FindGameObjectsWithTag("villain");
        visited = new List<bool>();
        for (int i = 0; i < states.Count; ++i)
        {
            visited.Add(false);
        }
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

            StartCoroutine(run_demo());
        }
        else
        {
            StartCoroutine(qLearning());

        }

        
    }

    void Update()
    {
        Time.timeScale = timeSpeed;
    }

    public int GetIndexState(GameObject state)
    {
        return states.IndexOf(state);
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

    float GetQForAction(Action action)
    {
        float q = 0;
        foreach (FeatureState featureState in features)
        {
            q += featureState.weight * featureState.value(action);
        }

        return q;
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
                float q = GetQForAction((Action)i);

                if (q > bestValue || bestAction == Action.IDLE)
                {
                    bestValue = q;
                    bestAction = (Action)i;
                } else
                {
                    if (q == bestValue)
                    {
                        if (UnityEngine.Random.Range(0, 2) == 1)
                        {
                            bestValue = q;
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
                float q = GetQForAction((Action)i);

                if (q > maxValue)
                {
                    maxValue = q;
                }
            }
        }

        return maxValue;
    }

    IEnumerator qLearning()
    {
        for (int episode = 1; episode <= numberOfEpisodes; ++episode)
        {
            for (int i = 0; i < visited.Count; ++i)
            {
                visited[i] = false;
            }


            if (visualLearning)
            {
                playerCamera.SetActive(true);
                minimapCamera.SetActive(true);
                // State reset
                this.transform.position = start.position;
                this.transform.rotation = startRotation;
                done = false;
                rewardCurrentEpisode = 0;
                currentState = 0;
                newState = 0;
                this.GetComponent<AICharacterControl>().attacked = false;
                this.GetComponent<AICharacterControl>().currentState = states.IndexOf(start.gameObject);
            }
            else
            {
                playerCamera.SetActive(false);
                minimapCamera.SetActive(false);
                done = false;
                rewardCurrentEpisode = 0;
                currentState = UnityEngine.Random.Range(0, states.Count);
                newState = currentState;
                this.GetComponent<AICharacterControl>().currentState = currentState;
            }

            foreach (GameObject villain in villains)
            {
                villain.GetComponent<VillainAI>().resetVillain();
            }

            if (visualLearning)
            {

                // Learning
                for (int step = 0; step < maxStepsPerEpisode; ++step)
                {

                    // Action
                    float explorationRateThreshold = UnityEngine.Random.Range(0f, 1f);
                    Action action;
                    //if (explorationRateThreshold > explorationRate)
                    //{
                    action = GetBestAction(currentState);
                    //}
                    //else
                    //{
                    //    action = GetRandomAction(currentState);
                    //}
                    // visited[currentState] = true;

                    control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
                    control.moveDone = false;

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);

                    while (!control.moveDone)
                    {
                        if (this.GetComponent<AICharacterControl>().IsAttacked())
                        {
                            newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                            // qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * ((-10) + attenuationFactor * GetMaxValue(newState));

                            float difference = (-1) + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);
                            for (int i = 0; i < features.Count; ++i)
                            {
                                features[i].weight = features[i].weight + learningRate * difference * features[i].value(action);
                            }

                            rewardCurrentEpisode -= 1;
                            done = true;
                            break;
                        }
                        yield return null;
                    }
                    this.GetComponent<AICharacterControl>().currentState = newState;

                    if (done == true)
                    {
                        break;
                    }

                    if (visited[newState] == true)
                    {
                        reward = -5;
                    }
                    else
                    {
                        reward = states[newState].GetComponent<States>().reward;
                    }

                    done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");

                    // Learning
                    // qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * (reward + attenuationFactor * GetMaxValue(newState));

                    float correction = reward + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);
                    for (int i = 0; i < features.Count; ++i)
                    {
                        features[i].weight = features[i].weight + learningRate * correction * features[i].value(action);
                    }

                    currentState = newState;
                    rewardCurrentEpisode += reward;

                    if (done == true)
                    {
                        break;
                    }
                    yield return null;
                }
            }
            else
            {
                // Learning
                for (int step = 0; step < maxStepsPerEpisode; ++step)
                {

                    // Action
                    float explorationRateThreshold = UnityEngine.Random.Range(0f, 1f);
                    Action action;
                    //if (explorationRateThreshold > explorationRate)
                    //{
                    action = GetBestAction(currentState);
                    //}
                    //    else
                    //{
                    //    action = GetRandomAction(currentState);
                    //}
                    // visited[currentState] = true;

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    this.GetComponent<AICharacterControl>().currentState = newState;
                    if (this.GetComponent<AICharacterControl>().IsAttacked())
                    {
                        reward = -1;
                        done = true;
                    }
                    else
                    {
                        if (visited[newState] == true)
                        {
                            reward = -5;
                        }
                        else
                        {
                            reward = states[newState].GetComponent<States>().reward;
                        }
                        done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");
                    }

                    // Learning
                    // qTable[currentState][(int)action] = qTable[currentState][(int)action] * (1 - learningRate) + learningRate * (reward + attenuationFactor * GetMaxValue(newState));

                    float correction = reward + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);

                    for (int i = 0; i < features.Count; ++i)
                    {
                        features[i].weight = features[i].weight + learningRate * correction * features[i].value(action);

                    }

                    currentState = newState;
                    rewardCurrentEpisode += reward;

                    if (done == true)
                    {
                        if (states[newState].transform == end)
                        {
                            winRatio++;
                        }
                        break;
                    }

                    // mut inamicii
                    foreach (GameObject villain in villains)
                    {
                        villain.GetComponent<VillainAI>().ChangeState();
                    }
                }
            }

            //explorationRate = minExplorationRate + (maxExplorationRate - minExplorationRate) * Mathf.Exp(-explorationDecayRate * episode);

            //rewardsAllEpisodes.Add(rewardCurrentEpisode);

            if (episode % 100 == 0)
            {
                /*float sum = 0;
                for (int i = 0; i < rewardsAllEpisodes.Capacity; ++i)
                {
                    sum += rewardsAllEpisodes[i];
                }
                Debug.Log(episode + ": " + sum / (numberOfEpisodes / 100));
                rewardsAllEpisodes.Clear();*/

                Debug.Log(episode + ": " + (float)winRatio / 100);
                winRatio = 0;
                if (!visualLearning)
                {
                    yield return null;
                }
            }

            if (visualLearning)
            {
                yield return null;
            }

            if (episode > numberOfEpisodes - 5)
            {
                visualLearning = true;
                yield return null;
            }
        }
        yield return null;
    }



    IEnumerator run_demo()
    {
        playerCamera.SetActive(true);
        minimapCamera.SetActive(true);

        // State reset
        this.transform.position = start.position;
        this.transform.rotation = startRotation;
        done = false;
        rewardCurrentEpisode = 0;
        currentState = states.IndexOf(start.gameObject);
        newState = 0;
        this.GetComponent<AICharacterControl>().currentState = states.IndexOf(start.gameObject);

        // Learning
        while (!done)
        {

            // Action
            float explorationRateThreshold = UnityEngine.Random.Range(0f, 1f);
            Action action = GetBestAction(currentState);

            control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
            control.moveDone = false;
            while (!control.moveDone)
            {
                if (this.GetComponent<AICharacterControl>().IsAttacked())
                {
                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    rewardCurrentEpisode -= 10;
                    done = true;
                    break;
                }
                yield return null;
            }
            this.GetComponent<AICharacterControl>().currentState = newState;


            if (done == true)
            {
                break;
            }

            newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
            reward = states[newState].GetComponent<States>().reward;
            done = states[newState].transform == end;

            currentState = newState;
            rewardCurrentEpisode += reward;

            if (done == true)
            {
                demo_done = true;
                break;
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

    public bool visualVillains()
    {
        return demo || visualLearning;
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
