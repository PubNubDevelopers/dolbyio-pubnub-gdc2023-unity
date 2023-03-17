using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TvCollider : MonoBehaviour
{
    private PubNubManager _pnManager;

    private void Start()
    {
        _pnManager = GameObject.Find("Multiplayers").GetComponent<PubNubManager>();
    }

    /// <summary>
    /// Triggerred whenever the player enters a television's bounds. Set in Audience.cs when setting up streamingSetting
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.name.Equals("FirstPlayer"))
        {
            Debug.Log("Player Entered the trigger");
            _pnManager.EnteredTVRadius(this.name);
        }
    }

    /// <summary>
    /// Triggerred whenever the player exits a television's bounds. Set in Audience.cs when setting up streamingSetting
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.transform.name.Equals("FirstPlayer"))
        {
            Debug.Log("Player within the trigger");
            _pnManager.ExitTVRadius(this.name);
        }
    }
}
