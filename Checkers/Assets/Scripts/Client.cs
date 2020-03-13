﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    public GameManager gameManager;
    public string clientName;
    public bool isHost;
    private static readonly int portNumber = 6007;
    private static string serverAddress = "157.55.186.240";
    private Thread clientReceiveThread;
    private bool socketReady;
    private Socket sender;
    private static byte[] buffer = new byte[256];


    enum GameHeaders
    {
        USER = 0,
        MOVE = 1,
        ENDT = 2,
        CHAT = 3
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    // connect to the game server
    public bool ConnectToServer()
    {
        if (socketReady) return false;

        // create IPAddress object from ip address
        if (!IPAddress.TryParse(serverAddress, out var serverIp))
        {
            Debug.LogError("Invalid IP Address");
        }

        try
        {
            CreateSocket(serverIp);
            clientReceiveThread = new Thread(() => SendNWait((int)GameHeaders.USER, clientName));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();            
        }
        catch (Exception e)
        {
            Debug.LogError("On client connect: " + e.Message);
        }

        return socketReady;
    }
    private void CreateSocket(IPAddress serverIp)
    {
        try
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPEndPoint localEndPoint = new IPEndPoint(serverIp, portNumber);

            // Creation TCP/IP Socket using  
            // Socket Class Costructor 
            sender = new Socket(serverIp.AddressFamily,
                       SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Connect Socket to the remote  
                // endpoint using method Connect() 
                sender.Connect(localEndPoint);

                // We print EndPoint information  
                // that we are connected 
                Debug.Log("Socket connected to -> " +
                              sender.RemoteEndPoint.ToString());

                // Send data to server
                // USER|xxx
                socketReady = true;
            }

            catch (Exception e)
            {
                Debug.Log("Unexpected exception : " + e.ToString());
                // Send END to server
                // Send((int)GameHeaders.ENDT, "bye");
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException : " + e.ToString());
        }

    }

    public void SendNWait(int header, string data)
    {
        Send(header, data);
        // Data buffer 
        byte[] messageReceived = new byte[1024];

        // Wait for data from server
        int byteRecv = sender.Receive(messageReceived);
        if (byteRecv > 0)
        {
            OnIncomingData(Encoding.ASCII.GetString(messageReceived));
        }

    }
    // check socket to server for messages on every frame 
    void Update()
    {

    }

    // Send messages to the server
    public void Send(int header, string data)
    {
        if (!socketReady) return;

        string message = "";

        switch (header)
        {
            case (int)GameHeaders.USER:
                message = "USER|";
                break;
            case (int)GameHeaders.MOVE:
                message = "MOVE|";
                break;
            case (int)GameHeaders.ENDT:
                message = "ENDT|";
                break;
            case (int)GameHeaders.CHAT:
                message = "CHAT|";
                break;
        }
        message += data;
        // Creation of message that 
        // we will send to Server 
        byte[] messageSent = Encoding.ASCII.GetBytes(message);
        Debug.Log("Sent: " + message);
        int byteSent = sender.Send(messageSent);
    }

    // Read messages from the server
    private void OnIncomingData(string data)
    {
        Debug.Log("Client: " + data);
        string[] aData = data.Split('|');

        switch (aData[0])
        {
            case "START":
                gameManager.isWhite = aData[1].Contains("WHITE");
                gameManager.oponentUsername = aData[2];
                Debug.Log("START, oponent name: " + gameManager.oponentUsername + "Is White = " + gameManager.isWhite);
                gameManager.startMatch = true;

                break;
            case "MOVE":
                Debug.Log("MOVE");
                // move pieces
                break;
            case "ENDT":
                Debug.Log("ENDT");
                // end turn
                break;
            case "CHAT":
                Debug.Log("CHAT");
                // update chat log
                break;
            default:
                Debug.LogError("Received a header outside of range");
                break;
        }
    }

    // close socket on each instance of the game closing or a user quiting
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
    private void OnDisable()
    {
        CloseSocket();
    }
    private void CloseSocket()
    {
        if (!socketReady) return;

        clientReceiveThread.Abort();
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();
        socketReady = false;
    }
}
