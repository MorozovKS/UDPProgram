using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form2 : Form
    {
        Form1 frm;
        public Form2(Form1 fr)
        {
            InitializeComponent();
            frm = fr;
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!frm.waitForMessage)
            {
                if (textBox1.Text != "")
                {
                    if (long.Parse(textBox1.Text) <= 65535)
                    {
                        UdpClient tryUdp;
                        try
                        {
                            tryUdp = new UdpClient(Int32.Parse(textBox1.Text));
                            tryUdp.Close();
                        }
                        catch
                        {
                            MessageBox.Show("Данный порт не доступен");
                        }

                        frm.labelPort.Text = textBox1.Text;
                        Close();
                    }
                    else { MessageBox.Show("номер порта не более 65535"); }
                }
                else { MessageBox.Show("Введите новый номер порта"); }
            } else { MessageBox.Show("Нельзя сменить порт во время ожидание приема файла"); }

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char ch = e.KeyChar;

            if (!Char.IsDigit(ch) && ch != 8 ) //Если символ, введенный с клавы - не цифра (IsDigit),
            {
                e.Handled = true;// то событие не обрабатывается. ch!=8 (8 - это Backspace)
            }
            if(textBox1.Text.Length==5 && ch !=8)
            {
                e.Handled = true;
            }
        }
    }
}
