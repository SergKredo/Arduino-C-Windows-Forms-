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
using System.Collections;
using ZedGraph;
using System.IO;
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
        bool curveOne = true; bool curveThree = true; bool curveFour = true;
        static GraphPane myPane;
        ZedGraphControl zedGraph;
        RollingPointPairList _data;
        int _capacity = 100;
        PointPairList[] lists = { new PointPairList(), new PointPairList(), new PointPairList(), new PointPairList() };
        LineItem[] myCurves = new LineItem[4];
        string[] dataCurves = { null, null, null, null, null };
        List<string> dataCurvesBuffer = new List<string>() { "0.00", "0.00", "0.00", "0.00" };
        SortedDictionary<double, double> dataDictionary = new SortedDictionary<double, double>();
        public object sendersReset = new object();
        public EventArgs eReset = new EventArgs();

        double differenceTime;
        double[] yNull = new double[4];
        double[] yI = new double[4];
        double[] yIStrih = new double[4];

        Dictionary<double, double> dictionaryTableTime = new Dictionary<double, double>();

        public object sendersPause = new object();
        public EventArgs ePause = new EventArgs();

        public object senderCheckListSelected = new object();
        public EventArgs eCheckListSelected = new EventArgs();

        List<List<string>> listData = new List<List<string>>() { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };

        int count, i, countTime = 0;
        double x, y1, y2, y3, y4, global, time = 0;
        long timeStart, timeContinue = 0;
        string serialPortName;
        public Form1()
        {
            InitializeComponent();
            timer.Enabled = true;
            timer.Interval = 1;
            zedGraph = this.zedGraphControl1;
            _data = new RollingPointPairList(_capacity);

            this.Controls.Add(zedGraph);
            CreateGraph(zedGraph);
            this.button2.Enabled = false;
            this.button3.Enabled = false;
            this.button1.Enabled = false;
            this.button4.Enabled = false;
            this.trackBar1.Enabled = false;
            SetColors(myPane);
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.SetItemCheckState(0, CheckState.Checked);
            this.checkedListBox1.SetItemCheckState(1, CheckState.Indeterminate);
            this.checkedListBox1.SetItemCheckState(2, CheckState.Checked);
            this.checkedListBox1.SetItemCheckState(3, CheckState.Checked);

            saveFileDialog1.Filter = "Data files(*.dat)|*.dat|Text files(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog1.DefaultExt = "*.dat";
            saveFileDialog1.AddExtension = true;

            ReadDataTimeAsync();
        }

        async void ReadDataTimeAsync()
        {
            string filePath = System.IO.Path.GetFullPath("tableTime.txt");
            // асинхронное чтение
            using (StreamReader sr = new StreamReader(filePath, System.Text.Encoding.UTF8))
            {
                string line;
                string pattern = @"(?<numbers>\d+[\,*\.*]\d*)|(?<numbers>\d+)";  // Шаблон регулярных выражений для поиска в тексте всех числовых данных
                Regex regex = new Regex(pattern);
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    List<double> list = new List<double>();
                    foreach (Match item in regex.Matches(line))
                    {
                        list.Add(Convert.ToDouble(item.Groups["numbers"].Value));
                    }
                    dictionaryTableTime.Add(list[0], list[1]);
                }
            }
            decimal numberOne = this.textBox1.Value;
            decimal numberTwo = this.textBox2.Value;
            double difference = (double)Math.Abs(numberOne - numberTwo);
            foreach (KeyValuePair<double, double> keyValuePair in dictionaryTableTime)
            {
                if (difference == 0)
                {
                    this.label19.Text = $"dt(ang.2-ang.1) = {0} ms";
                    this.textBox3.Minimum = 0;
                    this.textBox4.Minimum = 0;
                }
                if (keyValuePair.Key == difference)
                {
                    differenceTime = Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.label19.Text = $"dt(ang.2-ang.1) = {Math.Round(keyValuePair.Key * keyValuePair.Value * 1000)} ms";
                    this.textBox3.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.textBox4.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown2.Minimum = this.textBox4.Minimum - (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown2.Value = this.textBox4.Value - (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown1.Minimum = this.textBox3.Minimum - (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown1.Value = this.textBox3.Value - (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                }
            }
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
                string pattern = @"(?<numbers>\d+[\,*\.*]\d*)|(?<numbers>\d+)";  // Шаблон регулярных выражений для поиска в тексте всех числовых данных
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
                y4 = Convert.ToDouble(dataCurves[4]);
            }


            //////// Алгоритм для инверсии кривых от датчика освещенности ///////////////

            yIStrih[1] = y2; yIStrih[3] = y4;
            y2 = Math.Abs(yNull[1] + (yI[1] - yIStrih[1]));
            y4 = Math.Abs(yNull[3] + (yI[3] - yIStrih[3]));
            yNull[1] = y2; yNull[3] = y4;
            yI[1] = yIStrih[1]; yI[3] = yIStrih[3];

            /////////////////////////////////////////////////////////////////////////////

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

            listData[0].Add(y1.ToString());
            listData[1].Add(y2.ToString());
            listData[2].Add(y3.ToString());
            listData[3].Add(y4.ToString());
            listData[4].Add(Math.Round(x, 3).ToString());

            if (i++ == 0)
            {

                myCurves[0] = myPane.AddCurve("servo (curve 1)",
                   lists[0], Color.FromArgb(6, 245, 7), SymbolType.None);

                myCurves[2] = myPane.AddCurve("servo - filter (curve 2)",
               lists[2], Color.Yellow, SymbolType.None);

                myCurves[1] = myPane.AddCurve("light (curve 3)",
               lists[1], Color.BlueViolet, SymbolType.None);

                myCurves[3] = myPane.AddCurve("light - filter (curve 4)",
               lists[3], Color.Red, SymbolType.None);

            }

            if (curveOne)
            {
                myCurves[0].Line.IsVisible = true;
            }
            else
            {
                myCurves[0].Line.IsVisible = false;
            }

            myCurves[2].Line.IsVisible = true;

            if (curveThree)
            {
                myCurves[1].Line.IsVisible = true;
            }
            else
            {
                myCurves[1].Line.IsVisible = false;
            }
            if (curveFour)
            {
                myCurves[3].Line.IsVisible = true;
            }
            else
            {
                myCurves[3].Line.IsVisible = false;
            }



            myPane.YAxis.Scale.MinAuto = true;
            myPane.YAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.MinAuto = false;
            myPane.XAxis.Scale.MaxAuto = true;
            myPane.XAxis.Scale.Min = xmin;
            myPane.XAxis.Scale.Max = xmax;
            myPane.IsBoundedRanges = true;
            myCurves[0].AddPoint(x, y1);
            myCurves[0].Line.Width = 2;
            myCurves[1].AddPoint(x, y2);
            myCurves[1].Line.Width = 2;
            myCurves[2].AddPoint(x, y3);
            myCurves[2].Line.Width = 2;
            myCurves[3].AddPoint(x, y4);
            myCurves[3].Line.Width = 2;

            zedGraph.AxisChange();
            zedGraph.Refresh();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            this.label7.Text = "all time = " + Math.Round(x, 2) + " s";
            this.label8.Text = "count = " + countTime;
            this.label14.Text = $"f = {Math.Round((countTime / Math.Round(x, 2)))} Hz";
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

        private void textBox1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            decimal numberOne = numericUpDown.Value;
            decimal numberTwo = this.textBox2.Value;
            bool trig = true;
            double difference = (double)Math.Abs(numberOne - numberTwo);
            foreach (KeyValuePair<double, double> keyValuePair in dictionaryTableTime)
            {
                if (difference == 0 && trig)
                {
                    this.label19.Text = $"dt(ang.2-ang.1) = {0} ms";
                    this.textBox3.Minimum = 0;
                    this.textBox4.Minimum = 0;
                    this.numericUpDown2.Value += (decimal)differenceTime;
                    this.numericUpDown1.Value += (decimal)differenceTime;
                    trig = false;
                }
                if (keyValuePair.Key == difference && trig)
                {
                    differenceTime = Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.label19.Text = $"dt(ang.2-ang.1) = {Math.Round(keyValuePair.Key * keyValuePair.Value * 1000)} ms";
                    this.textBox3.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.textBox4.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown2.Value = this.textBox4.Value - (decimal)differenceTime;
                    this.numericUpDown1.Value = this.textBox3.Value - (decimal)differenceTime;
                }
            }
        }

        private void textBox2_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            decimal numberOne = numericUpDown.Value;
            decimal numberTwo = this.textBox1.Value;
            bool trig = true;
            double difference = (double)Math.Abs(numberOne - numberTwo);
            foreach (KeyValuePair<double, double> keyValuePair in dictionaryTableTime)
            {
                if (difference == 0 && trig)
                {
                    this.label19.Text = $"dt(ang.2-ang.1) = {0} ms";
                    this.textBox3.Minimum = 0;
                    this.textBox4.Minimum = 0;
                    this.numericUpDown2.Value += (decimal)differenceTime;
                    this.numericUpDown1.Value += (decimal)differenceTime;
                    trig = false;
                }
                if (keyValuePair.Key == difference && trig)
                {
                    differenceTime = Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.label19.Text = $"dt(ang.2-ang.1) = {Math.Round(keyValuePair.Key * keyValuePair.Value * 1000)} ms";
                    this.textBox3.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.textBox4.Minimum = (decimal)Math.Round(keyValuePair.Key * keyValuePair.Value * 1000);
                    this.numericUpDown2.Value = this.textBox4.Value - (decimal)differenceTime;
                    this.numericUpDown1.Value = this.textBox3.Value - (decimal)differenceTime;
                    trig = false;
                }
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            this.textBox4.Value = numericUpDown.Value + (decimal)differenceTime;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            this.textBox3.Value = numericUpDown.Value + (decimal)differenceTime;
        }

        private void textBox4_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            this.numericUpDown2.Value = numericUpDown.Value - (decimal)differenceTime;
        }

        private void textBox3_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = sender as NumericUpDown;
            this.numericUpDown1.Value = numericUpDown.Value - (decimal)differenceTime;
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            senderCheckListSelected = sender;
            eCheckListSelected = e;

            curveOne = curveThree = curveFour = false;


            foreach (var item in this.checkedListBox1.CheckedItems)
            {
                switch (item.ToString())
                {
                    case "servo (curve 1)":
                        curveOne = true;
                        break;
                    case "light (curve 3)":
                        curveThree = true;
                        break;
                    case "light - filter (curve 4)":
                        curveFour = true;
                        break;
                    default: break;
                }
            }
            this.checkedListBox1.SetItemCheckState(1, CheckState.Indeterminate);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            button2_Click(sendersPause, ePause);
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            double frequency = Math.Round((countTime / Math.Round(x, 2)));
            builder.Append($"Choper 'Arduino_Uno & Winforms_C# by SergKredo' - ({frequency} Hz) {Math.Round(x, 2)} sec: {DateTime.Now.ToLongTimeString()}\r\n");
            builder.Append("Time, sec");
            if (myCurves[0].Line.IsVisible)
            {
                builder.Append(" \t Servo_curve1, a.u.");
            }
            if (myCurves[1].Line.IsVisible)
            {
                builder.Append(" \t Light_curve3, a.u.");
            }
            if (myCurves[2].Line.IsVisible)
            {
                builder.Append(" \t Servo-filter_curve2, a.u.");
            }
            if (myCurves[3].Line.IsVisible)
            {
                builder.Append(" \t Light-filter_curve4, a.u.");
            }
            builder.Append($"\r\n");

            for (int i = 0; i < listData[0].Count; i++)
            {
                builder.Append($"{listData[4][i]}");
                if (myCurves[0].Line.IsVisible)
                {
                    builder.Append($" \t {listData[0][i]}");
                }
                if (myCurves[1].Line.IsVisible)
                {
                    builder.Append($" \t {listData[1][i]}");
                }
                if (myCurves[2].Line.IsVisible)
                {
                    builder.Append($" \t {listData[2][i]}");
                }
                if (myCurves[3].Line.IsVisible)
                {
                    builder.Append($" \t {listData[3][i]}");
                }
                builder.Append($"\r\n");
            }

            // получаем выбранный файл
            string filename = saveFileDialog1.FileName;
            // сохраняем текст в файл
            await WriteAllAsync(filename, builder.ToString());
        }


        async Task WriteAllAsync(string filename, string data)
        {
            await Task.Run(() => File.WriteAllText(filename, data));
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
            this.button4.Enabled = true;
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
                if (anglOne <= angleTwo || anglOne >= angleTwo)
                {
                    angleThree = anglOne;
                    anglOne = angleTwo;
                    serialPort.WriteLine(anglOne.ToString());
                    global = anglOne;
                    await SendLess();
                }
                if (anglOne == angleTwo)
                {
                    anglOne = angleThree;
                    serialPort.WriteLine(anglOne.ToString());
                    global = anglOne;
                    await SendEqually();
                }
            }
            this.button1.Enabled = true;
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
            listData = new List<List<string>>() { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };

            checkedListBox1_SelectedIndexChanged(senderCheckListSelected, eCheckListSelected);
            sendersReset = sender;
            eReset = e;
            this.button1.Enabled = true;
            this.button2.Enabled = false;
            this.button3.Enabled = false;
            this.button4.Enabled = false;
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
            sendersPause = sender;
            ePause = e;
            this.button1.Enabled = true;
            this.button3.Enabled = true;
            this.button2.Enabled = false;
            this.trackBar1.Enabled = true;
            timer.Stop();
            trigerButton2 = false;
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
