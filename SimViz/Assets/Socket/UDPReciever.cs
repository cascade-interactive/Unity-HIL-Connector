using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    public int listenPort = 5001;
    public ConcurrentQueue<byte[]> PacketQueue { get; private set; } = new ConcurrentQueue<byte[]>();

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning;

    void Start()
    {
        isRunning = true;
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"UDP Receiver listening on port {listenPort}");
    }

    private void ReceiveData()
    {
        udpClient = new UdpClient(listenPort);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, listenPort);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyIP);
                if (data.Length > 0)
                {
                    PacketQueue.Enqueue(data);
                }
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"UDP Receive Error: {e.Message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        udpClient?.Close();
        receiveThread?.Abort();
    }
}