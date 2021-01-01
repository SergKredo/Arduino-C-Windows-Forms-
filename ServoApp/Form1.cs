using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;
using ZedGraph;
using System.Runtime.Remoting.Messaging;

namespace ServoApp
{
    public partial class Form1 : Form
    {
        SerialPort serialPort;
        TimeSpan timeSpan;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        bool triger, trigerButton2, trigerStartApp = true;
        bool connectTriger = true;
        static GraphPane myPane;
        ZedGraphControl zedGraph;
        RollingPointPairList _data;
        int _capacity = 100;
        PointPairList[] lists = { new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList() };
        LineItem[] myCurves = new LineItem[4];
        string[] dataCurves = { null, null, null, null, null };
        List<string> dataCurvesBuffer = new List<string>() { "0.00", "0.00", "0.00", "0.00" };
        int count, i, countTime = 0;
        double x, y1, y2, y3, y4, global, time = 0;
        long timeStart, timeContinue = 0;
        string serialPortName;
        public Form1()
        {
            InitializeComponent();
            //serialPort = new SerialPort();
            //serialPort.PortName = "COM4";
            //serialPort.BaudRate = 115200;
            //serialPort.Open();
            timer.Enabled = true;
            timer.Interval = 1;
            zedGraph = this.zedGraphControl1;
            _data = new RollingPointPairList(_capacity);
            //zedGraph.Location = new System.Drawing.Point();
            //zedGraph.Name = "zedGraph";
            //zedGraph.Size = new System.Drawing.Size(300, 300);
            this.Controls.Add(zedGraph);
            CreateGraph(zedGraph);
            this.button2.Enabled = false;
            this.button3.Enabled = false;
            this.button1.Enabled = false;
            this.button4.Enabled = false;
            this.trackBar1.Enabled = false;
            SetColors(myPane);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            ++countTime;
            if (triger)
            {
                double trackNumber = trackBar1.Value + 1;
                serialPort.WriteLine(trackNumber.ToString());
            }
            else
            {
                double trackNumber = global + 1;
                serialPort.WriteLine(trackNumber.ToString());
            }
            x += time;
            try
            {
                dataCurves[0] = serialPort.ReadLine().Trim();
                if (dataCurves[0].Contains("\r"))
                {
                    throw new Exception();
                }
                string pattern = @"(?<numbers>\d+[\,*\.*]\d*)";  // Шаблон регулярных выражений для поиска в тексте всех числовых данных
                Regex regex = new Regex(pattern);
                foreach (Match item in regex.Matches(dataCurves[0]))
                {
                    dataCurves[++count] = item.Groups["numbers"].Value;
                    dataCurvesBuffer[count - 1] = dataCurves[count];
                }
                count = 0;
            }
            catch
            {
                foreach (string item in dataCurvesBuffer)
                {
                    dataCurves[++count] = item;
                }
                count = 0;
            };
            try
            {
                y1 = Convert.ToDouble(dataCurves[1].Replace('.', ','));
                y2 = Convert.ToDouble(dataCurves[2].Replace('.', ','));
                y3 = Convert.ToDouble(dataCurves[3].Replace('.', ','));
                y4 = Convert.ToDouble(dataCurves[4].Replace('.', ','));
            }
            catch
            {
                y1 = Convert.ToDouble(dataCurves[1]);
                y2 = Convert.ToDouble(dataCurves[2]);
                y3 = Convert.ToDouble(dataCurves[3]);
                y3 = Convert.ToDouble(dataCurves[4]);
            }
            _data.Add(x, y1);
            _data.Add(x, y2);
            _data.Add(x, y3);
            _data.Add(x, y4);
            double xmin = x - _capacity * 0.1;
            double xmax = x;
            lists[0].Add(x, y1);
            lists[1].Add(x, y2);
            lists[2].Add(x, y3);
            lists[3].Add(x, y4);
            // Generate a red curve with diamond
            // symbols, and "Porsche" in the legend
            if (i++ == 0)
            {
                myCurves[0] = myPane.AddCurve("servo (curve 1)",
                   lists[0], Color.FromArgb(6, 245, 7), SymbolType.None);
                myCurves[2] = myPane.AddCurve("servo - filter (curve 2)",
                   lists[2], Color.Yellow, SymbolType.None);
                myCurves[1] = myPane.AddCurve("light (curve 3)",
                   lists[1], Color.Red, SymbolType.None);
                myCurves[3] = myPane.AddCurve("light - filter (curve 4)",
                   lists[3], Color.BlueViolet, SymbolType.None);
                //myCurve.Line.IsSmooth = true;
            }
            //double trackNumberFirst = global + 1;
            //serialPort.WriteLine(trackNumberFirst.ToString());
            //textBox5.Text += dataCurves[0]+Environment.NewLine;
            myCurves[0].Line.IsVisible = true;
            myCurves[0].Symbol.Fill.Color = Color.Blue;
            myCurves[0].Symbol.Fill.Type = FillType.Solid;
            myCurves[0].Symbol.Size = 1;
            myCurves[1].Line.Width = 2;
            myCurves[2].Line.Width = 2;
            myCurves[3].Line.Width = 2;
            myPane.YAxis.Scale.MinAuto = true;
            myPane.YAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.MinAuto = false;
            myPane.XAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.Min = xmin;
            myPane.XAxis.Scale.Max = xmax;
            myPane.IsBoundedRanges = true;
            myCurves[0].AddPoint(x, y1);
            myCurves[1].AddPoint(x, y2);
            myCurves[2].AddPoint(x, y3);
            myCurves[3].AddPoint(x, y4);
            zedGraph.AxisChange();
            zedGraph.Refresh();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
            this.label8.Text = "count = " + countTime;
            timeContinue = DateTime.Now.Ticks;
            timeSpan = new TimeSpan(timeContinue - timeStart);
            timeStart = timeContinue;
            time = timeSpan.Milliseconds / 1000d;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox itemCombobox = sender as ComboBox;
            serialPortName = itemCombobox.Text;
        }

