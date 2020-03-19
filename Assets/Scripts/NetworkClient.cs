using System;
using System.Text;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine.UI;

public class NetworkClient : MonoBehaviour
{
    public GameObject[] cubes;
    public Text clientStatus;
    public Text messageReceived;
    public UdpNetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort portNumber;

    private DataStreamReader stream;
    private NetworkEvent.Type cmd;
    private List<Vector2> clientPositions;
    private int activeCubes;

    void Start ()
    {
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, portNumber);
        m_Connection = m_Driver.Connect(endpoint);
        clientStatus.text = "Client Status: Offline";
        messageReceived.text = "";
        activeCubes = 0;
        clientPositions = new List<Vector2>();
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        ValidateConnection();
        string inputString = GenerateInputString();

        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                clientStatus.text = "Client Status: Online";
                SendMessage(inputString, m_Driver, m_Connection);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                var receivedString = ReadMessage(stream);
                messageReceived.text = "Server:\n" + receivedString;
                clientPositions = PopulateClientPositions(receivedString);
                ManageCubes();
                SendMessage(inputString, m_Driver, m_Connection);
            }
        }
    }

    void ValidateConnection()
    {
        m_Driver.ScheduleUpdate().Complete();
        if (!m_Connection.IsCreated)
        {
            clientStatus.text = "Client Status: Error";
            return;
        }
    }

    private void SendMessage(string data, UdpNetworkDriver driver, NetworkConnection conn)
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes(data);
        using (var writer = new DataStreamWriter(1024, Allocator.Temp))
        {
            writer.Write(sendBytes, sendBytes.Length);
            conn.Send(driver, writer);
        }
    }

    private string ReadMessage(DataStreamReader stream)
    {
        var readerCtx = default(DataStreamReader.Context);
        var infoBuffer = new byte[stream.Length];
        stream.ReadBytesIntoArray(ref readerCtx, ref infoBuffer, stream.Length);
        return Encoding.ASCII.GetString(infoBuffer);
    }

    private string GenerateInputString()
    {
        string result = "";
        if (Input.GetKey(KeyCode.W))
        {
            result += "1-";
        }
        else
        {
            result += "0-";
        }
        if (Input.GetKey(KeyCode.S))
        {
            result += "1-";
        }
        else
        {
            result += "0-";
        }
        if (Input.GetKey(KeyCode.A))
        {
            result += "1-";
        }
        else
        {
            result += "0-";
        }
        if (Input.GetKey(KeyCode.D))
        {
            result += "1";
        }
        else
        {
            result += "0";
        }
        return result;
    }

    private List<Vector2> PopulateClientPositions(string allPositions)
    {
        List<Vector2> cubePositions = new List<Vector2>();
        string[] positions = allPositions.Split('a');
        for (int i = 0; i < positions.Length; i++)
        {
            string[] currentPos = positions[i].Split('e');
            Debug.Log(currentPos[0] + " " + currentPos[0]);
            cubePositions.Add(new Vector2(float.Parse(currentPos[0]), float.Parse(currentPos[1])));
        }
        return cubePositions;
    }

    private void ManageCubes()
    {
        Debug.Log("Active Cubes: " + activeCubes);
        if (activeCubes > clientPositions.Count)
        {
            DecreaseCubes();
        }
        else if (activeCubes < clientPositions.Count)
        {
            IncreaseCubes();
        }
        else
        {
            PositionCubes();
        }
    }

    private void DecreaseCubes()
    {
        foreach (GameObject cube in cubes)
        {
            if (cube.activeSelf)
            {
                cube.SetActive(false);
                activeCubes--;
                break;
            }
        }
    }

    private void IncreaseCubes()
    {
        foreach (GameObject cube in cubes)
        {
            if (!cube.activeSelf)
            {
                cube.SetActive(true);
                activeCubes++;
                break;
            }
        }
    }

    private void PositionCubes()
    {
        List<GameObject> activeCubes = GetActiveCubes();
        for (int i = 0; i < clientPositions.Count; i++)
        {
            activeCubes[i].transform.position = new Vector3(clientPositions[i].x, clientPositions[i].y, 0);
        }
    }

    private List<GameObject> GetActiveCubes()
    {
        List<GameObject> activeCubes = new List<GameObject>();
        foreach (GameObject cube in cubes)
        {
            if (cube.activeSelf)
            {
                activeCubes.Add(cube);
            }
        }
        return activeCubes;
    }
}