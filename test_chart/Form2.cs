using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using test_chart;
using System.IO;


namespace test_chart
{
    public partial class Form1 : Form
    {
        static FileStream[] rec_file=new FileStream[5];
        //static FileStream Rec_File1 = new FileStream(@"D:\\channel1.log", FileMode.Create, FileAccess.Write);  //用于存储波形数据文件
        //static FileStream Rec_File2 = new FileStream(@"D:\\channel2.log", FileMode.Create, FileAccess.Write);
        //static FileStream Rec_File3 = new FileStream(@"D:\\channel3.log", FileMode.Create, FileAccess.Write);
        //static FileStream Rec_File4 = new FileStream(@"D:\\channel4.log", FileMode.Create, FileAccess.Write);
        //static FileStream Rec_File5 = new FileStream(@"D:\\channel5.log", FileMode.Create, FileAccess.Write);
        //       static FileStream Rec_File = new FileStream(@"D:\\channel.txt", FileMode.Create, FileAccess.Write);
        public const int Y_Max = 30000;
        
        GraphPane myPane1, myPane2, myPane3, myPane4, myPane5;  //5个波形绘图控件
        static PointPairList list1, list2, list3, list4, list5, list_ref;   //5个实时波形和1个参考波形
        
        static short[] buffer;  //从文件读取已存储波形数据时的缓存
        static Int16[] Channel_Data0 = new Int16[11000];  //4860按点钞机的速度，20khz的采样率，大约是10张100元纸币的宽度
        static Int16[] Channel_Data1 = new Int16[11000];
        static Int16[] Channel_Data2 = new Int16[11000];
        static Int16[] Channel_Data3 = new Int16[11000];
        static Int16[] Channel_Data4 = new Int16[11000];
        static Int32 Sample_NO=0; //通道中采样点的序号
        static Int32[] Bill_NO=new Int32[5];   //纸币的序号，从1开始， 每张纸币加1
        static double[] avr;
        static int[] errCnt = new int[5];
        static int[] errFlag = new int[5];
        static int hasErr = 0;
        static List<int>[] errIndex = new List<int>[5];
        static bool threadEnd=false;
        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    threadEnd = true;
        //}
       
        detect[] ch = new detect[5];
        Thread t;

