using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PubNubAPI;
using System;
using UnityEditor;

/// <summary>
/// A Scriptable Object which can be used to configure the object to connect to the PubNub network.
/// More information on where to get those details from can be found in https://pubnub.com/docs
/// Get your Publish and Subscribe keys from the Admin Portal via https://admin.pubnub.com by creating an app and keyset.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "PubNubCredentials", menuName = "PubNubCredentials/PubNubCredentials")]
public class PubNubCredentials : ScriptableObject
{
    /// <summary>
    /// The Publish Key is required to be able to send messages in the PubNub Network.
    /// </summary>
    [Tooltip("Located this publish key in your keyset in the Admin Portal")]
    public string PublishKey;

    /// <summary>
    /// The Subscribe Key is required to be able to receive messages in the PubNub Network.
    /// </summary>
    [Tooltip("Find this subscribe key in your keyset in the Admin Portal")]
    public string SubscribeKey;

    /// <summary>
    /// The UserID (also known as Unique User Identifier (UUID) is required by PubNub to identify the client in the network.
    /// Either use the auto generated UserID or change to match your own desired ID.
    /// </summary>
    [Tooltip("Enter a unique ID that identifiers your user. If you do not, one will be automatically generated for you.")]
    public string UserID;

    /// <summary>
    /// A default channel to used to publis/subscribe to send/receive messages.
    /// Not required, but a starting point for your app.
    /// </summary>
    [Tooltip("Enter a default channel to publish/receive messages. If you do not, one will be automatically generated for you.")]
    public string ChannelName;
}
