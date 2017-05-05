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
        static FileStream[] rec_file = new FileStream[5];
        static FileStream[] recall_file = new FileStream[5];
        public const int Y_Max = 30000;
        GraphPane myPane1, myPane2, myPane3, myPane4, myPane5;  //5个波形绘图控件
        static PointPairList list1, list2, list3, list4, list5, list_ref;   //5个实时波形和1个参考波形
        static List<short>[] recData = new List<short>[5];
        static short[] buffer;  //从文件读取已存储波形数据时的缓存
        //static Int16[] Channel_Data0 = new Int16[5000];  //4860按点钞机的速度，20khz的采样率，大约是10张100元纸币的宽度
        //static Int16[] Channel_Data1 = new Int16[5000];
        //static Int16[] Channel_Data2 = new Int16[5000];
        //static Int16[] Channel_Data3 = new Int16[5000];
        //static Int16[] Channel_Data4 = new Int16[5000];
        static Int32 Sample_NO = 0; //通道中采样点的序号
                                    //     static Int32 Bill_NO=0;   //纸币的序号，从1开始， 每张纸币加1
        static Int32[] Bill_NO = new Int32[5];
        static List<int>[] errIndex = new List<int>[5];
        static double[] avr;
        static int[] errCnt = new int[5];
        static int[] errFlag = new int[5];
        static detect chOffLine = new detect();
        static bool threadEnd = false;
        static Int32 BillGoodNO = 0;
        GraphPane myPane;
        static List<ListViewItem> listView = new List<ListViewItem>();
        static int listViewChangedFlag = 0;
        static int listViewLastCount = 0;

        List<int> listEndPoint = new List<int>();

        //static IPEndPoint udpPoint ;
        //static UdpClient udpClient;
        //static IPEndPoint senderPoint;
        static IPEndPoint udpPoint = new IPEndPoint(IPAddress.Parse("192.168.2.222"), 5030);//2.222
        static UdpClient udpClient= new UdpClient(udpPoint);
        static IPEndPoint senderPoint = new IPEndPoint(IPAddress.Parse("192.168.2.20"), 0);

        static IPEndPoint udpConnectMe = new IPEndPoint(IPAddress.Parse("192.168.2.222"), 700);//2.222
        static IPEndPoint udpConnectHim = new IPEndPoint(IPAddress.Parse("192.168.2.6"), 700);
        static UdpClient udpClient2= new UdpClient(udpConnectMe);
        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    threadEnd = true;
        //}

        detect[] ch = new detect[5];
        detect chOffline;
        Thread t;
        Thread test;
        private void button2_Click(object sender, EventArgs e)
        {
            threadEnd = true;
            System.Environment.Exit(System.Environment.ExitCode);
            this.Dispose();
            this.Close();
        }

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
        string openfilename;
        int fileopendflag = 0;
        private void button1_Click_1(object sender, EventArgs e)//打开文件
        {

            OpenFileDialog dialog = new OpenFileDialog();

            long length = 0;
            int start = 0; ;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filename = dialog.FileName;
                FileStream Rec_File = new FileStream(@filename, FileMode.Open, FileAccess.Read);
                length = Rec_File.Length;
                start = 127;
                byte[] temp = new byte[length];
                Rec_File.Read(temp, 0, temp.Length);
                buffer = new short[temp.Length / sizeof(short)];
                Buffer.BlockCopy(temp, 0, buffer, 0, buffer.Length * sizeof(short));
                openfilename = Rec_File.Name;
                Rec_File.Close();
                textBox5.Text = rmDir(Rec_File.Name);
                listEndPoint.Clear();
                createPane(zedGraphControl1);
                fileopendflag = 1;
            }


        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((this.listView1.SelectedItems.Count > 0) && (!isStart)&&(fileopendflag==1))
            {
                myPane.YAxis.Scale.Min = 0;
                myPane.YAxis.Scale.Max = 25000;
                myPane.XAxis.Scale.Min = 5000 * int.Parse(listView1.SelectedItems[0].SubItems[1].Text);
                myPane.XAxis.Scale.Max = 5000 * int.Parse(listView1.SelectedItems[0].SubItems[1].Text) + 5000;
                int n = int.Parse(listView1.SelectedItems[0].SubItems[1].Text);
                if ((n <= listEndPoint.Count) && (n > 0))
                {
                    myPane.XAxis.Scale.Min = listEndPoint[n - 1] - 5000;
                    myPane.XAxis.Scale.Max = listEndPoint[n - 1];
                }
                Refresh();
            }

        }

        public string GetTimeStamp1()
        {
            string s = "";
            System.DateTime currentTime = new System.DateTime();
            currentTime = System.DateTime.Now;
            s += currentTime;
            return s;
        }
        public Form1()
        {
           InitializeComponent();
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;  //窗口居中   

            this.listView1.GridLines = true; //显示表格线
            this.listView1.View = View.Details;//显示表格细节
            this.listView1.LabelEdit = true; //是否可编辑,ListView只可编辑第一列。
            this.listView1.Scrollable = true;//有滚动条
            this.listView1.HeaderStyle = ColumnHeaderStyle.Clickable;//对表头进行设置
            this.listView1.FullRowSelect = true;//是否可以选择行

            this.listView2.GridLines = true; //显示表格线
            this.listView2.View = View.Details;//显示表格细节
            this.listView2.LabelEdit = true; //是否可编辑,ListView只可编辑第一列。
            this.listView2.Scrollable = true;//有滚动条
            this.listView2.HeaderStyle = ColumnHeaderStyle.Clickable;//对表头进行设置
            this.listView2.FullRowSelect = true;//是否可以选择行

            ListViewItem paraListView = new ListViewItem();
            string[] listViewString = new string[4];
            for (int i = 0; i < detect.para.para_num; i++)
            {
                listViewString[0] = detect.para.par[i].value.ToString();
                listViewString[1] = detect.para.par[i].name;
                listViewString[2] = detect.para.par[i].defaultVal.ToString();
                this.listView2.Items.Add(new ListViewItem(listViewString));
            }
            // udpClient = new UdpClient(udpPoint);
            //udpClient2 = new UdpClient(udpConnectMe);
        }

        string getChannel(string filename)
        {
            int pos = 0;
            for (int i = 0; i < (filename.Count() - 1); i++)
            {
                if ((filename[i] == 'c') && (filename[i + 1] == 'h')) pos = i;
            }
            string name = "";
            name += filename[pos + 2];
            return name;
        }
        short getShort = 0;
        int testflag = 0;
        void tesGetByte()
        {
            //IPEndPoint udpPoint = new IPEndPoint(IPAddress.Parse("192.168.2.222"), 5030);
            //UdpClient udpClient = new UdpClient(udpPoint);
            //IPEndPoint senderPoint = new IPEndPoint(IPAddress.Parse("192.168.2.20"), 0);
            while (true)
            {
                byte[] get = udpClient.Receive(ref senderPoint);
                getShort = BitConverter.ToInt16(get, 2);
                if (testflag == 0) return;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            // get = null;
            test = new Thread(tesGetByte);  //启动UDP数据接收线程 
            testflag = 1;
            test.Start();
            Thread.Sleep(3000);
            if (Math.Abs(getShort - 10800) > 300)
            {
                displayTextBox.Text += GetTimeStamp1();
                displayTextBox.Text += "系统没有正常连接,请检测网络或电路盒\n";
            }
            else
            {
                displayTextBox.Text += GetTimeStamp1();
                displayTextBox.Text += "系统网络连接正常\n";
            }
            testflag = 0;// t.Abort();

        }

        private void listView2_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if(e.Label==null)
            {
                MessageBox.Show("内容不能为空！", "信息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.listView2.Items[e.Item].BeginEdit();
            }
            else
            {
                displayTextBox.Text += "你把参数\"";
                displayTextBox.Text += detect.para.par[e.Item].name;
                displayTextBox.Text += "\"的值改为了";
                displayTextBox.Text += e.Label;
                displayTextBox.Text += "\n";
                detect.para.par[e.Item].value = int.Parse(e.Label);
            }
        }
        void writetxt(FileStream file,string text)
        {
            byte[] data = System.Text.Encoding.Default.GetBytes(text);
            for (int i=0;i< data.Length;i++)
            {
                file.WriteByte((byte)data[i]);
            }
        }
        private void button5_Click(object sender, EventArgs e)//保存存档
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "参数存档（*.txt）|*.txt";
            //设置默认文件类型显示顺序 
            dialog.FilterIndex = 1;
            //保存对话框是否记忆上次打开的目录 
            dialog.RestoreDirectory = true;
            long length = 0;
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filename = dialog.FileName;
                FileStream para_File = new FileStream(@filename, FileMode.CreateNew, FileAccess.ReadWrite);
                for(int i=0;i< detect.para.para_num; i++)
                {
                    writetxt(para_File, detect.para.par[i].name.ToString());
                    para_File.WriteByte((byte)':');
                    writetxt(para_File, detect.para.par[i].value.ToString());
                    para_File.WriteByte((byte)';');
                    para_File.WriteByte((byte)'\r');
                    para_File.WriteByte((byte)'\n');
                }
                para_File.Close();
            }
        }

        private void button6_Click(object sender, EventArgs e)//打开参数存档
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "参数存档（*.txt）|*.txt";
            long length = 0;
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filename = dialog.FileName;
                FileStream Rec_File = new FileStream(@filename, FileMode.Open, FileAccess.Read);
                length = Rec_File.Length;
                byte[] temp = new byte[length];
                Rec_File.Read(temp, 0, temp.Length);
                int cnt = 0;
                string val=null;
                for(int i=0;i< detect.para.para_num; i++)
                {
                    val = null;
                    while (temp[cnt++] != (byte)':') ;
                    while (temp[cnt] != (byte)';')
                    {
                        val += (char)temp[cnt++];
                    }
                    detect.para.par[i].value = int.Parse(val);
                    listView2.Items[i].Text = detect.para.par[i].value.ToString();
                }

                Rec_File.Close();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                displayTextBox.Text +=  "全部存储模式：点击开始后，所有数据都会存储下来，仅在调试中使用此模式！\n";
                detect.isallwrite = true;
            }
            else
            {
                displayTextBox.Text += "错纸模式：仅会存储错纸数据\n";
                detect.isallwrite = false;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            myPane.YAxis.Scale.Min = 0;
            myPane.YAxis.Scale.Max = 25000;
            int n = (int)(numericUpDown1.Value);//int.Parse(numericUpDown1.Text);
            if ((n <= listEndPoint.Count) && (n > 0))
            {
                myPane.XAxis.Scale.Min = listEndPoint[n - 1] - 5000;
                myPane.XAxis.Scale.Max = listEndPoint[n - 1];
                Refresh();
            }
            else
            {
                MessageBox.Show("超出了编号范围");
            }
        }

        string getName(string filename)
        {
            int pos = 0;
            int len = 0;
            for (int i = 0; i < filename.Count(); i++)
            {
                if (filename[i] == '\\') pos = i;
            }
            pos++;
            for (int i = pos; i < filename.Count() - 1; i++)
            {
                if ((filename[i] == 'c') && (filename[i + 1] == 'h')) len = i - pos;
            }
            string name = "";
            for (int i = 0; i < len; i++)
            {
                name += filename[pos + i];
            }
            return name;
        }
        string rmDir(string filename)
        {
            int pos = 0;
            for (int i = 0; i < filename.Count(); i++)
            {
                if (filename[i] == '\\') pos = i;
            }
            pos++;
            string name = "";
            for (int i = pos; i < filename.Count(); i++)
            {
                name += filename[i];
            }
            return name;
        }
        public void createPane(ZedGraphControl zgc)  //创建波形
        {
            myPane = zgc.GraphPane;
            myPane.CurveList.Clear();
            list1 = new PointPairList();
            chOffline = new detect();
            listView.Clear();
            listViewChangedFlag = 0;
            listViewLastCount = 0;
            this.listView1.Items.Clear();
            myPane.YAxis.Scale.Min = 0;
            myPane.YAxis.Scale.Max = 25000;
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 5000;
            PointPairList listMaxH = new PointPairList();
            PointPairList listMaxL = new PointPairList();
            PointPairList listerr = new PointPairList();
            int type;
            short temp_filted = 0, temp_filtedLastLast = 0, temp_filtedLast = 0;
            int errmCnt = 0;
            int pointCnt = 0, pointCount = 0; ;
            int bill_no = 0;
            int no = 0;
            int errType = 0;
            for (int i = 0; i < buffer.Count(); i++)
            {
                temp_filted = chOffline.filter(buffer[i]);
                temp_filted = chOffline.filter2(temp_filted);
                list1.Add(i, temp_filted);
                type = chOffline.isExtemum(temp_filted);

                if ((type == 1) || (type == 3))//小极大值
                    listMaxH.Add(i - 2, temp_filtedLastLast);
                else if (type == 2)//大极大值
                    listMaxL.Add(i - 2, temp_filtedLastLast);
                else if (type < 0)
                {
                    listerr.Add(i - 2, temp_filtedLastLast);
                    errType = 0 - type;
                    errmCnt++;
                }

                if ((type == 1) || (type == 3) || (type == 2))
                {
                    pointCount = pointCnt;
                }
                if ((type == 10)|| (type == -10))
                {
                    bill_no++;
                    pointCnt = 0;
                    listEndPoint.Add(i - 2);

                    if (errmCnt > 0)
                    {
                        no++;
                        string[] listViewString = new string[4];
                        listViewString[0] = no.ToString();
                        listViewString[1] = bill_no.ToString();
                        listViewString[2] = getChannel(this.openfilename);
                        listViewString[3] = "判据";
                        listViewString[3] += errType.ToString();
                        listView.Add(new ListViewItem(listViewString));
                        listViewChangedFlag = 1;
                        // this.listView1.Items.Add(listView[listView.Count-1]);
                        errmCnt = 0;
                        errType = 0;
                    }
                    else //好周期
                    {

                    }
                    errmCnt = 0;
                }
                temp_filtedLastLast = temp_filtedLast;
                temp_filtedLast = temp_filted;
                //if (i < 10)
                //{
                //    listView.Add(new ListViewItem(new string[] { "1", "1", "bbbb", "aaaa" }));
                //    this.listView1.Items.Add(listView[i]);
                //}
            }
            //createPane(zedGraphControl1);
            numericUpDown1.Maximum = listEndPoint.Count;
            myPane.YAxis.Scale.Min = 0;
            myPane.YAxis.Scale.Max = 50000;
            if (listEndPoint.Count != 0)
            {
                myPane.XAxis.Scale.Min = listEndPoint[0] - 5000;
                myPane.XAxis.Scale.Max = listEndPoint[Math.Min(listEndPoint.Count - 1, 50)];
            }
            else
            {
                myPane.XAxis.Scale.Min = 0;
                myPane.XAxis.Scale.Max = 60000;
            }


            textBox1.Text = bill_no.ToString();

            myPane = zedGraphControl1.GraphPane;
            myPane.CurveList.Clear();
            LineItem myCurve = myPane.AddCurve("原始数据", list1, Color.Red, SymbolType.None);
            LineItem myCurveH = myPane.AddCurve("极大值", listMaxH, Color.Green, SymbolType.Star);
            myCurveH.Line.Color = Color.White;
            LineItem myCurveL = myPane.AddCurve("极小值", listMaxL, Color.Blue, SymbolType.Circle);
            myCurveL.Line.Color = Color.White;
            LineItem myCurveErr2 = myPane.AddCurve("错误点", listerr, Color.Coral, SymbolType.Circle);
            myCurveErr2.Line.Color = Color.White;
            zedGraphControl1.IsShowPointValues = true;//当鼠标经过时，显示点的坐标。
            zedGraphControl1.AxisChange(); //画到zedGraphControl1控件中，此句必加
            Refresh();  //重绘控件  
        }

        //UDP 接收数据线程
        void receive_data()
        {
            //while (true)
            //{
            //    if (threadEnd == true) return;
            //}
            short[] Rev_data16 = new short[5];//9*81
            byte[] sendByte = new byte[6];

            short[] temp_filted = new short[5];
            int[] type = new int[5];
            int[] errmCnt = new int[5];
            int[] endFlag = new int[5];
            List<int> maxList = new List<int>();


            udpClient2.Connect(udpConnectHim);
            for (int i = 0; i < 5; i++)
            {
                errmCnt[i] = 0;
            }
            int no = 0;
            int[] errType = new int[5];
            while (true)
            {
                byte[] recvData = udpClient.Receive(ref senderPoint);
                for (int i = 0; i < 81; i++)
                {
                    for (int chIndex = 0; chIndex < 5; chIndex++)
                    {
                        Rev_data16[chIndex] = BitConverter.ToInt16(recvData, i * 18 + 2 * (5 - chIndex));//第1路
                        temp_filted[chIndex] = ch[chIndex].filter(Rev_data16[chIndex]);
                        temp_filted[chIndex] = ch[chIndex].filter2(temp_filted[chIndex]);
                        if (detect.isallwrite)
                        {
                            recall_file[chIndex].WriteByte((byte)(Rev_data16[chIndex] % 256));
                            recall_file[chIndex].WriteByte((byte)(Rev_data16[chIndex] / 256));
                        }
                        type[chIndex] = ch[chIndex].isExtemum(temp_filted[chIndex]);
                        if (type[chIndex] < 0)
                        {
                            errmCnt[chIndex]++;
                            errType[chIndex] = 0 - type[chIndex];
                        }
                        if ( (type[chIndex] == 10)|| (type[chIndex] == -10))
                        {
                            endFlag[chIndex] = 1;
                            Bill_NO[chIndex]++;
                        }
                        if (ch[chIndex].hasPaper)
                        {
                            recData[chIndex].Add(Rev_data16[chIndex]);
                        }
                    }

                    if ((endFlag[0] == 1) && (endFlag[1] == 1) && (endFlag[2] == 1) && (endFlag[3] == 1) && (endFlag[4] == 1))
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            endFlag[k] = 0;
                        }
                        if ((errmCnt[0] > 0) || (errmCnt[1] > 0) || (errmCnt[2] > 0) || (errmCnt[3] > 0) || (errmCnt[4] > 0))//error
                        {
                            sendByte[0] = 2;
                            udpClient2.Send(sendByte, 6);
                            for (int x = 0; x < 5000; x++)
                            {
                                for (int w = 0; w < 5; w++)
                                {
                                    if (x < recData[w].Count)
                                    {
                                        rec_file[w].WriteByte((byte)(recData[w][x] % 256));
                                        rec_file[w].WriteByte((byte)(recData[w][x] / 256));
                                    }
                                    else
                                    {
                                        rec_file[w].WriteByte((byte)(10800 % 256));
                                        rec_file[w].WriteByte((byte)(10800/256));
                                    }
                                }
                            }

                        }
                        else
                        {
                            sendByte[0] = 1;
                            BillGoodNO++;
                            udpClient2.Send(sendByte, 6);
                        }
                        for (int k = 0; k < 5; k++)
                        {
                            if (errmCnt[k] > 0)
                            {
                                errFlag[k] = 1;//显示用
                                errIndex[k].Add(Bill_NO[k]);
                                no++;
                                string[] listViewString = new string[4];
                                listViewString[0] = no.ToString();
                                listViewString[1] = Bill_NO[k].ToString();
                                listViewString[2] = (k + 1).ToString();
                                listViewString[3] = "判据";
                                listViewString[3] += errType[k].ToString();
                                listView.Add(new ListViewItem(listViewString));
                                listViewChangedFlag = 1;
                                // this.listView1.Items.Add(listView[listView.Count - 1]);
                            }
                            errType[k] = 0;
                            recData[k].Clear();
                        }
                        errmCnt[0] = 0; errmCnt[1] = 0; errmCnt[2] = 0; errmCnt[3] = 0; errmCnt[4] = 0;
                    }

                }
                if (threadEnd == true) return;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)   //每秒刷新一次波形
        {
            //createPane(zedGraphControl1);
            textBox1.Text = Bill_NO[4].ToString();
            textBox2.Text = BillGoodNO.ToString();
            textBox3.Text = (Bill_NO[4] - BillGoodNO).ToString();
            textBox4.Text = ((double)(Bill_NO[4] - BillGoodNO)*100 / Bill_NO[4]).ToString();
            textBox4.Text += "%";
            if (listViewChangedFlag == 1)
            {
                for (int i = 0; i < (listView.Count - listViewLastCount); i++)
                {
                    this.listView1.Items.Add(listView[listViewLastCount + i]);
                    listViewChangedFlag = 0;
                }
                listViewLastCount = listView.Count;
            }
        }

        bool isStart = false;
        Form3 form3;
        private void button4_Click(object sender, EventArgs e)//开始停止
        {
            isStart = !isStart;
            if (isStart)
            {
                form3 = new Form3();
                if (!form3.succuss)
                {
                    isStart = !isStart;
                    displayTextBox.Text += GetTimeStamp1();
                    displayTextBox.Text += "系统未开始\n";
                    return;
                }
                button4.Text = "停止";
                displayTextBox.Text += GetTimeStamp1();
                displayTextBox.Text += "批次";
                displayTextBox.Text += form3.name;
                displayTextBox.Text += "纸张已开始检测...";
                displayTextBox.Text += "\n";
                for (int i = 0; i < 5; i++)
                {
                    string s;
                    s = "D:\\detect\\";
                    s += form3.name;
                    s += "ch";
                    s += (i + 1).ToString();
                    s += GetTimeStamp();
                    s += ".log";
                    rec_file[i] = new FileStream(s, FileMode.Create, FileAccess.Write);
                    if (detect.isallwrite)
                    {
                        string all ;
                        all = "D:\\detect\\all";
                        all += form3.name;
                        all += "ch";
                        all += (i + 1).ToString();
                        all += GetTimeStamp();
                        all += ".log";
                        recall_file[i] = new FileStream(all, FileMode.Create, FileAccess.Write);
                    }
                }

                t = new Thread(receive_data);  //启动UDP数据接收线程   
                for (int i = 0; i < 5; i++)
                {
                    ch[i] = new detect();
                    errIndex[i] = new List<int>();
                    recData[i] = new List<short>();
                }
                button1.Enabled = false;
                zedGraphControl1.Enabled = false;
                button3.Enabled = false;
                checkBox1.Enabled = false;
                t.Start();
                fileopendflag = 0;
            }
            else
            {
                button4.Text = "开始";
                displayTextBox.Text += GetTimeStamp1();
                displayTextBox.Text += "系统已停止。";
                displayTextBox.Text += "\n";
                threadEnd = true;
                button1.Enabled = true;
                zedGraphControl1.Enabled = true;
                checkBox1.Enabled = true;
                button3.Enabled = true;
                for (int i = 0; i < 5; i++)
                {
                    rec_file[i].Close();
                    if (detect.isallwrite)
                        recall_file[i].Close();                }

            }
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