        private void LoadForm1(object sender, EventArgs e)
        {
            serialPort = new SerialPort();
            List<string> portName = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                portName.Add("COM" + i);
            }
            foreach (string item in portName)
            {
                try
                {
                    serialPort.PortName = item;
                    serialPort.Open();
                }
                catch { }
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    comboBox1.Items.Add(item);
                    comboBox2.Items.Add(9600);
                    comboBox2.Items.Add(115200);
                    comboBox2.DropDownStyle = ComboBoxStyle.Simple;
                    this.comboBox1.SelectedItem = item;
                    this.comboBox2.SelectedItem = comboBox2.Items[1];
                }
            }
            this.label13.Text = null;
        }

        private void ActiveElementComboBox(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (connectTriger)
                {
                    serialPort.PortName = serialPortName;
                    serialPort.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort.Open();
                    this.button5.Text = "Disconnect";
                    this.label13.Text = "Connected!";
                    connectTriger = false;
                    this.button1.Enabled = true;
                    this.trackBar1.Enabled = true;
                }
                else
                {
                    serialPort.Close();
                    this.button5.Text = "Connect";
                    this.button1.Enabled = false;
                    this.button2.Enabled = false;
                    this.button3.Enabled = false;
                    this.trackBar1.Enabled = false;
                    timer.Stop();
                    trigerButton2 = false;
                    myPane.CurveList.Clear();
                    lists = new PointPairList[4] { new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList() };
                    myCurves = new LineItem[4];
                    dataCurves = new string[5] { null, null, null, null, null };
                    dataCurvesBuffer = new List<string>() { "0.00", "0.00", "0.00", "0.00" };
                    count = 0; i = 0;
                    x = 0; y1 = 0; y2 = 0; y3 = 0; y4 = 0; global = 0;
                    time = 0;

                    zedGraph.GraphPane.YAxis.Scale.Min = 0;
                    myPane.XAxis.Scale.Min = 0;
                    myPane.XAxis.Scale.Max = 1.2;
                    zedGraph.AxisChange();
                    zedGraph.Refresh();
                    zedGraph.Invalidate();
                    countTime = 0;
                    this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
                    this.label8.Text = "count = " + countTime;
                    connectTriger = true;
                    this.label13.Text = "Disconnected!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (var item in this.checkedListBox1.CheckedItems)
            { 
            
            }

        }

        private static void CreateGraph(ZedGraphControl zgc)
        {
            myPane = zgc.GraphPane;
            // Set the titles and axis labels
            myPane.XAxis.Title.Text = "Time, s";
            myPane.YAxis.Title.Text = "Signal, a.u.";
            myPane.XAxis.Title.FontSpec.Size = 10;
            myPane.YAxis.Title.FontSpec.Size = 10;
            // Make up some data arrays based on the Sine function


            // Set the Y axis intersect the X axis at an X value of 0.0
            // myPane.YAxis.Cross = 0.0;
            // Turn off the axis frame and all the opposite side tics
            myPane.Chart.Border.IsVisible = false;
            myPane.XAxis.MajorTic.IsOpposite = false;
            myPane.XAxis.MinorTic.IsOpposite = false;
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;

            // Calculate the Axis Scale Ranges
            //zgc.AxisChange();
            //zgc.Refresh();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            timeStart = DateTime.Now.Ticks;
            ++countTime;
            this.button2.Enabled = true;
            this.button3.Enabled = true;
            this.button4.Enabled = true;
            if (trigerStartApp)
            {
                timer.Tick += Timer_Tick;
                trigerButton2 = true;
                trigerStartApp = false;
            }
            if (!timer.Enabled)
            {
                timer.Start();
                trigerButton2 = true;
            }
            triger = true;
            double trackNumber = trackBar1.Value + 1;
            serialPort.WriteLine(trackNumber.ToString());
            label2.Text = "angle = " + trackBar1.Value.ToString();
            //textBox5.Text += serialPort.ReadLine() + Environment.NewLine;
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
            this.label8.Text = "count = " + countTime;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            ++countTime;
            if (trigerStartApp)
            {
                timer.Tick += Timer_Tick;
                timer.Start();
                trigerButton2 = true;
                trigerStartApp = false;
            }
            if (!timer.Enabled)
            {
                timer.Start();
                trigerButton2 = true;
            }
            this.trackBar1.Enabled = false;
            this.button1.Enabled = false;
            this.button2.Enabled = true;
            this.button3.Enabled = true;
            triger = false;
            double anglOne = Convert.ToDouble(textBox1.Text);
            double angleTwo = Convert.ToDouble(textBox2.Text);
            double number = Convert.ToDouble(textBox5.Text);
            serialPort.WriteLine(textBox1.Text);

            double angleThree = 0;
            for (int i = 0; i < number; i++)
            {
                this.trackBar1.Enabled = true;
                timeStart = DateTime.Now.Ticks;
                if (!trigerButton2)
                {
                    break;
                }
                if (anglOne <= angleTwo)
                {
                    //timer.Stop();
                    angleThree = anglOne;
                    anglOne = angleTwo;
                    serialPort.WriteLine(anglOne.ToString());
                    global = anglOne;
                    //textBox5.Text += await SendFromServoLessAngleTwo();
                    await SendLess();
                }
                if (anglOne == angleTwo)
                {
                    //timer.Stop();
                    anglOne = angleThree;
                    serialPort.WriteLine(anglOne.ToString());
                    global = anglOne;
                    //textBox5.Text += await SendFromServoEquallyAngleTwo();
                    await SendEqually();
                }
            }
            this.button1.Enabled = true;
            this.button4.Enabled = true;
            triger = true;
            this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
            this.label8.Text = "count = " + countTime;
        }

        private static void SetColors(GraphPane pane)
        {
            // !!!
            // Установим цвет рамки для всего компонента
            pane.Border.Color = Color.FromArgb(48, 47, 47);
            pane.Title.IsVisible = false;
            pane.Chart.Border.IsVisible = false;

            // Установим цвет рамки вокруг графика
            pane.Chart.Border.Color = Color.FromArgb(48, 47, 47);

            // Закрасим фон всего компонента ZedGraph
            // Заливка будет сплошная
            pane.Fill.Type = FillType.Solid;
            pane.Fill.Color = Color.FromArgb(48, 47, 47);

            // Закрасим область графика (его фон) в черный цвет
            pane.Chart.Fill.Type = FillType.Solid;
            pane.Chart.Fill.Color = Color.FromArgb(48, 47, 47);

            // Включим показ оси на уровне X = 0 и Y = 0, чтобы видеть цвет осей
            pane.XAxis.MajorGrid.IsZeroLine = true;
            pane.YAxis.MajorGrid.IsZeroLine = true;
            // Установим цвет осей
            pane.XAxis.Color = Color.LightGray;
            pane.YAxis.Color = Color.LightGray;

            // Включим сетку
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MinorGrid.IsVisible = false;
            pane.YAxis.MinorGrid.IsVisible = false;
            // Установим цвет для сетки
            pane.XAxis.MajorGrid.Color = Color.LightGray;
            pane.YAxis.MajorGrid.Color = Color.LightGray;
            pane.XAxis.MinorGrid.Color = Color.LightGray;
            pane.YAxis.MinorGrid.Color = Color.LightGray;

            // Установим цвет для подписей рядом с осями
            pane.XAxis.Title.FontSpec.FontColor = Color.LightGray;
            pane.YAxis.Title.FontSpec.FontColor = Color.LightGray;

            // Установим цвет подписей под метками
            pane.XAxis.Scale.FontSpec.FontColor = Color.LightGray;
            pane.YAxis.Scale.FontSpec.FontColor = Color.LightGray;

            pane.XAxis.Scale.FontSpec.Size = 8;
            pane.YAxis.Scale.FontSpec.Size = 8;

            pane.Legend.FontSpec.Size = 10;
            pane.Legend.Fill.Type = FillType.Solid;
            pane.Legend.Fill.Color = Color.FromArgb(48, 47, 47);
            pane.Legend.Border.Color = Color.FromArgb(48, 47, 47);
            pane.Legend.FontSpec.FontColor = Color.LightGray;


        }


        private void button3_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            this.button3.Enabled = false;
            this.trackBar1.Enabled = true;
            timer.Stop();
            trigerButton2 = false;
            myPane.CurveList.Clear();
            lists = new PointPairList[4] { new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList() };
            myCurves = new LineItem[4];
            dataCurves = new string[5] { null, null, null, null, null };
            dataCurvesBuffer = new List<string>() { "0.00", "0.00", "0.00", "0.00" };
            count = 0; i = 0;
            x = 0; y1 = 0; y2 = 0; y3 = 0; y4 = 0; global = 0;
            time = 0;

            zedGraph.GraphPane.YAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 1.2;
            zedGraph.AxisChange();
            zedGraph.Refresh();
            zedGraph.Invalidate();
            countTime = 0;
            this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
            this.label8.Text = "count = " + countTime;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = true;
            this.button3.Enabled = true;
            this.button2.Enabled = false;
            this.trackBar1.Enabled = true;
            timer.Stop();
            trigerButton2 = false;
        }

        async Task<string> SendFromServoLessAngleTwo()  // Асинхронный метод, который возвращает задачу generics Task типа string
        {
            return await Task.Run(() => // Класс-объект Task ставит в очередь заданную работу для запуска в пуле потоков и возвращает объект типа Task<TResult>
            {
                Thread.Sleep((int)Convert.ToDouble(textBox4.Text));
                //timer.Start();
                return serialPort.ReadLine() + Environment.NewLine;
            }
            );
        }
        async Task<string> SendFromServoEquallyAngleTwo()  // Асинхронный метод, который возвращает задачу generics Task типа string
        {
            return await Task.Run(() => // Класс-объект Task ставит в очередь заданную работу для запуска в пуле потоков и возвращает объект типа Task<TResult>
            {
                Thread.Sleep((int)Convert.ToDouble(textBox3.Text));
                return serialPort.ReadLine() + Environment.NewLine;
            }
            );
        }
        async Task SendLess()
        {
            await Task.Run(() => // Класс-объект Task ставит в очередь заданную работу для запуска в пуле потоков и возвращает объект типа Task<TResult>
            {
                Thread.Sleep((int)Convert.ToDouble(textBox4.Text));
            });
        }
        async Task SendEqually()
        {
            await Task.Run(() => // Класс-объект Task ставит в очередь заданную работу для запуска в пуле потоков и возвращает объект типа Task<TResult>
            {
                Thread.Sleep((int)Convert.ToDouble(textBox3.Text));
            });
        }
    }
}
