using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UIElements;
using System.Linq;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    private string IncomingMessage;
    public string myID;
    public GameObject PlayerPrefab;
    ///Too confusing must use long names. 
    public List<string> PlayersToSpawnList;
    public List<string> PlayersToDestroyList;
    public List<PlayerCube> PlayersInGameList;
    public float mX, mY, mZ;

    private void Awake()
    {
        myID = "newID";
        mX = 1; mY = 1; mZ = 1;
    }
    // Start is called before the first frame update
    void Start()
    {
      

        udp = new UdpClient();
        
        udp.Connect("18.219.192.76", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("SendStuff", 1, .02f);

    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        CLIENT_REMOVED,
        GET_PLAYERS_IN_GAME,
       
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    
    }

    [Serializable]//This Class is used to send THIS clients cube position to the server, to make a list of all positions.
    public class PositionMessager {public Vector3 position;}
        
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{ public float R, G, B; }
        [Serializable]
        public struct receivedPosition { public float x, y, z; }
        public string id;
        public receivedColor color;
        public receivedPosition position;
    }

    [Serializable]// We need a class to store incoming new players from server
    public class NewPlayer { public Player player;}

    [Serializable]// We also need a list of dropped players so we can destroy them.//correction; now just holding 1 object to feed into a list
    public class DroppedPlayer { public string id; }

    [Serializable]//Lastly we need a list of the players already in the game for new arrivals to spawn.
    public class PlayersAlreadyInGame { public Player[] players;}

    [Serializable]//GameState['players'].append(player) this is the players dictionary on the server.
    public class GameState {public Player[] players;}

    public Message latestMessage; public GameState latestGameState;
    
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        IncomingMessage = "Got this" + returnData;
        //Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    NewPlayerArrived();
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    if (myID == "newID") { myID = p.player.id; }
                    PlayersToSpawnList.Add(p.player.id);
                    break;
                case commands.UPDATE://When we receive this command from server, update all our cubes. 
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    
                    break;
                case commands.CLIENT_REMOVED:// Someone dropped from the server. 
                    DroppedPlayer d = JsonUtility.FromJson<DroppedPlayer>(returnData);//Store that player. 
                    Debug.Log("Player Left The Game:");
                    PlayerRemoved();
                    PlayersToDestroyList.Add(d.id);//Destroy that player by its id.;
                    break;
                case commands.GET_PLAYERS_IN_GAME:
                    //When a new player connects, get/make an object(list) of all players already in the game.
                    PlayersAlreadyInGame pigl = JsonUtility.FromJson<PlayersAlreadyInGame>(returnData);
                    //we need to loop through this list and add them to the spawn list.
                    for (int i = 0; i < pigl.players.Length; i++)
                    {
                        if(pigl.players[i].id != myID)
                        PlayersToSpawnList.Add(pigl.players[i].id);
                    }
                    
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    
    void SpawnPlayersFromList()//Take the list we got from the server to spawn cubes in next function.
    {
        for(int i = 0; i < PlayersToSpawnList.Count; i++)//loop through our spawn list
            {
                InstantiateCubes(PlayersToSpawnList[i]);
            }
        
        PlayersToSpawnList.Clear();//Clear the list when were done.

        PlayersToSpawnList.TrimExcess();
        
    }

    void InstantiateCubes(string id)//Spawn Player.
    {
        GameObject o = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
        o.GetComponent<PlayerCube>().ClientID = id;//add the passed id to its id
        PlayersInGameList.Add(o.GetComponent<PlayerCube>()); //add the cube to our list
        
    }

    void UpdatePlayers()
    {
        //Loop through all the players and update their position from our gamestate.
        for(int i = 0; i < latestGameState.players.Length; i++)
        {
           if(latestGameState.players[i].id != myID)
            {
                for(int j = 0; j < PlayersInGameList.Count; j++)
                {
                    if (PlayersInGameList[j].ClientID == latestGameState.players[i].id)
                    {
                        float XX = latestGameState.players[i].position.x;
                        float YY = latestGameState.players[i].position.y;
                        float ZZ = latestGameState.players[i].position.z;
                        PlayersInGameList[j].transform.position = new Vector3(XX, YY, ZZ);
                    }


                }

            }            
        }
     
    }

    void NewPlayerArrived()
    {
        Debug.Log("New Player Arrived");
        Debug.Log(IncomingMessage);
    }

    void PlayerRemoved()
    {
        Debug.Log(IncomingMessage);
    }

    void DestroyPlayers(string id){
        for(int i = 0; i < PlayersInGameList.Count; i++)
        {
            if(PlayersInGameList[i].ClientID == id)
            {
                PlayersInGameList[i].gameObject.SendMessage("DestroyCube");
                Debug.Log("Attempt to destroy object");
            }
        }
        
    }
    void DestroyFromDroppedList()
    {
        for (int i = 0; i < PlayersToDestroyList.Count; i++)
        {
            DestroyPlayers(PlayersToDestroyList[i]);
        }
        PlayersToDestroyList.Clear();
        PlayersToDestroyList.TrimExcess();
    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void SendStuff()
    {
       PositionMessager m = new PositionMessager();
        
        m.position.x = mX;
        m.position.y = mY;
        m.position.z = mZ;
        Byte[] sendM = Encoding.ASCII.GetBytes(JsonUtility.ToJson(m));
        udp.Send(sendM, sendM.Length);

    }

    void Update(){
        SpawnPlayersFromList();
        DestroyFromDroppedList();
        UpdatePlayers();
    }
}
