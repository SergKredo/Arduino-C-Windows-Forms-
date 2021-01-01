using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;
using ZedGraph;
using System.Runtime.Remoting.Messaging;

namespace Choper_Arduino_WPF_
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
        PointPairList[] lists = { new PointPairList(), new PointPairList(), new PointPairList() };
        LineItem[] myCurves = new LineItem[3];
        string[] dataCurves = { null, null, null, null };
        List<string> dataCurvesBuffer = new List<string>() { "0.00", "0.00", "0.00" };
        int count, i, countTime = 0;
        double x, y1, y2, y3, global, time = 0;
        long timeStart, timeContinue = 0;
        string serialPortName;

        public MainWindow()
        {
            InitializeComponent();
            timer.Enabled = true;
            timer.Interval = 1;
            
        }
        
    }
}
