using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using System.IO;
using System.Threading;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkMenager : MonoBehaviour {
    const int BUFFERSIZE = 2048;
    const int TIMEOUT = 1000;

    // Start is called before the first frame update
    Socket serverGateway = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    public List<Socket> playerSocket = new List<Socket>();
    public static Queue<Package> sendingQueue = new Queue<Package>();
    private static byte[] result = new byte[BUFFERSIZE];
    bool online;
    string temp = "";
    public static bool gameOver = false;
    public static bool gameStart = false;
    Thread talkSystem;

    public TextMeshProUGUI IPInput;
    public TMP_InputField IPInputObj;
    public TextMeshProUGUI PortInput;
    public TMP_InputField PortInputObj;
    static string IP = "";
    static int PORT = 0;
    //UdpClient udpClient = new UdpClient(PORT*3);
    void Start() {
        gameOver = false;
        gameStart = false;
        Application.runInBackground = true;
        udpClient.EnableBroadcast = true;
        udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        if (IP != "" && PORT != 0) {
            IPInputObj.text = IP + '\0';
            PortInputObj.text = PORT.ToString() + '\0';
            goToOnline();
        }
    }

    // Update is called once per frame
    void Update() {
        listenSocket();
        sendPackage();
    }

    public void goToOnline() {
        if (online)
            return;

        IP = IPInput.text.Substring(0, IPInput.text.Length - 1);
        string portText = PortInput.text;

        int.TryParse(portText.Substring(0, portText.Length - 1),out PORT);
        IPAddress ip = IPAddress.Parse(IP);
        IPEndPoint ipe = new IPEndPoint(ip, PORT);
        IPEndPoint textipe = new IPEndPoint(ip, PORT * 2);

        try {
            serverGateway.Bind(ipe);
            udpClient.Bind(textipe);
            serverGateway.Listen(5);
            online = true;
            talkSystem = new Thread(listenMessage);
            talkSystem.Start();
            IPInputObj.interactable = false;
            PortInputObj.interactable = false;
        } catch (System.Net.Sockets.SocketException sockEx) {
            Debug.Log(sockEx);
            serverGateway.Close();
            online = false;
        }
    }

    void listenSocket() {
        if (!online)
            return;
        try {
            //通過clientSocket接收資料
            List<Socket> readlist = new List<Socket>();
            if(!gameStart) {
                readlist.Add(serverGateway);
                Socket.Select(readlist, null, null, TIMEOUT);
                if (readlist.Count > 0) {
                    var receiveNumber = readlist[0].Accept();
                    Debug.Log(receiveNumber);
                    if (receiveNumber != null) {

                        if (playerSocket.Count < 4) {
                            receiveNumber.ReceiveBufferSize = BUFFERSIZE;
                            int newPlayerID;
                            if (playerSocket.IndexOf(null) == -1) {
                                newPlayerID = playerSocket.Count;
                                playerSocket.Add(receiveNumber);
                            } else {
                                newPlayerID = playerSocket.IndexOf(null);
                                Debug.Log(newPlayerID);
                                playerSocket[newPlayerID] = receiveNumber;
                            }
                            Package pkg = new Package(-1, ACTION.ASSIGN_PLAYER_ID, newPlayerID, 0);
                            string json = JsonUtility.ToJson(pkg) + '#';
                            receiveNumber.Send(Encoding.Unicode.GetBytes(json));
                        }
                    }
                }
                if (playerSocket.Count == 0)
                    return;
            }


            readlist = new List<Socket>(playerSocket);
            readlist.RemoveAll(it => it == null);
            if(readlist.Count == 0) {
                return;
            }
            Socket.Select(readlist, null, null, TIMEOUT);
            for (int i = 0; i < readlist.Count; i++) {
                int receiveNumber = readlist[i].Receive(result);
                if (receiveNumber == 0) {
                    int disPlayer = playerSocket.IndexOf(readlist[i]);
                    Package disPkg = new Package(-1, ACTION.PLAYER_DISCONNECTED, disPlayer, 0);
                    packageUnpacker.pkgQueue.Enqueue(disPkg);
                    readlist[i].Disconnect(false);
                    readlist[i].Close();
                    playerSocket[disPlayer] = null;
                    Array.Clear(result, 0, result.Length);
                    continue;
                }
                
                string json = Encoding.Unicode.GetString(result);
                json = json.Replace("\0", string.Empty);
                Debug.Log("Receive : " + json);
                Array.Clear(result, 0, result.Length);
                for (int k = 0; k < json.Length; k++) {
                    if (json[k] != '#') {
                        temp += json[k];
                    } else {
                        Debug.LogWarning("Package Get" + temp);
                        packageUnpacker.pkgQueue.Enqueue(JsonUtility.FromJson<Package>(temp));
                        
                        temp = "";
                    }
                }
            }

        } catch (System.Net.Sockets.SocketException sockEx) {
            Debug.Log(sockEx);
            online = false;
        }
    }


    void sendPackage() {
        while (sendingQueue.Count > 0) {
            brodcast(sendingQueue.Dequeue());
            break;
        }
        if(sendingQueue.Count == 0 && gameOver) {
            Invoke("ServerRestart", 3);
        }
    }

    void brodcast(Package pkg) {
        for (int i = 0; i < playerSocket.Count; i++) {
            if (playerSocket[i] == null)
                continue;
            string json = JsonUtility.ToJson(pkg) + '#';
            playerSocket[i].Send(Encoding.Unicode.GetBytes(json));
        }
    }

    void listenMessage() {

        //IPEndPoint object will allow us to read datagrams sent from any source.
        EndPoint server = new IPEndPoint(IPAddress.Any, PORT+1) as EndPoint;
        byte[] receiveBytes = new byte[BUFFERSIZE];
        while (true) {
            try {

                
                // Blocks until a message returns on this socket from a remote host.
                int chk = udpClient.ReceiveFrom(receiveBytes, ref server);

                if (chk <= 0)
                    continue;
                string returnData = Encoding.Unicode.GetString(receiveBytes);
                sendMessage(returnData);

                // Uses the IPEndPoint object to determine which of these two hosts responded.
                Console.WriteLine("This is the message you received " +
                                                returnData.ToString());
                Array.Clear(receiveBytes, 0, receiveBytes.Length);

            } catch (Exception e) {
                Debug.Log(e.ToString());
            }

        }



    }
    void sendMessage(string sending) {
        try {
            Byte[] sendBytes = Encoding.Unicode.GetBytes(sending);
            //for (int i=0;i<playerSocket.Count;i++) {
                //if (playerSocket[i] == null)
                    //continue;
                //IPEndPoint target = playerSocket[i].RemoteEndPoint as IPEndPoint;
                IPEndPoint target = new IPEndPoint(IPAddress.Broadcast, PORT + 1);
                //target.Port = PORT + 1;
                udpClient.SendTo(sendBytes, target);
            //}
            Array.Clear(sendBytes, 0, sendBytes.Length);
        } catch (Exception e) {
            Debug.LogError(e.ToString());
        }
    }
    private void OnApplicationQuit() {
        Disconnected();
    }
    private void Disconnected() {
        try {
            if(talkSystem.IsAlive)
                talkSystem.Abort();
            udpClient.Dispose();
        }catch {

        }

        for (int i = 0; i < playerSocket.Count; i++) {
            try {
                playerSocket[i].Dispose();
            } catch {

            }

        }

    }
    public void ServerRestart() {
        Debug.Log("ServerRestart");
        Disconnected();
        SceneManager.LoadScene("SampleScene");
    }
    public void restartButtonClick() {
        IP = "";
        PORT = 0;
        ServerRestart();
    }


}
