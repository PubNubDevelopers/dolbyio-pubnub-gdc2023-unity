using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Dolby.Millicast;
using UnityEngine;
using PubNubAPI;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
using StarterAssets;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;
using System.Diagnostics;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class PubNubManager : MonoBehaviour
{
    //PubNub Required Credentials
    [Header("PubNub Credentials")]
    public PubNubCredentials credentials;

    //UI Fields

    //Chat
    [Header("Chat")]
    public Text chatMessageDisplay;
    public InputField chatInputField;

    //Presence
    [Header("Presence")]
    public Text totalCount;

    //Emotes
    [Header("Emotes")]
    public GameObject emoteWheel;
    public GameObject emojiDisplay;

    [Header("Prefab")]
    public GameObject Prefab;

    //Helper
    private PubNub pubnub;
    private GameObject chatContainer;
    private List<string> _channelNames = new List<string>();
    private Audience _audience;
    private GameObject _player;
    private List<string> _debugConsolePlayers = new List<string>();

    void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeConnection();
        pubnub.SubscribeCallback += SubscribeCallbackHandler;
        Subscribe(new List<string>
        {
            credentials.ChannelName,
            "streamer.*",
            "debug-console.*"
        }); 
        chatContainer = GameObject.Find("UICanvas").transform.Find("Chat").gameObject;
        _audience = GameObject.Find("TVs").GetComponent<Audience>();
        _player = GameObject.Find("FirstPlayer");
        //Handles the initial presence information.
        InitialPresenceFirstLoad();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Determines the validity of the PubNub credentials.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private void InitializeConnection()
    {
        if (string.IsNullOrWhiteSpace(credentials.UserID))
        {
            credentials.UserID = SystemInfo.deviceUniqueIdentifier;
        }

        if(string.IsNullOrWhiteSpace(credentials.ChannelName))
        {
            credentials.ChannelName = "default";
        }
        // Initialize the PubNub Connection.
        if (CheckValidCredentials(credentials) || pubnub == null)
        {
            //Required PubNub Arguments
            PNConfiguration pnConfiguration = new PNConfiguration();
            pnConfiguration.PublishKey = credentials.PublishKey;
            pnConfiguration.SubscribeKey = credentials.SubscribeKey;
            pnConfiguration.UserId = credentials.UserID;

            //Optional args
            pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;

            pubnub = new PubNub(pnConfiguration);
        }
        if (!CheckValidCredentials(credentials))
        {
            throw new Exception(GetCredentialsErrorMessage(credentials));
        }
    }
    /// <summary>
    ///  Event Handlers for listening for events on the PubNub network.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SubscribeCallbackHandler(object sender, EventArgs e)
    {
        SubscribeEventEventArgs subscribeEventEventArgs = e as SubscribeEventEventArgs;
      
        if (subscribeEventEventArgs.MessageResult != null)
        {       
            //Handle messages between the debug console and Unity player.
            if (subscribeEventEventArgs.MessageResult.Channel.Equals("default"))
            {
                //Display Chat
                DisplayChat(subscribeEventEventArgs.MessageResult);
            }   
        }
        
        if (subscribeEventEventArgs.PresenceEventResult != null)
        {  
            //Update global count for default channel.
            if(subscribeEventEventArgs.PresenceEventResult.Channel.Equals("default"))
            {
                //For spatial presence, there is an additional leave event generated when the player enters a tv's bounds.
                //Ensuring the total presence count isn't affected by this leave event since the player hasn't fully left the game.
                bool falseLeaveDefault = subscribeEventEventArgs.PresenceEventResult.UUID.Equals(pubnub.PNConfig.UserId) && subscribeEventEventArgs.PresenceEventResult.Event.Equals("leave");
                if (!falseLeaveDefault)
                {
                    totalCount.text = subscribeEventEventArgs.PresenceEventResult.Occupancy.ToString();
                }

                if(subscribeEventEventArgs.PresenceEventResult.UUID.StartsWith("debug-console"))
                {
                    if(subscribeEventEventArgs.PresenceEventResult.Event.Equals("join"))
                    {
                        GenerateDebugConsolePlayer(subscribeEventEventArgs.PresenceEventResult.UUID);
                    }

                    else
                    {
                        RemoveDebugConsolePlayer(subscribeEventEventArgs.PresenceEventResult.UUID);
                    }
                }
            }

            //Handle updates for mobile experience users.
            if (subscribeEventEventArgs.PresenceEventResult.Channel.StartsWith("streamer."))
            {
                //Handle of joining or removing connections.
                if(subscribeEventEventArgs.PresenceEventResult.UUID.StartsWith("GDC_Stream_"))
                {
                    //Spawn new tvs of users on join events
                    if (subscribeEventEventArgs.PresenceEventResult.Event.Equals("join"))
                    {
                        //The stream name is the UUID as set in the mobile app.
                        _audience.AddStream(subscribeEventEventArgs.PresenceEventResult.UUID);
                    }

                    //remove tv's for leave or time out. Mobile User has stopped sharing the experience, closed the app, or timed out.
                    else if (subscribeEventEventArgs.PresenceEventResult.Event.Equals("leave") || subscribeEventEventArgs.PresenceEventResult.Event.Equals("timeout"))
                    {
                        _audience.RemoveStream(subscribeEventEventArgs.PresenceEventResult.UUID);
                    }
                }

                //Update presence count for a tv.
                else
                {
                    GameObject tv = _audience.FindStream(subscribeEventEventArgs.PresenceEventResult.Channel.Substring(9)); //grab the tv from the channel name
                    if (tv != null)
                    {
                        int occupancy = subscribeEventEventArgs.PresenceEventResult.Occupancy - 1 >= 0 ? subscribeEventEventArgs.PresenceEventResult.Occupancy - 1 : 0;
                        //Update the tv component's spatial presence count.
                        tv.GetComponentInChildren<TextMesh>().text = occupancy.ToString();
                    }
                }              
            }   
        }
    }

    /// <summary>
    /// PubNub subscribe operations. Listens for Events occurring on the network.
    /// </summary>
    /// <param name="channels"></param>
    public void Subscribe(List<string> channels)
    {
        pubnub.Subscribe()
            .Channels(channels)
            .WithPresence()
            .Execute();
    }

    /// <summary>
    /// Determines if the user has entered in the required fields to connect to the pubnub network.
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns></returns>
    private bool CheckValidCredentials(PubNubCredentials credentials)
    {
        if (credentials == null) return false;

        return !string.IsNullOrWhiteSpace(credentials.PublishKey) &&
               !string.IsNullOrWhiteSpace(credentials.SubscribeKey) &&
               !string.IsNullOrWhiteSpace(credentials.UserID);
    }

    /// <summary>
    /// Formats the error message to throw in case one of the PubNub Credentials scriptable object fields are missing.
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns></returns>
    private string GetCredentialsErrorMessage(PubNubCredentials credentials)
    {
        string message = "";
        if (string.IsNullOrWhiteSpace(credentials.PublishKey))
            return "Publish Key cannot be empty. Please obtain from the PubNub Admin Portal.";
        if (string.IsNullOrWhiteSpace(credentials.SubscribeKey))
            return "Subscribe Key cannot be empty. Please obtain from the PubNub Admin Portal.";
        if (string.IsNullOrWhiteSpace(credentials.UserID))
            return "UserID cannot be empty. Please enter a unique User ID or clear the field to allow the system to generate one for you.";

        return message + " can't be Empty. Please configure in PubNub Credentials Scriptable Object";
    }

    private void DisplayChat(PNMessageResult message)
    {
        string finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(Color.white) + ">" + "[" + message.IssuingClientId + "]: " + message.Payload.ToString() + "</color>";

        chatMessageDisplay.text += finalMessage;

        // Canvas refresh:
        Canvas.ForceUpdateCanvases();
        //vlg.enabled = false;
        //vlg.enabled = true;
    }

    /// <summary>
    /// Sends a chat message to other users.
    /// </summary>
    public void SendChatMessage()
    {
        if (!string.IsNullOrEmpty(chatInputField.text))
        {                   
            pubnub.Publish()
                .Channel(credentials.ChannelName)
                .Message(chatInputField.text)
                //.Meta(metaDict)
                .Async((result, status) => {
                    if (!status.Error)
                    {
                        UnityEngine.Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow, result.Timetoken));
                    }
                    else
                    {
                        //Handle Error
                        UnityEngine.Debug.Log(status.Error);
                        UnityEngine.Debug.Log(status.ErrorData.Info);
                    }
                });

            //Clear the input field.
            chatInputField.text = string.Empty;
        }
    }

    /// <summary>
    /// Opens/Closes the Emote Wheel.
    /// </summary>
    public void EmoteWheelOnClick()
    {
        //Close the friend list if open.
        if (emoteWheel.activeSelf)
        {
            emoteWheel.SetActive(false);
        }

        //Open friend list.
        else
        {
            emoteWheel.SetActive(true);
        }
    }
  
    /// <summary>
    /// Renders the clicked emoji above player head. Place reference to emoji above players head in the inspector.
    /// </summary>
    public void DisplayEmoji(Sprite emoji)
    {
        emoteWheel.SetActive(false); //Hide the emote wheel after clicking an emoji
        emojiDisplay.SetActive(true);
        emojiDisplay.GetComponent<Image>().sprite = emoji;
        StartCoroutine(DisplayEmoji());
    }

    /// <summary>
    /// Waits for 5 seconds before deactivating emoji.
    /// </summary>
    /// <returns></returns>
    IEnumerator DisplayEmoji()
    {
        yield return new WaitForSecondsRealtime(5);
        emojiDisplay.SetActive(false);
    }

    /// <summary>
    /// Opens/closes the chat window.
    /// </summary>
    public void ToggleChatWindow(bool toggle)
    {
        if(toggle)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField(); // Set focus on input field.
        }
        chatContainer.SetActive(toggle);
    }

    /// <summary>
    /// Returns a bool that states the chat window is open or closed.
    /// </summary>
    /// <returns></returns>
    public bool IsChatOpen()
    {
        return chatContainer.activeSelf;
    }

    /// <summary>
    /// Returns a bool that states if the user has finished typing a message when hitting enter key.
    /// </summary>
    /// <returns></returns>
    public bool HasFinishedTyping()
    {
        return String.IsNullOrWhiteSpace(chatInputField.text);
    }

    /// <summary>
    /// The player has entered the specific TV's bounds. Subscribe to that tv's spatial presence channel.
    /// </summary>
    /// <param name="tvName"></param>
    public void EnteredTVRadius(string tvName)
    { 
        pubnub.Subscribe()
            .Channels(new List<string>() {
                "streamer." + tvName
            })
            .Execute();     
    }

    /// <summary>
    /// The player has left the specific TV's bounds. Unsubscribe from that tv's spatial presence channel.
    /// </summary>
    /// <param name="tvName"></param>
    public void ExitTVRadius(string tvName)
    {        
        pubnub.Unsubscribe()
            .Channels(new List<string>() {
                "streamer." + tvName
            })
            .Async((result, status) => {
                 if (status.Error)
                 {
                    UnityEngine.Debug.Log(string.Format("Unsubscribe Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                 }
                 else
                 {
                    UnityEngine.Debug.Log(string.Format("DateTime {0}, In Unsubscribe, result: {1}", DateTime.UtcNow, result.Message));
                 }
             });
    }

    /// <summary>
    /// Initial presence request for the channel name. Used for the initial total count, as well as to spawn any mobile experience
    /// users already streaming.
    /// </summary>
    private void InitialPresenceFirstLoad()
    {
        pubnub.HereNow()
            .Channels(new List<string>(){
                credentials.ChannelName
            })
            .IncludeState(true)
            .IncludeUUIDs(true)
            .Async((result, status) => {
                //handle any errors
                if (status.Error)
                {
                    UnityEngine.Debug.Log(string.Format("HereNow Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                }
                //Success
                else
                {
                    //Initial count of players in the game
                    totalCount.text = result.Channels[credentials.ChannelName].Occupants.Count.ToString();
                    //loop through each channel to determine if any gameobjects need to be rendered.                
                    foreach (PNHereNowOccupantData user in result.Channels[credentials.ChannelName].Occupants)
                    {
                        //Only care about non Unity player UUIDs.
                        if (!user.UUID.Equals(pubnub.PNConfig.UserId))
                        {
                            //Mobile Experience Streamers.
                            if (user.UUID.StartsWith("GDC_Stream_"))
                            {
                                //The stream name is the UUID as set in the mobile app.
                                _audience.AddStream(user.UUID);
                            }

                            //debug console player.
                            else if (user.UUID.StartsWith("debug-console"))
                            {
                                GenerateDebugConsolePlayer(user.UUID);
                            }                      
                        }
                    }
                }
            });
    }

    /// <summary>
    /// Generates a debug console player in a random position if one does not exisst
    /// </summary>
    /// <param name="UserId"></param>
    private void GenerateDebugConsolePlayer(string UserId)
    {
        if(!_debugConsolePlayers.Contains(UserId))
        {
            //Grab a random tvPortal gameobject that represents a connected mobile user.
            GameObject tvPortal = _audience.GetRandomStream();

            //if no mobile user has joined the game, set the position close the player.
            if(tvPortal == null)
            {
                tvPortal = _player;
            }
            //Set the player position to be close to that TV Portal.
            //Note that there will be no presence indicators for this 
            Vector3 newPosition = new Vector3(tvPortal.transform.position.x - 2f, tvPortal.transform.position.y, tvPortal.transform.position.z);

            //Create a new Player Prefab near the streamer portal experience.
            GameObject newPlayer = Instantiate(Prefab, newPosition, Quaternion.identity);
            newPlayer.name = UserId;
            newPlayer.SetActive(true);
            _debugConsolePlayers.Add(UserId);
        }
    }

    /// <summary>
    /// Removes the Debug Console Player from the Game if they disconnect.
    /// </summary>
    /// <param name="UserId"></param>
    private void RemoveDebugConsolePlayer(string UserId)
    {
        GameObject debugConsolePlayer = GameObject.Find(UserId);
        Destroy(debugConsolePlayer);
        _debugConsolePlayers.Remove(UserId);
    }
}
