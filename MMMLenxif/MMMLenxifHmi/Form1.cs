using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MMMLenxif
{
    public partial class Form1 : Form
    {
        MMMLenxifService service;
        string testPath = @"C:\DATA\Private\PrivateProjVS\TestData\rawtestxml\DSZ_7829 - copia.xmp";

        public Form1()
        {
            InitializeComponent();
            this.service = new MMMLenxifService();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            testPath = @"F:\Proyectos Personales Visual Studio\MMMLenxif\testxmp\DSZ_5314 - copia (2).xmp";
            testPath = @"E:\rawtestxml";
            testPath = @"F:\RAW Laptop\2022\enero\02";
            service.UpdateManualLens(testPath);
        }
    }
}
