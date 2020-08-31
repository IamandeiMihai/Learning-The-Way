using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    float rewardCurrentEpisode;

    public Transform start;
    public Quaternion startRotation;
    public Transform end;

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
    private GameObject[] gems;
    private bool gemCollected;

    private float caughtReward = -2;
    private float gemReward = 1;
    private float bonusReward = 1;

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
            return 1 / Mathf.Pow(mapStatus.GetDistance(nextState, states.IndexOf(end.gameObject)), 2);
        }

        return 0;
    }

    float OldManNearby(Action action)
    {
        int nr = 0;
        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        if (nextPosition != null)
        {
            int nextState = states.IndexOf(nextPosition);
            foreach (GameObject villain in villains)
            {
                int dist = 0;
                if (currentState == villain.GetComponent<VillainAI>().currentState)
                {
                    dist = mapStatus.GetDistance(nextState, villain.GetComponent<VillainAI>().oldState);
                }
                else
                {
                    dist = mapStatus.GetDistance(nextState, villain.GetComponent<VillainAI>().currentState);
                }
                if (dist < 2)
                {
                    nr++;
                }
            }
        }
        return nr;
    }

    float IsOnEnd(Action action)
    {
        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        return nextPosition.transform.parent.name.Equals("Ends") ? 1 : 0;
    }

    float DistanceToTheClosestGem(Action action)
    {
        float dist = 999;

        GameObject nextPosition = states[currentState].GetComponent<States>().NextStates()[(int)action];
        if (nextPosition != null)
        {
            int nextState = states.IndexOf(nextPosition);
            foreach (GameObject gem in gems)
            {
                if (gem.activeSelf == true)
                {
                    float gemDist = mapStatus.GetDistance(nextState, states.IndexOf(gem.GetComponent<GemsLogic>().gemState));
                    if (gemDist < dist)
                    {
                        dist = gemDist;
                    }
                }
            }
            if (dist == 999)
            {
                return 0;
            }
            if (dist == 0)
            {
                return 1;
            }
            return 1 / Mathf.Pow(dist, 1);
        }
        return 0;
    }

    void Start()
    {
        features = new List<FeatureState>
        {
            new FeatureState(Bias, 0),
            new FeatureState(DistanceToTheClosestGem, 0),
            new FeatureState(DistanceToFinish, 0),
            new FeatureState(OldManNearby, 0),
            new FeatureState(IsOnEnd, 0),
        };


        control = GetComponent<AICharacterControl>();
        control.cry = false;
        states.AddRange(GameObject.FindGameObjectsWithTag("state"));
        mapStatus = GameObject.Find("Points").GetComponent<MapStatus>();
        mapStatus.Initialze();
        villains = GameObject.FindGameObjectsWithTag("villain");

        gems = GameObject.FindGameObjectsWithTag("gem");

        foreach (GameObject gem in gems)
        {
            gem.GetComponent<GemsLogic>().Initialize();
        }

        currentState = 0;

        if (demo == true)
        {
            StreamReader reader = new StreamReader(Application.persistentDataPath + "\\" + fileName + ".txt");

            for (int i = 0; i < features.Count; i++)
            {
                features[i].weight = float.Parse(reader.ReadLine());
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
            // Episode reset

            foreach (GameObject gem in gems)
            {
                gem.GetComponent<GemsLogic>().ResetEpisode();
            }
            gemCollected = false;

            if (visualLearning)
            {
                // Visual Learning
                playerCamera.SetActive(true);
                minimapCamera.SetActive(true);
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
                // Background Learning
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
                // Visual - Episode computation
                for (int step = 0; step < maxStepsPerEpisode; ++step)
                {

                    // Action
                    Action action;

                    action = GetBestAction(currentState);

                    control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
                    control.moveDone = false;

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    reward = states[newState].GetComponent<States>().reward;

                    while (!control.moveDone)
                    {
                        foreach (GameObject gem in gems)
                        {
                            if (gem.GetComponent<GemsLogic>().isCollectingGem())
                            {
                                gemCollected = true;
                                reward = gemReward;
                                gem.GetComponent<GemsLogic>().pickUpGem = false;
                                gem.SetActive(false);

                            }
                        }

                        // Learning

                        if (this.GetComponent<AICharacterControl>().IsAttacked())
                        {
                            newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);

                            float difference = caughtReward + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);
                            for (int i = 0; i < features.Count; ++i)
                            {
                                features[i].weight = features[i].weight + learningRate * difference * features[i].value(action);
                            }

                            rewardCurrentEpisode -= caughtReward;
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

                    done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");

                    if (states[newState].name.Equals(end.name) && gemCollected == true)
                    {
                        // Bonus gem finish
                        reward += bonusReward;
                    }

                    float correction = reward + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);
                    for (int i = 0; i < features.Count; ++i)
                    {
                        features[i].weight = features[i].weight + learningRate * correction * features[i].value(action);
                    }

                    // Prepare next step
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
                // Background - Episode computation
                for (int step = 0; step < maxStepsPerEpisode; ++step)
                {

                    // Action
                    Action action;

                    action = GetBestAction(currentState);

                    newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                    this.GetComponent<AICharacterControl>().currentState = newState;
                    if (this.GetComponent<AICharacterControl>().IsAttacked())
                    {
                        reward = caughtReward;
                        done = true;
                    }
                    else
                    {
                        reward = states[newState].GetComponent<States>().reward;
                        done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");
                    }
                    foreach (GameObject gem in gems)
                    {
                        if (gem.GetComponent<GemsLogic>().isCollectingGem())
                        {
                            gemCollected = true;
                            reward = gemReward;
                            gem.GetComponent<GemsLogic>().pickUpGem = false;
                            gem.SetActive(false);
                        }
                    }


                    // Learning

                    if (states[newState].name.Equals(end.name) && gemCollected == true)
                    {
                        // Bonus gem finish
                        reward += bonusReward;
                    }

                    float correction = reward + attenuationFactor * GetMaxValue(newState) - GetQForAction(action);

                    for (int i = 0; i < features.Count; ++i)
                    {
                        features[i].weight = features[i].weight + learningRate * correction * features[i].value(action);
                    }

                    // Prepare next step

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

                    foreach (GameObject villain in villains)
                    {
                        villain.GetComponent<VillainAI>().ChangeState();
                    }
                }
            }

            if (episode % 100 == 0)
            {
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
        for (int episode = 0; episode < 5; ++episode)
        {
            playerCamera.SetActive(true);
            minimapCamera.SetActive(true);
            // State reset
            this.transform.position = start.position;
            this.transform.rotation = startRotation;
            done = false;
            demo_done = false;
            rewardCurrentEpisode = 0;
            currentState = 0;
            newState = 0;
            this.GetComponent<AICharacterControl>().attacked = false;
            this.GetComponent<AICharacterControl>().currentState = states.IndexOf(start.gameObject);

            foreach (GameObject villain in villains)
            {
                villain.GetComponent<VillainAI>().resetVillain();
            }

            foreach (GameObject gem in gems)
            {
                gem.GetComponent<GemsLogic>().ResetEpisode();
            }

            while (!done)
            {

                // Action
                Action action;
                action = GetBestAction(currentState);

                control.target = states[currentState].GetComponent<States>().NextStates()[(int)action].transform;
                control.moveDone = false;

                newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);

                while (!control.moveDone)
                {
                    foreach (GameObject gem in gems)
                    {
                        if (gem.GetComponent<GemsLogic>().isCollectingGem())
                        {
                            reward = gemReward;
                            gem.GetComponent<GemsLogic>().pickUpGem = false;
                            gem.SetActive(false);
                        }
                    }

                    if (this.GetComponent<AICharacterControl>().IsAttacked())
                    {
                        newState = states.IndexOf(states[currentState].GetComponent<States>().NextStates()[(int)action]);
                        rewardCurrentEpisode -= caughtReward;

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

                reward = states[newState].GetComponent<States>().reward;

                done = states[newState].transform == end || states[newState].transform.parent.name.Equals("Ends");

                if (states[newState].name.Equals(end.name) && gemCollected == true)
                {
                    reward += bonusReward;
                }

                // Prepare next step

                currentState = newState;
                rewardCurrentEpisode += reward;

                if (done == true)
                {
                    demo_done = true;
                    break;
                }
                yield return null;
            }
            yield return new WaitForSeconds(4);
        }
    }



    void OnApplicationQuit()
    {
        string path = Application.persistentDataPath;
        string name = "/WEIGHTS";

        for (int i = 0; i < 10; i++)
        {
            name += chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        name += ".txt";
        
        Debug.Log(path + name);

        StreamWriter writer = new StreamWriter(path + name);
        for (int i = 0; i < features.Count; i++)
        {
            writer.WriteLine(String.Format("{0}", features[i].weight));
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