         public string GetTimeStamp()
        {
            string s = "";
            System.DateTime currentTime = new System.DateTime();
            currentTime = System.DateTime.Now;
            s += currentTime.ToLongDateString();
            s += currentTime.Hour;
            s += "时";
            s += currentTime.Minute;
            s += "分";
            s += currentTime.Second;
            s += "秒";
            return s;
        }
        public Form1()
        {            
            InitializeComponent();            
            this.StartPosition = FormStartPosition.CenterScreen;  //窗口居中            
            t = new Thread(receive_data);  //启动UDP数据接收线程   
            for(int i=0;i<5;i++)
            {
                string s;
                s = "D:\\detect\\ch";
                s += (i + 1).ToString();
                s += GetTimeStamp();
                s += ".log";
                rec_file[i] = new FileStream(s, FileMode.Create, FileAccess.Write);
            }
            for (int i = 0; i < 5; i++)
            {
                ch[i] = new detect();
                errIndex[i] = new List<int>();
            }
            t.Start();

        }
        //~ Form1()
        //{
        //    threadEnd = true;
        //}

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult r = MessageBox.Show("确定要退出程序?", "操作提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (r != DialogResult.OK)
                {
                    e.Cancel = true;
                    threadEnd = true;
                }
            }
        }
        public void createPane(ZedGraphControl zgc)  //创建波形
        {
            list1 = new PointPairList();  //通道1的波形数据
            list2 = new PointPairList();  //通道1的波形数据
            list3 = new PointPairList();  //通道1的波形数据
            list4 = new PointPairList();  //通道1的波形数据
            list5 = new PointPairList();  //通道1的波形数据

            //以下对波形绘图控件的参数进行设置
            myPane1 = zedGraphControl1.GraphPane;

            myPane2 = zedGraphControl2.GraphPane;

            myPane3 = zedGraphControl3.GraphPane;

            myPane4 = zedGraphControl4.GraphPane;

            myPane5 = zedGraphControl5.GraphPane;

            myPane1.YAxis.Scale.Min = 0;
            myPane1.YAxis.Scale.Max = Y_Max;
            myPane1.XAxis.Scale.Min = 0;
            myPane1.XAxis.Scale.Max = 485;

            myPane2.YAxis.Scale.Min = 0;
            myPane2.YAxis.Scale.Max = Y_Max;
            myPane2.XAxis.Scale.Min = 0;
            myPane2.XAxis.Scale.Max = 485;

            myPane3.YAxis.Scale.Min = 0;
            myPane3.YAxis.Scale.Max = Y_Max;
            myPane3.XAxis.Scale.Min = 0;
            myPane3.XAxis.Scale.Max = 485;

            myPane4.YAxis.Scale.Min = 0;
            myPane4.YAxis.Scale.Max = Y_Max;
            myPane4.XAxis.Scale.Min = 0;
            myPane4.XAxis.Scale.Max = 485;

            myPane5.YAxis.Scale.Min = 0;
            myPane5.YAxis.Scale.Max = Y_Max;
            myPane5.XAxis.Scale.Min = 0;
            myPane5.XAxis.Scale.Max = 485;                     


            myPane1.CurveList.Clear();
            myPane2.CurveList.Clear();
            myPane3.CurveList.Clear();
            myPane4.CurveList.Clear();
            myPane5.CurveList.Clear();

            //for (int i = 0; i < 486; i++)  //生成各通道的实时波形数据,每通道只显示了前面486个采样点
            //{
            //    //list1.Add(i, 15000 + Channel_Data0[i]);
            //    //list1.Add(i, 0);
            //    //list2.Add(i, 15000 + Channel_Data1[i]);
            //    //list3.Add(i, 15000 + Channel_Data2[i]);
            //    //list4.Add(i, 15000 + Channel_Data3[i]);
            //    //list5.Add(i, 15000 + Channel_Data4[i]);
            //    list1.Add(i, Channel_Data0[i]);
            //    list2.Add(i, Channel_Data1[i]);
            //    list3.Add(i, Channel_Data2[i]);
            //    list4.Add(i, Channel_Data3[i]);
            //    list5.Add(i, Channel_Data4[i]);
            //}
                                   
            //LineItem myCurve1 = myPane1.AddCurve("", list1, Color.Red, SymbolType.None); //通道1实时波形
            //LineItem myCurve2 = myPane2.AddCurve("", list2, Color.Red, SymbolType.None); //通道2实时波形
            //LineItem myCurve3 = myPane3.AddCurve("", list3, Color.Red, SymbolType.None); //通道3实时波形
            //LineItem myCurve4 = myPane4.AddCurve("", list4, Color.Red, SymbolType.None); //通道4实时波形
            //LineItem myCurve5 = myPane5.AddCurve("", list5, Color.Red, SymbolType.None); //通道5实时波形

            //LineItem myCurve_ref1 = myPane1.AddCurve("", list_ref, Color.Gray, SymbolType.None);    //当前纸币标准波形曲线
            //LineItem myCurve_ref2 = myPane2.AddCurve("", list_ref, Color.Gray, SymbolType.None);    //当前纸币标准波形曲线
            //LineItem myCurve_ref3 = myPane3.AddCurve("", list_ref, Color.Gray, SymbolType.None);    //当前纸币标准波形曲线
            //LineItem myCurve_ref4 = myPane4.AddCurve("", list_ref, Color.Gray, SymbolType.None);    //当前纸币标准波形曲线
            //LineItem myCurve_ref5 = myPane5.AddCurve("", list_ref, Color.Gray, SymbolType.None);    //当前纸币标准波形曲线

            textBox1.Text = Bill_NO[0].ToString();
          
            if(errFlag[0]==1)
            {
                for(int i=0;i<errIndex[0].Count;i++)
                {
                    textBox2.Text += errIndex[0][i].ToString();
                    textBox2.Text += ",";
                }
                
                errFlag[0] = 0;
            }
            if (errFlag[1] == 1)
            {
                for (int i = 0; i < errIndex[1].Count; i++)
                {
                    textBox3.Text += errIndex[1][i].ToString();
                    textBox3.Text += ",";
                }
                errFlag[1] = 0;
            }
            if (errFlag[2] == 1)
            {
                for (int i = 0; i < errIndex[2].Count; i++)
                {
                    textBox4.Text += errIndex[2][i].ToString();
                    textBox4.Text += ",";
                }
                errFlag[2] = 0;
            }
            if (errFlag[3] == 1)
            {
                for (int i = 0; i < errIndex[3].Count; i++)
                {
                    textBox5.Text += errIndex[3][i].ToString();
                    textBox5.Text += ",";
                }
                errFlag[3] = 0;
            }
            if (errFlag[4] == 1)
            {
                for (int i = 0; i < errIndex[4].Count; i++)
                {
                    textBox6.Text += errIndex[4][i].ToString();
                    textBox6.Text += ",";
                }
                errFlag[4] = 0;
            }
            //errCnt[0]+= analysis.isCorrect(analysis.getMaxExremumPoint(Channel_Data0), avr[0]);
            //errCnt[1] += analysis.isCorrect(analysis.getMaxExremumPoint(Channel_Data1), avr[1]);
            //errCnt[2] += analysis.isCorrect(analysis.getMaxExremumPoint(Channel_Data2), avr[2]);
            //errCnt[3] += analysis.isCorrect(analysis.getMaxExremumPoint(Channel_Data3), avr[3]);
            //errCnt[4] += analysis.isCorrect(analysis.getMaxExremumPoint(Channel_Data4), avr[4]);
            //textBox2.Text = errCnt[0].ToString();
            //textBox3.Text = errCnt[1].ToString();
            //textBox4.Text = errCnt[2].ToString();
            //textBox5.Text = errCnt[3].ToString();
            //textBox6.Text = errCnt[4].ToString();

            zgc.IsShowPointValues = true;//当鼠标经过时，显示点的坐标。
            zgc.AxisChange(); //画到zedGraphControl1控件中，此句必加
            Refresh();  //重绘控件                
        }

