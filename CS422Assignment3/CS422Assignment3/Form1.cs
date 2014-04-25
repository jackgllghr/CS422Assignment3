using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO.Ports;
using System.Text.RegularExpressions;
using NAudio.Wave;

namespace CS422Assignment3
{
    public partial class Form1 : Form
    {
        private static SerialPort _serialPort = new SerialPort();
        String Instring;
        int Analog1 = 0;
        int Analog2 = 0;
        int Button1 = 0;
        DirectSoundOut[] jaws= new DirectSoundOut[3];

        double x = 0;
        double y = 0;

        float sharkX = 0, sharkY=0;

        int score = 0;

        double angle1, angle2;

        public Form1()
        {
            _serialPort.PortName = "COM4";
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.Two;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

            _serialPort.Open();

            jaws[0] = new DirectSoundOut();
            jaws[1] = new DirectSoundOut();
            jaws[2] = new DirectSoundOut();

            var jawsAudio1 = new WaveChannel32(new WaveFileReader("Sounds/Jaws1.wav"));
            jaws[0].Init(jawsAudio1);
            var jawsAudio2 = new WaveChannel32(new WaveFileReader("Sounds/Jaws2.wav"));
            jaws[1].Init(jawsAudio2);
            var jawsAudio3 = new WaveChannel32(new WaveFileReader("Sounds/Jaws3.wav"));
            jaws[2].Init(jawsAudio3);


            InitializeComponent();
        }

        // Get serial data and convert to INT 32
        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Match position;
            Instring += _serialPort.ReadExisting();

            if (Instring.Length > 8)
            {
                position = Regex.Match(Instring, @"\n[0123456789,]*\r");

                if (position.Success)
                {
                    String data = Instring.Substring(position.Index + 1, position.Length - 2);
                    String[] sdata = data.Split(',');
                    if (sdata.Length == 3)
                    {
                        Analog1 = Convert.ToInt32(sdata[0]);
                        Analog2 = Convert.ToInt32(sdata[1]);
                        Button1 = Convert.ToInt32(sdata[2]);
                        Instring = "";
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            angle1 = 0.5 * Math.PI * ((double)(Analog1 - 97)) / (511.0 - 97.0);
            angle2 = 0.5 * Math.PI * ((((double)(Analog2 - 477)) / (890.0 - 477.0)) - 1.0);
            x = (Math.Cos(angle1) + (Math.Cos(angle1 + angle2)));
            y = (Math.Sin(angle1) + (Math.Sin(angle1 + angle2)));

            //label1.Text = Analog1.ToString() + " " + Analog2.ToString() + " " + Button1.ToString();
            label1.Text = x.ToString() + " " + y.ToString();
            _serialPort.WriteLine("H");

            if (Analog1 > 200) jaws[0].Play();

            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Brush cm = new SolidBrush(Color.Black);
            Pen pm = new Pen(Color.Black, 3);

            double minx = 0.373511;
            double maxx = 1.69879;
            double miny = -0.05840;
            double maxy = 0.98404;

            int xx = (int)(panel1.Width * ((x - minx) / (maxx - minx)));

            int yy = (int)(panel1.Height * ((y - miny) / (maxy - miny)));

            e.Graphics.DrawEllipse(pm, new Rectangle(xx - 5, panel1.Height - (yy - 5), 10, 10));
        }
    }
}
