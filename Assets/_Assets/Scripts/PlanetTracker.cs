using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTracker : Singleton<PlanetTracker>
{
    [SerializeField] private Planet[] planets;

    public Vector3 GetNetGravityDir(Vector3 _playerPos)
    {
        Vector3 newGrav = new Vector3(0, 0, 0);
        foreach (Planet planet in planets)
        {
            Vector3 planetGrav = (planet.transform.position - _playerPos) * (planet.Mass / Vector3.SqrMagnitude(_playerPos - planet.transform.position));
            if (planetGrav.sqrMagnitude > newGrav.sqrMagnitude)
                newGrav = planetGrav;
        }

        //Debug.Log(newGrav.magnitude);

        return newGrav;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        planets = FindObjectsOfType<Planet>(false);
    }
#endif
}
