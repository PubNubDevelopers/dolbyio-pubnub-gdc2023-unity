using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Linq;
using Dolby.Millicast;


namespace Dolby.Millicast
{
    public class Audience : MonoBehaviour
    {
        //The prefab for the TV model
        public GameObject tvModelPrefab;

        //Positions for the TVs
        public float spawnRadius = 200f;
        public Terrain terrain;
        public Vector3 spawnPosition = Vector3.zero;
        public Vector3 randomPosition = Vector3.zero;
        
        // Keep track of existing TV positions
        private List<Vector3> tvPositions = new List<Vector3>();

        // Dolby.io Credentials
        public string accountId = "ACCOUNT_ID";
        public string publishToken = "PUBLISH_TOKEN";
        public string publish_url = "https://director.millicast.com/api/director/publish";

        //List of streamNames
        private List<string> streamNames = new List<string>();
        //List of TV instances
        private List<GameObject> tvModels = new List<GameObject>();
        //List of McSubscriber components attached to the TVs
        private List<McSubscriber> mcSubscribers = new List<McSubscriber>();

        private void Start(){
            // Find the Terrain object in the scene and assign it to the terrain field
            terrain = GameObject.Find("Terrain").GetComponent<Terrain>();     
        }
        
        /// <summary>
        /// Adds the given stream name to spawn the tv in the game world.
        /// </summary>
        /// <param name="streamName"></param>
        public void AddStream(string streamName)
        {
            // Assign Dolby.io's credentials
            Credentials credentials = CreateCredentials(accountId, publish_url, publishToken);


            // Skip creating a new TV instance for this stream name
            if (!streamNames.Contains(streamName))
            {
                //Create the TV sizes + no overlap
                SpawnTVInstance(tvModelPrefab, terrain, tvPositions, spawnRadius);
                //Fill out the McSubscriber's input
                streamingSetting(tvModelPrefab, spawnPosition, streamName, credentials, tvModels, mcSubscribers);
                // Update the stream name list
                streamNames.Add(streamName);
            }
        }

        /// <summary>
        /// Removes the TV with the old stream nanme no longer in the list.
        /// </summary>
        /// <param name="streamName"></param>
        public void RemoveStream(string streamName)
        {
            // Find the corresponding McSubscriber component
            int index = streamNames.IndexOf(streamName);
            if (index >= 0 && index < mcSubscribers.Count)
            {
                McSubscriber mcSubscriber = mcSubscribers[index];
                if (mcSubscriber != null)
                {
                    // Remove the McSubscriber component from Millicast
                    mcSubscriber.UnSubscribe();

                    // Find the corresponding TV model and destroy it
                    GameObject tvModel = tvModels[index];
                    if (tvModel != null)
                    {
                        tvModels.RemoveAt(index);
                        mcSubscribers.RemoveAt(index);
                        streamNames.RemoveAt(index);
                        Destroy(tvModel);
                    }
                }
            }
        }
        
        // Create a new instance of Credentials using the McCredentials instance
        public Credentials CreateCredentials(string accountId, string publish_url, string publishToken)
        {
            McCredentials mcCredentials = new McCredentials();
            mcCredentials.accountId = accountId;
            mcCredentials.publish_url = publish_url;
            mcCredentials.publish_token = publishToken;
            return new Credentials(mcCredentials, true);
        }

        //Create TV instances in specific positions and not overlapping with the environment
        public void SpawnTVInstance(GameObject tvModelPrefab, Terrain terrain, List<Vector3> tvPositions, float spawnRadius)
        {
            // Check if the newly created TV instance overlaps with existing TV instances
            float minDistance = 1.3f; // Set the minimum distance between TV instances
            bool overlapping = true;

            // Get terrain center and size
            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 terrainCenter = terrain.transform.position + new Vector3(terrainSize.x / 2f, terrainSize.y / 2f, terrainSize.z / 2f);

            // Calculate offset from origin
            Vector3 offsetFromOrigin = -terrainCenter;

            while (overlapping)
            {
                overlapping = false;
                randomPosition = new Vector3(Random.Range(spawnRadius, terrainSize.x - spawnRadius), 0f, Random.Range(spawnRadius, terrainSize.z - spawnRadius));

                // Adjust spawn position using terrain center and offset from origin
                spawnPosition = terrainCenter + randomPosition + offsetFromOrigin;

                // Sample height at the spawn position and adjust the Y coordinate
                float terrainHeight = terrain.SampleHeight(spawnPosition);
                spawnPosition.y = terrainHeight + tvModelPrefab.transform.lossyScale.y / 2f;

                // Check if the new TV instance overlaps with any terrain objects
                Collider[] hitColliders = Physics.OverlapSphere(spawnPosition, tvModelPrefab.transform.lossyScale.x / 2f);
                foreach (Collider collider in hitColliders)
                {
                    if (collider.gameObject.tag != "Terrain" && collider.gameObject.tag != "TV01")
                    {
                        overlapping = true;
                        break;
                    }
                }

                // Check if the new TV instance overlaps with any existing TV instances
                if (!overlapping)
                {
                    foreach (Vector3 tvPos in tvPositions)
                    {
                        if (Vector3.Distance(spawnPosition, tvPos) < minDistance)
                        {
                            overlapping = true;
                            break;
                        }
                    }
                }
            }

            // Add the new TV position to the list of TV positions
            tvPositions.Add(spawnPosition);
        }

