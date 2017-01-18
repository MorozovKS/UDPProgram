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
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;

namespace WindowsFormsApplication2
{
    public partial class Form3 : Form
    {
        [Serializable]
        public class FileDetails
        {
            public string FILETYPE = "";
            public long FILESIZE = 0;
        }
        public UdpClient sende;
        private static FileDetails fileDet;
        Task task1;
        Task task2;
        private IPAddress remoteIPAddress;
        private int remotePort;
        private IPEndPoint RemoteIpEndPoint=null;
        private static IPEndPoint endPoint;
        private static FileStream fs;
        Form1 form1;

        public Form3(Form1 fr)
        {
            InitializeComponent();
            form1 = fr;
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        [STAThread]
        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                remoteIPAddress = IPAddress.Parse(textBoxIP.Text);
            }
            catch
            {
                MessageBox.Show("Неправильно введен IP");
                return;
            }
            try
            {
                if(int.Parse(textBoxPort.Text)<= 65535)
                    remotePort = int.Parse(textBoxPort.Text);
                else 
                    throw new Exception();
            } catch
            {
                MessageBox.Show("Номер порта не более 65535");
                return;
            }
            try
            {
                fs = new FileStream(textBoxDir.Text, FileMode.Open, FileAccess.Read);
            }
            catch
            {
                MessageBox.Show("Неправильный путь к файлу");
                return;
            }


            sende = new UdpClient(int.Parse(form1.labelPort.Text));
            fileDet = new FileDetails();
            endPoint = new IPEndPoint(remoteIPAddress, remotePort);
            task1 = Task.Run(() => SendFileInfo());     
 
        }

        public void SendFileInfo()
        {
            try
            {
                fileDet.FILETYPE = Path.GetFileName(fs.Name);
                fileDet.FILESIZE = fs.Length;

                XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
                MemoryStream stream = new MemoryStream();
                fileSerializer.Serialize(stream, fileDet);
                stream.Position = 0;
                Byte[] bytes = new Byte[stream.Length];
                stream.Read(bytes, 0, Convert.ToInt32(stream.Length));
                sende.Send(bytes, bytes.Length, endPoint);
                stream.Close();
                byte[] takeB = new byte[1];
                
               // MessageBox.Show("Ответ принят");
                task2 = Task.Run(() => SendFile());
            }
            catch
            {
                MessageBox.Show("Невозможно подключиться\nПроверьте введенные данные");
                fs.Close();
                sende.Close();
            }
        }

        float steps;
        int count = 0;
        int timerC = 0;
        float countPerS = 0;



        private void SendFile()
        {
            float fileScale = fs.Length / 4096;
            steps = fileScale;
            fileScale /= 100;
            int step = 0;
            if (fileScale < 1)
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

            this.Invoke((MethodInvoker)delegate {
                this.Height = 320; // runs on UI thread
            });
            this.Invoke((MethodInvoker)delegate {
                progressBar1.Step = step; // runs on UI thread
            });

            long operCount = 0;
            this.Invoke((MethodInvoker)delegate {
                timer1.Enabled = true; // runs on UI thread
            });


            try
            {
               
                long position = 0;
                byte[] bytes = new byte[4096];
                byte[] takeB = new byte[1];
                sende.Close();
                sende = new UdpClient(int.Parse(form1.labelPort.Text));
                takeB = sende.Receive(ref RemoteIpEndPoint);

                do
                {
                    count++;
                    fs.Read(bytes, 0, bytes.Length);
                    try
                    {
                        sende.Send(bytes, bytes.Length, endPoint);
                    }
                    catch
                    {
                        fs.Close();
                        sende.Close();
                    }
                    operCount++;
                    position += 4096;
                    if (operCount > fileScale)
                    {
                        this.Invoke((MethodInvoker)delegate {
                            progressBar1.PerformStep(); // runs on UI thread
                        });
                        operCount = 0;
                    }
                    takeB = sende.Receive(ref RemoteIpEndPoint);
                } while (fs.Length - position > 4096);
                bytes = new Byte[fs.Length - position];
                fs.Read(bytes, 0, bytes.Length);
                try
                {
                    sende.Send(bytes, bytes.Length, endPoint);
                }
                catch
                {
                    fs.Close();
                    sende.Close();
                }
                finally
                {
                    fs.Close();
                    sende.Close();
                }
                this.Invoke((MethodInvoker)delegate {
                    progressBar1.Value = 100; // runs on UI thread
                });
                this.Invoke((MethodInvoker)delegate {
                    this.Height = 250; // runs on UI thread
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

                MessageBox.Show("Файл успешно отправлен.");
            }
            catch
            {
                this.Invoke((MethodInvoker)delegate {
                    progressBar1.Value = 100; // runs on UI thread
                });
                this.Invoke((MethodInvoker)delegate {
                    this.Height = 250; // runs on UI thread
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
                fs.Close();
                sende.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBoxDir.Text = openFileDialog1.FileName;
        }

        private void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            form1.fr = null;
        }

        private void textBoxPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            char ch = e.KeyChar;

            if (!Char.IsDigit(ch) && ch != 8) //Если символ, введенный с клавы - не цифра (IsDigit),
            {
                e.Handled = true;// то событие не обрабатывается. ch!=8 (8 - это Backspace)
            }
            if (textBoxPort.Text.Length == 5 && ch != 8)
            {
                e.Handled = true;
            }
        }

        private void textBoxIP_KeyPress(object sender, KeyPressEventArgs e)
        {
            char ch = e.KeyChar;

            if (!Char.IsDigit(ch) && ch != 8 && ch!=46) //Если символ, введенный с клавы - не цифра (IsDigit),
            {
                e.Handled = true;// то событие не обрабатывается. ch!=8 (8 - это Backspace)
            }
            if (textBoxIP.Text.Length == 15 && ch != 8)
            {
                e.Handled = true;
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            timerC++;
            if(timerC!=0&&count==0)
            {
                fs.Close();
                sende.Close();
            }
            if (timerC % 10 == 0)
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
