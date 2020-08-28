using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
    public delegate void PacketHandler(int fromClient, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static void Start(int maxPlayers, int port) {
        MaxPlayers = maxPlayers;
        Port = port;

        Debug.Log("Starting Server...");
        initServerData();

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on port: {Port}");
    }

    private static void TCPConnectCallback(IAsyncResult result) {
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");
        for(int i = 1; i <= MaxPlayers; i++) {
            if(clients[i].tcp.socket == null) {
                clients[i].tcp.Connect(client);
                return;
            }
        }

        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect: Server is full");
    }

    private static void UDPReceiveCallback(IAsyncResult result) {
        try {
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndpoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if(data.Length < 4) {
                return;
            }

            using(Packet packet = new Packet(data)) {
                int clientId = packet.ReadInt();
                if(clientId == 0) {
                    return;
                }

                // New Client
                if(clients[clientId].udp.endPoint == null) {
                    clients[clientId].udp.Connect(clientEndpoint);
                    return;
                }
                if(clients[clientId].udp.endPoint.ToString() == clientEndpoint.ToString()) {
                    clients[clientId].udp.HandleData(packet);
                }
            }
        }
        catch(Exception ex) {
            Debug.Log($"Error receiving UDP Data: {ex.Message}");
        }
    }
    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet) {
        try {
            if(clientEndPoint != null) {
                udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
            }
        }
        catch(Exception ex) {
            Debug.Log($"Error sending UDP data to client: {clientEndPoint.ToString()} {ex.Message}");
        }
    }
    private static void initServerData() {

        for(int i = 1; i <= MaxPlayers; i++) {
            //TODO: Dynamically add to server list until max players is reached
            //TODO: Create unique ids per client 
            clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>() {
                { (int) ClientPackets.welcomeReceived,  ServerHandle.WelcomeReceived },
                { (int) ClientPackets.playerMovement, ServerHandle.PlayerMovement },
            };

        Debug.Log("Initialized Packets.");
    }
}
