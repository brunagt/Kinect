using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TCPServer
{
    class TestClass
    {
        static void Main(string[] args)
        {
            Server TCPServer = new Server();
            // Display the number of command line arguments:
            //System.Console.WriteLine(args.Length);
        }
    }


    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 3200);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
            System.Console.WriteLine("Listening...");


        }

        //starts the tcp listener and accept connections
        private void ListenForClients()
        {
            this.tcpListener.Start();
           // System.Console.WriteLine("Listening...");

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();
                System.Console.WriteLine("Client connected");


                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }


        //Read the data from the client
        private void HandleClientComm(object client)
        {

            TcpClient tcpClient = (TcpClient)client; //start the client
            NetworkStream clientStream = tcpClient.GetStream(); //get the stream of data for network access

            byte[] message = new byte[4096];
            int bytesRead;

            while (true) 
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0) //if we receive 0 bytes
                {
                    //the client has disconnected from the server 
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
               
               // System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));
                System.Console.WriteLine(encoder.GetString(message, 0, bytesRead));

                //Reply
                byte[] buffer = encoder.GetBytes("Hello Android!");
                clientStream.Write(buffer, 0, buffer.Length);
                //System.Console.WriteLine("Kinect: " + "Hello Android!");

                System.Console.WriteLine("Client disconnected");
                clientStream.Flush();
            }

            tcpClient.Close();
        }


    }
}


/* store the message
public delegate void MessageReceivedHandler(string message);
public event MessageReceivedHandler MessageReceived;

...

//message has successfully been received
ASCIIEncoding encoder = new ASCIIEncoding();
string message = encoder.GetString(message, 0, bytesRead);
if(this.MessageReceived != null)
  this.MessageReceived(message);
*/