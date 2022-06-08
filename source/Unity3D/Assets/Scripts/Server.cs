using UnityEngine;
using System.Collections;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System;
using System.Text;

public class Server : MonoBehaviour
{
	private bool mRunning;
	public static string msg = "";

	public Thread mThread;
	public TcpListener tcp_Listener = null;
	public TcpClient client;
	public NetworkStream stream;
	public static string returnValue = "";
	public static bool readyToUser = false;
	public static int frame = 0;
	public static string fdata = "";
	private int state = 0;
	public static bool serverReady = false;
	public RSAMode rsa = new RSAMode();
	static UTF8Encoding ByteConv = new UTF8Encoding();
	void Awake ()
	{
		mRunning = true;
		ThreadStart ts = new ThreadStart (Receive);
		mThread = new Thread (ts);
		mThread.Start ();
		print ("Thread done...");
	}

	void Start ()
	{
		mRunning = true;
	}

	public void stopListening ()
	{
		mRunning = false;
	}


	void Receive ()
	{  
		if (mRunning) {
			tcp_Listener = null;  
			try {
				tcp_Listener = new TcpListener (IPAddress.Parse ("127.0.0.1"), 9999);
				tcp_Listener.Start ();

				byte[] bytes = new byte[2048];
				byte[] msg1 = new byte[2048];
				string data = null;
				string str = null;
				state = 0;
				// Debug.Log("Waiting for a connection... ");
				while (true) {
					if (!KinectManager.AuToken) {
						Thread.Sleep (1);

						if (state == 0) {
							Debug.Log ("Waiting for a connection... ");
							client = tcp_Listener.AcceptTcpClient ();
							if(client.Connected){
								Debug.Log ("Connected!");
								Socket c = client.Client;
								try{
									FileStream fs = new FileStream ("whitelist.txt", FileMode.Open);
									StreamReader sr = new StreamReader (fs);
									string WhiteList;
									IPEndPoint ip_point = (IPEndPoint)c.RemoteEndPoint;
									string ipadress = ip_point.Address.ToString ();
									Debug.Log ("Authentifying");
									while ((WhiteList = sr.ReadLine ()) != null) {
										Debug.Log (WhiteList + "|||||||||" + ipadress);
										if (ipadress == WhiteList) {
											//Debug.Log ("Connected!");
											data = null;
											stream = client.GetStream ();
											Debug.Log ("Complete");
											break;
										}else{
											Debug.Log ("Done");
										}
									}
									fs.Close();
									sr.Close();
									if(WhiteList==null){
										Debug.Log ("Disconnected");
										c.Shutdown(SocketShutdown.Both);
										c.Disconnect(true);
									}
								}catch(Exception e){
									Debug.Log (e);
									c.Shutdown(SocketShutdown.Both);
									c.Disconnect(true);
								}
							}
						}
						int i;

						while (state == 0 && ((i = stream.Read (bytes, 0, bytes.Length)) != 0)) {
							byte[] decBytes = rsa.RSADecryption(bytes);
							str = System.Text.Encoding.UTF8.GetString (decBytes);                          
							if (str [0] == 'R') {
								Debug.Log ("Ready");
								//string Whitelist=System.IO.File.ReadAllText("whitelist.txt");

								readyToUser = true;
								state++;
								frame = 0;
								fdata = "";
							}
						}

						while (state == 1) {
							//Debug.Log ("");
							if (fdata.Length > 0 && frame == 5) {
								returnValue = "";
								//Debug.Log("데이터 줌" +"::::");
								//timePLz=true;
								returnValue = "";
								Debug.Log ("Frame" + frame + "::::Buffer" + fdata);
								//msg1 = System.Text.Encoding.UTF8.GetBytes(fdata);
								string deBugSTR = "0.6796593,0.7412902,0.8305915,0.6896438,0.7349544,0.8929448,0.6217898,0.8053113,0.8942865,0.4965235,0.8482569,0.8974352,0.3263746,0.6376214,0.8653297,0.3277444,0.7131414,0.8872406,0.3806789,0.80237,0.8873248,0.5632311,0.8121269,0.8280618,0.6838039,0.7581038,0.8441817,0.5756704,0.7844499,0.8772598,0.4969064,0.848163,0.8971123,0.3226864,0.6347477,0.8684753,0.328193,0.7109078,0.8889313,0.381863,0.8020788,0.8877149,0.4433437,0.7996083,0.8266807,0.6223586,0.7638255,0.8303609,0.6237358,0.8134101,0.8797389,0.5085399,0.8557188,0.8984311,0.3229521,0.6325173,0.8703964,0.3306565,0.7127049,0.8926477,0.3840765,0.803398,0.8933172,0.6826693,0.7087148,0.8314139,0.723768,0.7388116,0.885611,0.625351,0.8039635,0.8944617,0.5052989,0.848197,0.9001153,0.3263156,0.6353663,0.8680958,0.3303797,0.7119336,0.8898092,0.3875072,0.8034071,0.8909258,0.6826693,0.7087148,0.8314139,0.711146,0.7384147,0.8996086,0.6254948,0.8040758,0.8948542,0.5048166,0.8481519,0.8999347,0.3264563,0.6355041,0.8675796,0.330312,0.711849,0.889379,0.3873426,0.8032313,0.8905979";
								msg1 = System.Text.Encoding.UTF8.GetBytes (deBugSTR);
								byte[] encMSG = rsa.RSAEncryption(msg1);    //데이터 암호화                                
								stream.Write (encMSG, 0, encMSG.Length);    //암호화된 데이터 전송

								stream.Flush ();
								serverReady = true;
								state = 2;

								msg1 = System.Text.Encoding.UTF8.GetBytes ("");
							}
							frame = 0;
							fdata = "";

						}

						while (state == 2 && ((i = stream.Read (bytes, 0, bytes.Length)) != 0)) {
							//Debug.Log("데이터 받음" +"::::");
							//timePLz=true;
							byte[] decBytes = rsa.RSADecryption(bytes);
							str = System.Text.Encoding.UTF8.GetString (decBytes);

							returnValue = str;
							state = 1;
							serverReady = false;
						}
						Array.Clear (bytes, 0, bytes.Length);
					}
				}
			} catch (SocketException e) {
				Debug.Log ("SocketException:" + e);
			} finally {
				stopListening ();
				mThread.Join (500);
				tcp_Listener.Stop ();
			}
		}
	}

	void Update ()
	{

	}

	void OnApplicationQuit ()
	{ // stop listening thread
		stopListening ();// wait for listening thread to terminate (max. 500ms)
		mThread.Join (500);
		tcp_Listener.Stop ();
	}
}