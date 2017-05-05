using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using ZedGraph;
using System.IO;
using System.Diagnostics;

namespace test_chart
{
    public class parameter
    {
        public int defaultVal;
        public int value;
        public string name;
        public parameter(string s, int defval)
        {
            name = s;
            defaultVal = defval;
            value = defval;
        }
    }
    public class parameterList
    {
        public parameter nomalH = new parameter("正常高阈值", 18200);
        public parameter unnomalH = new parameter("非正常高阈值", 13500);
        public parameter nomalL = new parameter("低阈值", 13200);
        public parameter h2hDistanceMin = new parameter("两高最小间距", 135);
        public parameter h2hDistanceMax = new parameter("两高最大间距", 180);
        public parameter signalAver = new parameter("信号质量参考值", 1000);
        public parameter Hminnum = new parameter("最小高个数", 0);
        public parameter Hmaxnum = new parameter("最大高个数", 0);
        public parameter Minnum = new parameter("最小谷值", 4250);

        public parameter[] par = new parameter[10];
        public int para_num = 9;
        public parameterList()
        {
            par[0] = nomalH;
            par[1] = unnomalH;
            par[2] = nomalL;
            par[3] = h2hDistanceMin;
            par[4] = h2hDistanceMax;
            par[5] = signalAver;
            par[6] = Hminnum;
            par[7] = Hmaxnum;
            par[8] = Minnum;
        }
    }
    public class detect
    {
        public static parameterList para=new parameterList();
        public static bool isallwrite = false;
        public List<int> errIndex;
        public  short val1 = 0;
        public  short val2 = 0;
        public  short filter(short val)
        {
            short temp = (short)((val + val1 + val2) / 3);
            val2 = val1;
            val1 = val;
            return temp;
        }

        public short val11 = 0;
        public short val12 = 0;
        public short filter2(short val)
        {
            short temp = (short)((val + val11 + val12) / 3);
            val12 = val11;
            val11 = val;
            return temp;
        }
        short k1 = 0;
        short k2 = 0;
         short lastVal=0 ;
         short lastLastVal = 0;
         bool hasFindExtremumH = false;
         short extremumH_val = 0;

         int extremum_cnt = 0;
         bool isNew = true;
         UInt32 cnt = 0;
         UInt32 lastCnt = 0;
         int zeroCnt = 0;
         public bool hasPaper= false;
        int dMaxIndex = 0;
        int MaxCnt = 0;
        int dMinIndex = 0;
        int MinCnt = 0;
        int MinCount = 0;
        int MinErrFlag = 0;//太近了
        public detect()
        {
             k1 = 0;
             k2 = 0;
             lastVal = 0;
             lastLastVal = 0;
             hasFindExtremumH = false;
             extremumH_val = 0;

             extremum_cnt = 0;
             isNew = true;
             cnt = 0;
             lastCnt = 0;
             zeroCnt = 0;
             hasPaper = false;
            dMaxIndex = 0;
            MaxCnt = 0;
            dMinIndex = 0;
            MinCnt = 0;
            MinCount = 0;
            MinErrFlag = 0;
            errIndex = new List<int>();
        }
         int lastMinVal = 0;
         int ePointCnt = 0;//最小的极大值
         int lowFlag = 0;
         int notHighFlag = 0;

