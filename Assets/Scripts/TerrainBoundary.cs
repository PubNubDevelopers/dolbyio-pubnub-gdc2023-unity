using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBoundary : MonoBehaviour
{
    private TerrainCollider terrainCollider;

    private void Start()
    {
        terrainCollider = FindObjectOfType<TerrainCollider>();
    }

    private void Update()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (terrainCollider.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (transform.position.y < hit.point.y)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
        }
    }
}
