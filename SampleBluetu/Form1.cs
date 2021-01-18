using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace SampleBluetu
{
    public partial class Form1 : Form
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int count = 0;
        public Form1()
        {
            InitializeComponent();
            serialPort1.PortName = "COM6";
            serialPort1.BaudRate = 115200;
            timer.Enabled = true;
            timer.Interval = 600;
            timer.Tick += Timer_Tick;
            timer.Start();
            try
            {
                if (!serialPort1.IsOpen)
                {
                    serialPort1.Open();
                }
             
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(DataReceived);

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (count % 2 != 0)
                {
                    string text = serialPort1.ReadLine();
                    this.label1.Text = text;
                    count++;
                }
                else
                {
                    this.label1.Text = "Hello Serg";
                    count++;
                }
                if (!string.IsNullOrEmpty(this.textBox1.Text))
                {
                    serialPort1.Write(this.textBox1.Text);
                    //this.label2.Text = serialPort1.ReadLine();
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort spl = (SerialPort)sender;
                object myObject = new object();
                string text = spl.ReadLine().ToString();
                Console.Write(text+"\n");
                //this.textBox1.Text+= text;
                count++;
            }
            catch (Exception ex)
            {

            }
        }
    }
}