        //Ensures each TV instance has all of the information needed in McSubscriber
        public void streamingSetting(GameObject tvModelPrefab, Vector3 spawnPosition, string streamName, Credentials credentials, List<GameObject> tvModels, List<McSubscriber> mcSubscribers)
        {
            // Create a new TV instance
            GameObject tvModel = Instantiate(tvModelPrefab, spawnPosition, Quaternion.identity);
            tvModel.name = streamName; // Used to obtain the specific tv instance for presence updates.
            tvModel.tag = "TV01";

            // Scale the TV to 52.31
            Vector3 scale = Vector3.one * 52.31f;
            tvModel.transform.localScale = scale;

            // Get the McSubscriber component attached to the TV instance
            McSubscriber mcSubscriber = tvModel.AddComponent<McSubscriber>();

            // Set the input stream name for the McSubscriber
            mcSubscriber.streamName = streamName;
            // Adding Credentials
            mcSubscriber.credentials = credentials;
            // Check off Subscribe on Start
            mcSubscriber.Subscribe();

            // Create a new audioSource and make it a child
            GameObject audioSourceObject = new GameObject("AudioSource");
            audioSourceObject.transform.parent = tvModel.transform;
            audioSourceObject.transform.localPosition = Vector3.zero;
            AudioSource audioSource = audioSourceObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = audioSourceObject.AddComponent<AudioSource>();
            }

            // Settings for the audioSource
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 20f;

            // Instantiate a new videoRender material for this TV instance
            Material videoRenderMaterial = new Material(Resources.Load<Material>("videoRender"));

            // Assign the new material to the TV instance's "TV_2:polySurface2" mesh
            foreach (MeshFilter meshFilter in tvModel.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.gameObject.name == "TV_2:polySurface2")
                {
                    MeshRenderer tvMeshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                    tvMeshRenderer.material = videoRenderMaterial;
                }
            }

            //Add a box collider as a trigger to detect when the player enters its radius.
            BoxCollider collider = tvModel.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.2f, 0.2f, 0.2f);
            collider.isTrigger = true; //dont want other objects to bump, but to detect when player has entered the bounds of tv.
            tvModel.AddComponent<TvCollider>(); //Add the script that handles the triggers.
            
            //Add the spatial presence symbol above the tv.
            Vector3 pos = new Vector3(-0.5f, 2.2f, -1.5f);
            GameObject particpant = Instantiate((GameObject)Resources.Load("Participant"), spawnPosition + pos, tvModel.transform.rotation);
            particpant.transform.Rotate(0, 90, 0); // rotate to face the direction of front of screen
            particpant.transform.parent = tvModel.transform;

            // Adding material and audioSource to the mcSubscriber
            mcSubscriber.AddVideoRenderTarget(videoRenderMaterial);
            mcSubscriber.AddRenderAudioSource(audioSource);

            // Add the TV model and McSubscriber to the lists
            tvModels.Add(tvModel);
            mcSubscribers.Add(mcSubscriber);
        }

        /// <summary>
        /// Returns the tv instance that matches the stream name.
        /// </summary>
        /// <param name="streamName"></param>
        /// <returns></returns>
        public GameObject FindStream(string streamName)
        {
            return tvModels.FirstOrDefault(tv => tv.name.Contains(streamName));
        }

        /// <summary>
        /// Return a random tv instance.
        /// </summary>
        /// <returns></returns>
        public GameObject GetRandomStream()
        {
            return tvModels.Count > 0 ? tvModels[Random.Range(0, tvModels.Count - 1)] : null;
        }
    }
}
