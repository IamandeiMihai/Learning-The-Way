using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class States : MonoBehaviour
{
    public GameObject northNeighbour;
    public GameObject eastNeighbour;
    public GameObject southNeighbour;
    public GameObject westNeighbour;

    public GameObject[] NextStates()
    {
        GameObject[] nextStates = { northNeighbour, eastNeighbour, southNeighbour, westNeighbour };
        return nextStates;
    }
}