         int testcnt = 0;
         int totalcnt = 0;
         long testsum = 0;
         int testaver = 0;
        int Hnum=0;
        public int isExtemum(short val)
        {
            short k = (short)(val - lastVal);
            int temp = 0;
            cnt++; MaxCnt++; MinCnt++;
            if (hasPaper)
            {
                totalcnt++;
                testcnt++;
            }
            if ((lastLastVal > 10800) && (hasPaper)) testsum += (lastLastVal - 10800);

            if ((k2 >= 0) && (k1 < 0))//极大值
            {
                if (lastLastVal > 14000) notHighFlag = 1;
                if ((!hasPaper && (lastLastVal > 17500)) || (hasPaper && (lastLastVal > para.nomalH.value)))
                {
                    temp = 2;
                    dMaxIndex = MaxCnt;
                    MaxCnt = 0;
                    Hnum++;
                    hasPaper = true;
                    if ((dMaxIndex < para.h2hDistanceMin.value) && (hasPaper))
                        temp = -1;
                    if ((hasPaper) && (dMaxIndex < para.h2hDistanceMax.value))//一个间距
                    {
                        if (MinCount > 4)
                            temp = -2;
                        if (MinErrFlag == 1)//小值间距太小
                            temp = -3;
                    }
                    //if ((hasPaper) && (dMaxIndex >= 180))//一个间距
                    //{
                    //    if (MinCount > (3*(dMaxIndex+30)/150+1))
                    //        temp = -2;
                    //}
                    MinCount = 0;
                    MinErrFlag = 0;
                    ePointCnt = 0;
                    lastMinVal = 0;
                    notHighFlag = 0;
                    testcnt = 0;
                    testsum = 0;
                }
                else if (lastLastVal > para.nomalL.value)
                {
                    //if ((MaxCnt > 135) && (MaxCnt < 160) && (hasPaper) && (lastLastVal > 13500))//还是大
                    if ((((MaxCnt > para.h2hDistanceMin.value) && (MaxCnt < 160)) || ((MaxCnt > 285) && (MaxCnt < 310))) && (hasPaper) && (lastLastVal > para.unnomalH.value))//还是大
                    {
                        temp = 2;
                        dMaxIndex = MaxCnt;
                        MaxCnt = 0;
                        Hnum++;
                        if ((hasPaper) && (dMaxIndex < para.h2hDistanceMax.value))//一个间距
                        {
                            if (MinCount > 4)
                                temp = -2;
                            if (MinErrFlag == 1)//小值间距太小
                                temp = -3;
                        }
                        MinCount = 0;
                        MinErrFlag = 0;
                        ePointCnt = 0;
                        lastMinVal = 0;
                        lowFlag = 0;
                        notHighFlag = 0;
                        testcnt = 0;
                        testsum = 0;
                    }
                    else
                    {
                        temp = 1;
                        MinCount++;
                        dMinIndex = MinCnt;
                        MinCnt = 0;
                        // if ((ePointCnt > 1) && (hasPaper)&&(dMinIndex < 25)&&(MaxCnt < 150)) temp = -4;
                        ePointCnt = 0;
                        if ((dMinIndex < 20) && (hasPaper))
                            MinErrFlag = 1;
                        if ((MaxCnt < 150) && (hasPaper) && (lastMinVal > 0))
                        {
                            if ((Math.Abs(lastMinVal - lastLastVal) * 100 / lastMinVal) > 25)
                                temp = -5;
                        }
                        lastMinVal = lastLastVal;
                    }
                }

                else//其他极大值
                {
                    temp = 5;
                    ePointCnt++;
                    if ((ePointCnt > 1) && (hasPaper) && (MinCnt < 28) && (MaxCnt < 150) && (lowFlag == 1)) temp = -4;
                }
                //if (lastLastVal > 13200)
                //{
                //    if ((ePointCnt != 1) && (hasPaper)&&(lowFlag==1)) temp = -4;
                //    ePointCnt = 0;
                //}
            }
            else if ((k2 <= 0) && (k1 > 0))//极小值
            {
                if ((lastLastVal < 4000) && (hasPaper) && (MaxCnt < 150) && (notHighFlag == 1))
                {
                    temp = -4;
                    lowFlag = 1;
                }
                if ((lastLastVal < para.Minnum.value) && (hasPaper) && (totalcnt > 150))///////过低谷值
                {
                    if (testcnt > 0) testaver = (int)(testsum / testcnt);
                    if ((testsum / testcnt) > para.signalAver.value)
                        temp = -6;
                }
            }
            else temp = 0;
            k2 = k1;
            k1 = k;
            lastLastVal = lastVal;
            lastVal = val;

            if ((temp > 0) && (temp < 10))
            {
                if (hasPaper)
                {
                    if ((cnt - lastCnt) > 50)
                    {
                        cnt = 0;
                        extremum_cnt = 0;
                    }
                    lastCnt = cnt;
                }
            }
            if ((temp == 0) && (Math.Abs(val - 10800) < 150)) zeroCnt++;
            if (temp == 2)
            {
                zeroCnt = 0;
                hasPaper = true;
            }
            
            if ((zeroCnt > 400) && (hasPaper))//一张纸结束
            {
                temp = 10;
                hasFindExtremumH = false;
                hasPaper = false;
                totalcnt = 0;

                if (para.Hmaxnum.value != 0) 
                {
                    if(Hnum > para.Hmaxnum.value)
                        temp = -10;
                    if(Hnum < para.Hminnum.value)
                        temp = -10;
                }
                Hnum = 0;
            }
            return temp;
        }
    }
}
