Dolby.io + PubNub: Unity Metaverse Game Experience
==================================================
Welcome to the Dolby.io and PubNub Real-Time Unity Metaverse Game Experience!

This is the Unity demo that allows the player explore the world and interact with users that connect from the [Mobile Social Experience](https://github.com/PubNubDevelopers/pubnub-dolby-gdc2023-mobile-experience) to showcase real-time features. Originally built as a demo for GDC 2023, the player in the Unity game can explore a forest world. Users who join from the mobile experience will have their video livestreamed from their camera into the game. When a user from the mobile experience joins, a television object in the Unity game will spawn that is showcasing their video in real time. When a user from the mobile experience leaves, that television object in the Unity game is destroyed.

The Unity player can also see their own video displayed in the bottom left-hand corner of the screen, send emojis, detect the number of currently active users connected to the game world, and send text-based chat messages.

<p align="middle">
  <img src="/Media/demo.gif"/>
</p>

## Building and Running

While this demo is fully playable and featured, if you would like to expand upon this application yourself to add more real-time functionality and to not have API limits, you will need Dolby.io and PubNub API keys to power the real-time features used in these applications.

#### Get Your Dolby.io Keys
1. Sign in to your [Dolby.io Dashboard](https://dolby.io/signup).
2. Create an application in the Live broadcast section and navigate to the token details section.
3. Save the Publishing token and Account ID from your Dolby.io dashboard application to use later.

#### Get Your PubNub Keys
1. Sign in to your [PubNub Dashboard](https://admin.pubnub.com/). You are now in the Admin Portal.
2. Click on the generated app and then keyset.
3. Enable Presence by clicking on the slider to turn it on. A pop-up will require that you enter in “ENABLE”. Enter in “ENABLE” in all caps and then press the “Enable” button. Enable the "Generate Leave on TCP FIN or RST checkbox", which generates leave events when clients close their connection (used to track occupancy and remove non-connected clients in app). You can leave the rest of the default settings.
5. Click on save changes.
6. Save the Publish and Subscribe Keys.

#### Unity Editor
Install [Unity](https://store.unity.com/download-nuo) if you do not have it. The editor used for this game is 2021.3.10f1.

#### Build & Run

1. Clone the GitHub repository.
	```bash
	git clone https://github.com/PubNubDevelopers/dolbyio-pubnub-gdc2023-unity.git
	```  
2. Open the project in Unity Hub.
3. Under Assets > PubNub Credentials > PubNubCredentials.asset, replace the Publish Key and Subscribe Key fields with the PubNub keys you saved above. You can change the User ID to your liking. However, changing the Channel Name will require you to change this in the [mobile experience app.js file](https://github.com/PubNubDevelopers/pubnub-dolby-gdc2023-mobile-experience/blob/main/app.js), when the ```pubnub.subscribe``` call is made.
4. Under Assets > Resources > Credewntials (Mc Crednetials), replace your Account Id and Publish_token with the Dolby.io information you saved above.
5. If you would like to have your webcam stream your video in the bottom left corner of the screen, or stream your in-game experience to users in the web, make note of the MainCamera and Creator's POVs' Stream Names in the Hierarchy.
6. Open your [Dolby.io Dashboard](https://dolby.io/signup) in a browser. Click the Broadcast button to open a new tab that is showing your video.
7. Replace the parameter in the URL for the stream name with the Creator's POV Stream Name (default is ```webCamUser```). Refresh the page to ensure it is showing the correct stream name. With the stream name as ```webCamUser```, the parameter will be replaced in the URL as so:
<p align="middle">
  <img src="/Media/streamname.png"/>
</p>
8. Run the Game in the Editor.
9. Move with WASD, left + shift to sprint, space bar to jump. Send emojis by clicking on emoji wheel and select the emoji to display in the world.
10. Follow the [instructions for mobile experience](https://github.com/PubNubDevelopers/pubnub-dolby-gdc2023-mobile-experience/blob/main/README.md) to have other users join/leave experience to spawn/destroy television objects in game world. View the presence indicator when new users join/leave to see updates in real time.
11. Send and view chat by hitting the Enter Key. Type a message, and press enter to send again.

## Architecture of Mobile + Unity Experience
<p align="middle">
  <img src="/Media/architecture.png"/>
</p>

There is a server that provides the mobile experience the Dolby.io publish token and account id necessary to connect to Dolby.io’s network. The server also provides the mobile user the PubNub Publish/Subscribe keys necessary to connect to the PubNub Network via an object.
Once this object is created, users connect to the PubNub network using a subscribe call and by providing a channel, which is the mechanism through which the data is transmitted from one device to another.

When the Unity game starts, it creates a PubNub object and connects to the network via a subscribe call that is listening to the same channel name as the mobile experience (using a wildcard subscribe), which allows the Unity experience to listen for hierarchical list of channel names. Event listeners allow users to then catch any new mobile connections, since subscribe events, with a setting enabled, generates what are called Presence events.
The basic Presence events are sent across the network when the joins, leaves, or suddenly quits from the network.

The Unity game then creates the streamer TV game object that pulls in the live video from the mobile users by connecting to the Dolby.io network.
The presence events are also used to determine how many active players are in the game at a time. The chat messages listen for any message events generated when a user publishes a message to a specified channel whenever a message is sent. The ID of the sender, as well as their corresponding message is displayed on the screen. The debug console players (little red objects) when a debug console player connects to the network (channel name starts with “debug-console”). The Unity experience catches these presence events, and generates the game objects based on the channel name.

## Links
- Mobile Experience Demo: https://developer.dolby.io/demos/GDC-demo-experience/
- Mobile Experience Repo: https://github.com/PubNubDevelopers/pubnub-dolby-gdc2023-mobile-experience
- Dolby.io Documentation: https://docs.dolby.io/
- PubNub Documentation: https://www.pubnub.com/docs/

## License
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
