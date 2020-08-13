using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class States : MonoBehaviour
{
    public float reward;

    public GameObject northNeighbour;
    public GameObject eastNeighbour;
    public GameObject southNeighbour;
    public GameObject westNeighbour;

    public GameObject[] NextStates()
    {
        GameObject[] nextStates = { northNeighbour, eastNeighbour, southNeighbour, westNeighbour };
        return nextStates;
    }

    public GameObject[] NotNullNextStates()
    {

        List<GameObject> nextStates = new List<GameObject>();

        if (northNeighbour != null)
        {
            nextStates.Add(northNeighbour);
        }

        if (eastNeighbour != null)
        {
            nextStates.Add(eastNeighbour);
        }

        if (southNeighbour != null)
        {
            nextStates.Add(southNeighbour);
        }

        if (westNeighbour != null)
        {
            nextStates.Add(westNeighbour);
        }

        return nextStates.ToArray();
    }
}