        //UDP 接收数据线程
        void receive_data()
        {

            Int32 pointer = 0, m;
            bool Start_Processing = false;  //通道开始记录标志，有效时，表示有纸币到来。使用一个通道做触发条件
            bool End_Processing = true;  //表示通道一次采集结束
            short[] Rev_data16 = new short[729];//9*81
            IPEndPoint udpPoint = new IPEndPoint(IPAddress.Parse("192.168.2.222"), 5030);
            UdpClient udpClient = new UdpClient(udpPoint);
            IPEndPoint senderPoint = new IPEndPoint(IPAddress.Parse("192.168.2.20"), 0);

            IPEndPoint udpConnectMe = new IPEndPoint(IPAddress.Parse("192.168.2.222"), 700);
            IPEndPoint udpConnectHim = new IPEndPoint(IPAddress.Parse("192.168.2.6"), 700);
            UdpClient udpClient2 = new UdpClient(udpConnectMe);
            udpClient2.Connect(udpConnectHim);
            byte[] sendByte = new byte[6];

            short[] temp_filted = new short[5];
            int[] type = new int[5];
            int[] errmCnt = new int[5];
            int[] endFlag = new int[5];
            int errPaperCnt = 0;
            List<int> maxList = new List<int>();


            for (int i = 0; i < 5; i++)
            {
                errmCnt[i] = 0;
            }
            while (true)
            {
                byte[] recvData = udpClient.Receive(ref senderPoint);
                for (int i = 0; i < 81; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        Rev_data16[j] = BitConverter.ToInt16(recvData, i * 18 + 2 * (5 - j));//第1路
                        temp_filted[j] = ch[j].filter(Rev_data16[j]);
                        rec_file[j].WriteByte((byte)(temp_filted[j] % 256));
                        rec_file[j].WriteByte((byte)(temp_filted[j] / 256));
                        if (type[j] == -2)
                            errmCnt[j]++;
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        if (type[k] == 10)
                        {
                            endFlag[k] = 1;
                            Bill_NO[k]++;
                            if (errmCnt[k] > 0)
                            {
                                errmCnt[k] = 0;
                                errFlag[k] = 1;//用于显示
                                errIndex[k].Add(Bill_NO[k]);
                                hasErr++;
                            }
                        }
                    }
                    if ((endFlag[0] == 1) && (endFlag[1] == 1) && (endFlag[2] == 1) && (endFlag[3] == 1) && (endFlag[4] == 1))
                    {

                        for (int k = 0; k < 5; k++)
                        {
                            endFlag[k] = 0;
                        }
                        if (hasErr > 0)
                        {
                            sendByte[0] = 2;
                        }
                        else
                        {
                            sendByte[0] = 1;
                        }
                        udpClient2.Send(sendByte, 6);

                    }

                }
                if (threadEnd == true) return;
            }
        }
       

        private void timer1_Tick(object sender, EventArgs e)   //每秒刷新一次波形
        {           
            createPane(zedGraphControl1);
            createPane(zedGraphControl2);
            createPane(zedGraphControl3);
            createPane(zedGraphControl4);
            createPane(zedGraphControl5);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)  //读文件，初始化100元的参考波形数据。
        {
            string filename = "50_ref.data";
            FileStream Rec_File = new FileStream(@filename, FileMode.Open, FileAccess.Read);
            byte[] temp = new byte[Rec_File.Length];
            Rec_File.Read(temp, 0, temp.Length);
            buffer = new short[temp.Length / sizeof(short)];
            Buffer.BlockCopy(temp, 0, buffer, 0, temp.Length);
            Rec_File.Close();

            list_ref = new PointPairList(); //标准波形数据
            for (int i = 0; i < buffer.Length; i++)   //根据当期纸币类型生成标准波形数据
            {
                list_ref.Add(i, buffer[i]);
                //list_ref.Add(i, 6000);
            }         
        }
    }
}
