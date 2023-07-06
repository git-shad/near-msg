using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace near_msg
{

    public partial class MainWindow : Window
    {
        private delegate void anon();
        private string myIp = null;
        private bool ConnectionStop = false;
        private int loopspeed = 100;
        private static Thread letThread(anon tr)
        {
            var thread = new Thread(new ThreadStart(tr));
            thread.Start();
            return thread;
        }

        private Thread ServerTCPConnect()
        {
            return letThread(() =>
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(IPAddress.Any, 8080);
                    listener.Start();
                    
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.Message);
                    return;
                }

                for (; ; )
                {
                    TcpClient client = null;
                    NetworkStream stream = null;
                    try
                    {
                        var stopwhen = 200;//loops
                        while(true){
                            if(ConnectionStop){
                                stopwhen = 0;
                                ConnectionStop = false;
                            }
                            --stopwhen;
                            Console.WriteLine("wait connection");
                            if (listener.Pending())
                            {
                                client = listener.AcceptTcpClient();
                                break;
                            }
                            else {
                                if(stopwhen <= 0){
                                    Console.WriteLine("listening stop!");
                                    Dispatcher.Invoke(() => {
                                        receiveisclick = true;
                                        receive.Stroke = Brushes.Black;
                                        blur1.Radius = 4;
                                    });
                                    listener.Stop();
                                    return;
                                }
                                Thread.Sleep(100);
                            }
                        }
                        
                        if (client.Connected)
                        {
                            //----------------------------------------------------------
                            stream = client.GetStream();
                            var buffer = Encoding.UTF8.GetBytes(myIp);
                            stream.Write(buffer, 0, buffer.Length);
                            buffer = new byte[15];
                            stream.Read(buffer, 0, buffer.Length);

                            Dispatcher.Invoke(() =>
                            {
                                connectedIP.Text = Encoding.UTF8.GetString(buffer);
                            });
                            //----------------------------------------------------------

                            do {
                                stream = client.GetStream();
                                
                                //handle for closing network connection
                                if (ConnectionStop)
                                {
                                    buffer = Encoding.UTF8.GetBytes("--=stopNetwork=--");
                                    stream.Write(buffer, 0, buffer.Length);
                                    stream.Close();
                                    client.Close();
                                    listener.Stop();
                                    ConnectionStop = false;
                                    Console.WriteLine("server is stop");
                                    Dispatcher.Invoke(() => {
                                        ifConnect.Text = "Disconnect";
                                    });
                                    return;
                                }
                                
                                //end

                                Dispatcher.Invoke(() => { ifConnect.Text = "Connected"; });

                                if (stream.DataAvailable)
                                {
                                    Console.WriteLine("receive data from client");
                                    buffer = new byte[client.ReceiveBufferSize];
                                    stream.Read(buffer, 0, buffer.Length);
                                    Dispatcher.Invoke(() =>
                                    {
                                        msg.Text = Encoding.UTF8.GetString(buffer).Trim().Replace(((char)0).ToString(), "");
                                        
                                    });
                                    Array.Clear(buffer, 0, buffer.Length);
                                    stream.Flush();

                                }
                                
                                Dispatcher.Invoke(() =>
                                {
                                    if(msg.Text.Length > 0 && gosend){
                                        buffer = Encoding.UTF8.GetBytes(msg.Text);
                                        stream.Write(buffer,0,buffer.Length);
                                        Array.Clear(buffer, 0, buffer.Length);
                                        stream.Flush();
                                        gosend = false;
                                    }
                                });
                                Thread.Sleep(loopspeed);
                            } while (true);
                            
                        }
                    }
                    catch (Exception se)
                    {
                        Console.WriteLine(se.Message);
                        stream.Close();
                        client.Close();
                    }
                }

            });
        }

        private void ClientTCPConnect(){
            letThread(() => {
                TcpClient client = null;
                var nearIp = "no action";

                try {
                    
                    Dispatcher.Invoke(() => {
                        nearIp = connectedIP.Text;
                    });
                    client = new TcpClient(nearIp, 8080);
                    
                    
                }catch(SocketException se){
                    try { client.Close(); }
                    catch (NullReferenceException nre) {
                        Console.WriteLine(nre.Message);
                    }
                    return;
                }
                
                NetworkStream stream = null;
                if(client.Connected){try {

                    Dispatcher.Invoke(() =>
                    {//receive and send IP
                        nearIp = connectedIP.Text;
                     });
                    stream = client.GetStream();
                    var buffer = new byte[15];
                    stream.Read(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(nearIp);
                    stream.Write(buffer, 0, buffer.Length);

                    do{
                        Dispatcher.Invoke(() => { ifConnect.Text = "Connected"; });
                        stream = client.GetStream();

                        if (stream.DataAvailable )
                        {
                            Console.WriteLine("receive data from server.");
                            buffer = new byte[client.ReceiveBufferSize];
                            stream.Read(buffer, 0, buffer.Length);
                            var read = Encoding.UTF8.GetString(buffer).Trim().Replace(((char)0).ToString(), "");

                            if (read.Contains("--=stopNetwork=--"))
                            {
                                Dispatcher.Invoke(() => { ifConnect.Text = "Disconnect"; });
                                stream.Close();
                                client.Close();
                                Console.WriteLine("client is stop!");
                                return;
                            }
                            Dispatcher.Invoke(() => {  msg.Text = read; });
                            Array.Clear(buffer,0,buffer.Length);
                            stream.Flush();
                        }

                        Dispatcher.Invoke(() =>
                        {
                            if (msg.Text.Length > 0 && gosend)
                            {
                                buffer = Encoding.UTF8.GetBytes(msg.Text);
                                stream.Write(buffer, 0, buffer.Length);
                                Array.Clear(buffer, 0, buffer.Length);
                                stream.Flush();
                                gosend = false;
                            }
                        });
                        Thread.Sleep(loopspeed);
                    }while(true);
                }catch (Exception se) {
                    Console.WriteLine(se.Message);
                }
                }
            });
        }

        public MainWindow()
        {
            InitializeComponent();
            var pos = new Point(20, 20);
            Left = pos.X;
            Top = pos.Y;
            (new Thread(new ThreadStart(() =>
            {
                try
                {
                    var text = "IP Address: ";
                    //repeat generate if IP address change
                    while (true)
                    {
                        Thread.Sleep(50);
                        var IPlist = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

                        Dispatcher.Invoke(() =>
                        { 
                            myIp = (IPlist[IPlist.Length - 1]).ToString();
                            myHostIP.Text = text + myIp; 
                        });

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }))).Start();
            ifConnect.Text = "Disconnect";
            


        }

        private void canMove(object sender, MouseButtonEventArgs e)
        {

            try
            {
                base.OnMouseLeftButtonDown(e);
                this.DragMove();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        //this section is for exit method
        private void exit(object sender, MouseButtonEventArgs e)
        {
            receiveisclick = false;
            receiveButton(null,null);
            this.IsEnabled = true;
            Environment.Exit(0);
        }

        private void MouseEnter_exit(object sender, MouseEventArgs e)
        {
            exit_ani.Stroke = Brushes.Black;
        }

        private void MouseLeave_exit(object sender, MouseEventArgs e)
        {
            exit_ani.Stroke = null;
        }//end

        private bool gosend = false;
        private bool isClientStart = false;
        private void sendButton(object sender, MouseButtonEventArgs e)
        {
            send.Stroke = Brushes.Blue;
            blur0.Radius = 2;
            gosend = true;
            letThread(() =>
            {
                Thread.Sleep(500);
                Dispatcher.Invoke(() =>
                {
                    send.Stroke = Brushes.Black;
                    blur0.Radius = 4;
                });
            });
            if (!isClientStart || ifConnect.Text.Contains("Disconnect"))
            {
                Console.WriteLine("try to connect!");
                isClientStart = true;
                ClientTCPConnect();
            }

        }

        private void MouseLeave_send(object sender, MouseEventArgs e)
        {
            send.Stroke = Brushes.Black;
            blur0.Radius = 4;
        }

        bool receiveisclick = true;
        private void receiveButton(object sender, MouseButtonEventArgs e)
        {
            if (receiveisclick)
            {
                receiveisclick = false;
                receive.Stroke = Brushes.Blue;
                blur1.Radius = 2;
                ServerTCPConnect();
            }
            else
            {
                receiveisclick = true;
                receive.Stroke = Brushes.Black;
                blur1.Radius = 4;

                ifConnect.Text = "Disconnect";
                ConnectionStop = true;
            }
            
        }

        private void msg_key(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter){
                msg.Text += "\n";
            }
        }

        
    }
}
