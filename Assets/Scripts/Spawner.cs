using UnityEngine;
using Mirror;

public class Spawner : MonoBehaviour // Doesn't even need to be NetworkBehaviour anymore
{
    public GameObject[] tetrominoes;

    // Just a helper function to get a random block
    public GameObject GetRandomPrefab()
    {
        int i = Random.Range(0, tetrominoes.Length);
        return tetrominoes[i];
    }
}