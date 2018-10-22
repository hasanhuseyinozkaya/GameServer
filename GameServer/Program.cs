using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using ClassContainer;
using System.Data;
using Game.Helpers;

// State object for reading client data asynchronously  

    
public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class ConnectionPool
{
    public StateObject State { get; set; }
    public Socket Handler { get; set; }
    public string UserID { get; set; }
    public string UserName { get; set; }
}

[Serializable]
public class SendMeToTheServer
{
    public string Name { get; set; }
    public int speed { get; set; }

}
public class AsynchronousSocketListener
{

   public static  List<ConnectionPool> connectionPoolIns = new List<ConnectionPool>();
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    public AsynchronousSocketListener()
    {
    }

    public static void StartListening()
    {
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();

    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        Console.WriteLine("Connection Made");
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);
        
        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;

 

        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.   
        int bytesRead = handler.EndReceive(ar);

        SeriObj send = (SeriObj)GameObjectDeSerializer.ByteArrayToObject(state.buffer);


        ConnectionPool connectionPool = new ConnectionPool();
        connectionPool.State = state;
        connectionPool.Handler = handler;
        connectionPool.UserName = send.UserName;


        Console.WriteLine(send.UserName + " Connected To Server");
        var isUserAlreadyConnected = AsynchronousSocketListener.connectionPoolIns.Find(t => t.UserName == send.UserName);
        if (isUserAlreadyConnected != null)
            AsynchronousSocketListener.connectionPoolIns.Remove(isUserAlreadyConnected);

        AsynchronousSocketListener.connectionPoolIns.Add(connectionPool);
        Console.WriteLine("Total Connection Count " + AsynchronousSocketListener.connectionPoolIns.Count);

        for (int i = 0; i < AsynchronousSocketListener.connectionPoolIns.Count; i++)
        {
            var connectedUsers = AsynchronousSocketListener.connectionPoolIns[i];
            Send(connectedUsers.Handler, send.UserName + " Connected To Server", connectedUsers.UserName);

        } 

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();
            //if (content.IndexOf("<EOF>") > -1)
            //{
                // All the data has been read from the   
                // client. Display it on the console.  
                //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                //    content.Length, content);
                // Echo the data back to the client.  
              //  Send(handler, "received");
            //}
            //else
            //{
            //    // Not all data received. Get more.  
            //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //    new AsyncCallback(ReadCallback), state);
            //}
        }
    }

    private static void Send(Socket handler, String data,string username)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        Console.WriteLine(handler.Connected);
        if(SocketConnected(handler))
        {
            handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
        }
    }
   static bool SocketConnected(Socket s)
    {
        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);
        if (part1 && part2)
            return false;
        else
            return true;
    }
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

         //   handler.Shutdown(SocketShutdown.Both);
          //  handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {
        StartListening();
        return 0;
    }
}