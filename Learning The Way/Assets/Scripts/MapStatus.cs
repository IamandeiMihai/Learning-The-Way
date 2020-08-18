using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapStatus : MonoBehaviour
{
    [Serializable]
    class DistanceMatrix
    {
        [SerializeField] public int[] distances;
    };

    [SerializeField] DistanceMatrix[] distanceToState;

    List<GameObject> states;

    // Start is called before the first frame update
    public void Initialze()
    {
        states = GameObject.Find("Child").GetComponent<QLearning>().states;
        distanceToState = new DistanceMatrix[states.Count];
        for (int i = 0; i < states.Count; ++i)
        {
            distanceToState[i] = new DistanceMatrix();
            distanceToState[i].distances = new int[states.Count];
        }


        for (int state = 0; state < states.Count; ++state)
        {
            BFS(state);
        }
    }

    void BFS(int start)
    {
        Queue<int> queue = new Queue<int>();
        bool[] visited = new bool[states.Count];

        for (int i = 0; i < visited.Length; ++i)
        {
            visited[i] = false;
        }

        queue.Enqueue(start);
        visited[start] = true;
        distanceToState[start].distances[start] = 0;
        

        while (queue.Count != 0)
        {
            int currentState = queue.Dequeue();
            GameObject[] neighbours = states[currentState].GetComponent<States>().NotNullNextStates();

            foreach(GameObject neighbour in neighbours)
            {
                int nextState = states.IndexOf(neighbour);
                if (!visited[nextState])
                {
                    queue.Enqueue(nextState);
                    visited[nextState] = true;
                    distanceToState[start].distances[nextState] = distanceToState[start].distances[currentState] + 1;
                }
            }
        }
    }

    public int GetDistance(int start, int target)
    {
        return distanceToState[start].distances[target];
    }

    public int GetNextStateToTarget(int start, int target)
    {
        GameObject[] neighbours = states[start].GetComponent<States>().NotNullNextStates();
        int minDistance = 100;
        int closestNeighbour = -1;

        foreach(GameObject neighbour in neighbours)
        {
            int idx = states.IndexOf(neighbour);
            if (minDistance > distanceToState[idx].distances[target])
            {
                minDistance = distanceToState[idx].distances[target];
                closestNeighbour = System.Array.IndexOf(neighbours, neighbour);
            }
        }
        return closestNeighbour;
    }
}
