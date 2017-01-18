using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Threading;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        string savePath="";
        List<Byte> byteList = new List<byte>();
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token;
        Task task1;
        Task task2;
        [Serializable]
        public class FileDetails
        {
            public string FILETYPE = "";
            public long FILESIZE = 0;
        }
        
        public Form3 fr;
        private static FileDetails fileDet;
        public bool waitForMessage = false;
        public static UdpClient receivingUdpClient;
        private static IPEndPoint RemoteIpEndPoint;
        private static FileStream fs;
        private static Byte[] receiveBytes;


        public Form1()
        {
            InitializeComponent();
            receiveBytes = new Byte[0];
            RemoteIpEndPoint = null;
        }
       
        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void GetFileDetails()
        {
            try
            {
                receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
                MemoryStream stream1 = new MemoryStream();
                stream1.Write(receiveBytes, 0, receiveBytes.Length);
                stream1.Position = 0;
                fileDet = (FileDetails)fileSerializer.Deserialize(stream1);
            }
            catch (Exception eR)
            {
                Console.WriteLine(eR.ToString());
                return;
            }
            
            
            task2 = Task.Run(() => ReceiveFile(), token);
        }

        float steps;
        int count = 0;

        public void ReceiveFile()
        {
            
            byte[] sendB = new byte[1];
            sendB[0] = 0x11;
            
            
            float fileScale = (float)fileDet.FILESIZE / 4096;
            steps = fileScale;
            fileScale /= 100;
            int step=0;
            if(fileScale<1)
            {
                float fils = 0;
                do
                {
                    step++;
                    fils += fileScale;
                } while (fils < 1);
                fileScale = fils;
            }
            else { step = 1; }

            int height = 280;
            this.Invoke((MethodInvoker)delegate {
                this.Height = height; // runs on UI thread
            });
            this.Invoke((MethodInvoker)delegate {
                progressBar1.Step = step; // runs on UI thread
            });
            long operCount=0;
            
            this.Invoke((MethodInvoker)delegate {
                timer1.Enabled = true; // runs on UI thread
            });
            try
            {
                if (savePath != "")
                    fs = new FileStream(savePath  + fileDet.FILETYPE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                else
                    fs = new FileStream(fileDet.FILETYPE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
               
            }
            catch { }
            receivingUdpClient.Send(sendB, 1, RemoteIpEndPoint);
            do
            {
                count++;
                try
                {
                    operCount++;
                    receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    receivingUdpClient.Send(sendB, 1, RemoteIpEndPoint);
                    byteList.AddRange(receiveBytes); 
                    if(operCount>fileScale)
                    {
                        this.Invoke((MethodInvoker)delegate {
                            progressBar1.PerformStep(); // runs on UI thread
                        });
                        operCount = 0;
                    }                
                    
                }
                catch 
                {
                    try
                    {
                        fs.Close();
                    }
                    catch { }
                    receivingUdpClient.Close();
                    this.Invoke((MethodInvoker)delegate {
                        progressBar1.Value = 100; // runs on UI thread
                    });
                    this.Invoke((MethodInvoker)delegate {
                        this.Height = 210; // runs on UI thread
                    });
                    this.Invoke((MethodInvoker)delegate {
                        timer1.Enabled = false; // runs on UI thread
                    });
                    this.Invoke((MethodInvoker)delegate {
                        progressBar1.Value = 0; // runs on UI thread
                    });
                    count = 0;
                    steps = 0;
                    countPerS = 0;
                    timerC = 0;
                    MessageBox.Show("Произошла ошибка соединения");
                    break;
                }  
                if(byteList.Count>= 1048576)
                {
                    fs.Write(byteList.ToArray(), 0, byteList.Count);
                    byteList = new List<byte>();
                }          
            } while (receiveBytes.Length == 4096);
            if (byteList.Count !=0)
            {
                fs.Write(byteList.ToArray(), 0, byteList.Count);
                byteList = new List<byte>();
            }
            this.Invoke((MethodInvoker)delegate {
                progressBar1.Value = 100; // runs on UI thread
            });
            this.Invoke((MethodInvoker)delegate {
                this.Height = 210; // runs on UI thread
            });
            this.Invoke((MethodInvoker)delegate {
                timer1.Enabled = false; // runs on UI thread
            });
            this.Invoke((MethodInvoker)delegate {
                progressBar1.Value=0; // runs on UI thread
            });
            count = 0;
            steps = 0;
            countPerS = 0;
            timerC = 0;
           
            try
            {
                fs.Close();
                receivingUdpClient.Close();
            }
            catch { }
            waitForMessage = false;   
            byteList = new List<byte>();
            string newText = "Ожидание"; // running on worker thread
            this.Invoke((MethodInvoker)delegate {
                labelStatus.Text = newText; // runs on UI thread
            });
        }
        
        private void Form1_Shown(object sender, EventArgs e)
        { 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (fr == null)
            {
                if (receivingUdpClient != null)
                {
                    if (receivingUdpClient.Client == null)
                        receivingUdpClient = new UdpClient(Int32.Parse(labelPort.Text));
                }
                else
                    receivingUdpClient = new UdpClient(Int32.Parse(labelPort.Text));
                token = source.Token;
                waitForMessage = true;
                labelStatus.Text = "Ожидание приема";
                task1 = Task.Run(() => GetFileDetails(), token);
            } else
            {
                MessageBox.Show("Нельзя ожидать прием файла во время попытки отправить файл");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (task1 != null)
            {

                if (task1.Status.ToString() == "Running")
                {
                    receivingUdpClient.Close();
                    waitForMessage = false;
                    labelStatus.Text = "Ожидание";
                }
            }
            fr = new Form3(this);
            fr.Show();           
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!waitForMessage)
            {
                Form2 frm = new Form2(this);
                frm.Show();
            } else { MessageBox.Show("Нельзя сменить порт во время ожидание приема файла"); }
        }

        private void labelPort_TextChanged(object sender, EventArgs e)
        {
            try
            {
                receivingUdpClient.Close();
            }
            catch { }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (task1 != null)
            {
               
                if (task1.Status.ToString() == "Running")
                {
                    receivingUdpClient.Close();
                    waitForMessage = false;
                    labelStatus.Text = "Ожидание";
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            savePath = folderBrowserDialog1.SelectedPath;
            textBox1.Text = savePath;
        }

        int timerC = 0;
        float countPerS = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
           
            timerC++;
            if (timerC != 0 && count == 0)
            {
                
                receivingUdpClient.Close();
            }
            if (timerC%10==0)
            {
                labelP.Text = (timerC / 10).ToString() + " c";
                countPerS = count;
                steps -= count;
                count = 0;
                labelO.Text = (steps / countPerS).ToString() + " c";
            }
        }
    }
}
