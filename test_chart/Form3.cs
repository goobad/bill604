using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
namespace test_chart
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;  //窗口居中            
            this.ShowDialog();
        }
        public string name;
        public bool succuss = false;
        private void button1_Click(object sender, EventArgs e)
        {
            name=textBox1.Text;
            if(name.Count()==0)
            {
                MessageBox.Show("不能输入空白值");
                return;
            }
            for (int i=0;i< name.Count();i++)
            {
                if ((!char.IsLetter(name[i])) && (!char.IsDigit(name[i])))
                {
                    MessageBox.Show("你只能输入数字或字母");
                    return;
                }
            }
            this.Dispose();
            succuss = true;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
            succuss = false;
        }
    }
}
