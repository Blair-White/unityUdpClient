using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UIElements;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    private string IncomingMessage;
    public string myID;
    
    // Start is called before the first frame update
    void Start()
    {
        myID = "newID";
        udp = new UdpClient();
        
        udp.Connect("18.219.192.76", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        CLIENT_REMOVED
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;        
    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    [Serializable]
    public class PlayerID
    {
        public int ID;
    }
    public Message latestMessage;
    public GameState latestGameState;
    
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
                    break;
                case commands.UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    if(myID == "newID")
                    { myID = latestGameState.players[latestGameState.players.Length-1].id; }
                    break;
                case commands.CLIENT_REMOVED:
                    Debug.Log("Player Left The Game:");
                    PlayerRemoved();
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

    void SpawnPlayer(){

    }

    void UpdatePlayers(){
     
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

    void DestroyPlayers(){

    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayer();
        UpdatePlayers();
        DestroyPlayers();
    }
}
