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
        WaveOut[] jaws= new WaveOut[3];
        
        bool isActive=true;
        
        double x = 0;
        double y = 0;

        double sharkX = 0, sharkY=0;
        double sharkRad0=0.1, sharkRad1=0.3, sharkRad2=0.5;

        double minx = 0.6;
        double maxx = 1.7;
        double miny = 0.1;
        double maxy = 1.1;

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
            
            jaws[0] = new WaveOut();
            jaws[1] = new WaveOut();
            jaws[2] = new WaveOut();

            var jawsAudio1 = new WaveChannel32(new WaveFileReader("Sounds/Jaws3.wav"));
            jaws[0].Init(jawsAudio1);
            var jawsAudio2 = new LoopStream(new WaveFileReader("Sounds/Jaws2.wav"));
            jaws[1].Init(jawsAudio2);
            var jawsAudio3 = new LoopStream(new WaveFileReader("Sounds/Jaws1.wav"));
            jaws[2].Init(jawsAudio3);

            resetShark();

            
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
            //label1.Text = sharkX.ToString() + " " + sharkY.ToString();
            _serialPort.WriteLine("H");

            if(isActive) checkForShark(x, y);

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
        private void checkForShark(double x, double y)
        {
            label2.Text = "Score: " + score.ToString();


            double dx = Math.Abs(x - sharkX);
            double dy = Math.Abs(y - sharkY);

            //Get distance between the cursor and shark
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance <= sharkRad2)
            {
                if (distance <= sharkRad1)
                {
                    if (distance <= sharkRad0)
                    {
                        isActive = false;
                        jaws[0].Play();
                        jaws[1].Stop();
                        jaws[2].Stop();
                        score++;
                        label2.Text = "Score: " + score.ToString();
                        
                        resetShark();
                    }
                    else
                    {
                        jaws[1].Play();
                        //jaws[0].Stop();
                        jaws[2].Stop();
                    }
                }
                else 
                { 
                    jaws[2].Play();
                    jaws[1].Stop();
                    //jaws[0].Stop();
                }
            }
            else
            {
                jaws[2].Stop();
                jaws[1].Stop();
                //jaws[0].Stop();
            }

        }
        private void resetShark()
        {
            Random rand = new Random();
            //sharkX = rand.Next((int)(minx * 1000), (int)(maxx * 1000)) / 1000;
            //sharkY = rand.Next((int)(miny * 1000), (int)(maxy * 1000)) / 1000;

            sharkX = map(rand.NextDouble(), 0, 1, minx, maxx);
            sharkY = map(rand.NextDouble(), 0, 1, miny, maxy);
            

            isActive = true;
            
        }
        public static double map(double value, double from1, double to1, double from2, double to2)
        {

            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;

        }

  
    }
    


    public class LoopStream : WaveStream
    {
        WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.EnableLooping = true;
        }

        /// <summary>
        /// Use this to turn looping on or off
        /// </summary>
        public bool EnableLooping { get; set; }

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (sourceStream.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    // loop
                    sourceStream.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}
