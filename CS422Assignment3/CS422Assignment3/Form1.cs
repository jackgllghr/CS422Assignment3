﻿/* Jack Gallagher 69537058
 * 
 * CS422 Assignment 3
 * 
 * Move the Arduino arm to catch the shark! 
 * Listen to audio cues to determine how close you are.
 * 
 * The UI displays the cursor position in a panel, the xy coordinates of the cursor, and the score
 * 
 */

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
        int currentLoop;

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
            _serialPort.PortName = "COM6";
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.Two;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

            _serialPort.Open();
            
            //Set up audio outputs
            jaws[0] = new WaveOut();
            jaws[1] = new WaveOut();
            jaws[2] = new WaveOut();

            var jawsAudio1 = new WaveChannel32(new WaveFileReader("Sounds/Jaws3.wav"));
            jaws[0].Init(jawsAudio1);
            var jawsAudio2 = new LoopStream(new WaveFileReader("Sounds/Jaws2.wav"));
            jaws[1].Init(jawsAudio2);
            var jawsAudio3 = new LoopStream(new WaveFileReader("Sounds/Jaws1.wav"));
            jaws[2].Init(jawsAudio3);

            //Set the shark to a random position
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

        //On each tick of the timer check calculate the x and y coordinates of the arm, then check for collision
        private void timer1_Tick(object sender, EventArgs e)
        {
            angle1 = 0.5 * Math.PI * ((double)(Analog1 - 97)) / (511.0 - 97.0);
            angle2 = 0.5 * Math.PI * ((((double)(Analog2 - 477)) / (890.0 - 477.0)) - 1.0);
            x = (Math.Cos(angle1) + (Math.Cos(angle1 + angle2)));
            y = (Math.Sin(angle1) + (Math.Sin(angle1 + angle2)));

            //label1.Text = Analog1.ToString() + " " + Analog2.ToString() + " " + Button1.ToString();
            label1.Text = "X: "+x.ToString() + "\nY: " + y.ToString();
            //label1.Text = sharkX.ToString() + " " + sharkY.ToString();
            _serialPort.WriteLine("H");
            
            if(isActive) checkForShark(x, y);

            panel1.Invalidate();
        }
        //Paint the onscreen reticle
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Brush cm = new SolidBrush(Color.Black);
            Pen pm = new Pen(Color.Black, 3);

            int xx = (int)(panel1.Width * ((x - minx) / (maxx - minx)));

            int yy = (int)(panel1.Height * ((y - miny) / (maxy - miny)));

            e.Graphics.DrawEllipse(pm, new Rectangle(xx - 5, panel1.Height - (yy - 5), 10, 10));
        }
        //Check how close the cursor is to the shark
        private void checkForShark(double x, double y)
        {
            label2.Text = "Score: " + score.ToString();


            double dx = Math.Abs(x - sharkX);
            double dy = Math.Abs(y - sharkY);

            //Get distance(radius) between the cursor and shark
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            //If at a certain radius, play the first loop
            if (distance <= sharkRad2)
            {
                //if closer, play the faster loop
                if (distance <= sharkRad1)
                {
                    //if within a small radius, "catch" the shark, increment the score, play the audio and reset the shark
                    if (distance <= sharkRad0)
                    {
                        isActive = false;
                        jaws[0].Play();
                        jaws[1].Stop();
                        jaws[2].Stop();
                        score++;
                        pictureBox1.Visible = true;
                        label2.Text = "Score: " + score.ToString();
                        timer2.Start();
                        resetShark();
                    }
                    else
                    {
                        if (currentLoop != 1)
                        {
                            currentLoop = 1;
                            jaws[1].Play();
                            jaws[2].Stop();
                        }
                    }
                }
                else 
                {
                    if (currentLoop != 2)
                    {
                        currentLoop = 2;
                        jaws[2].Play();
                        jaws[1].Stop();
                    }
                }
            }
            else
            {
                jaws[2].Stop();
                jaws[1].Stop();
            }

        }
        //set the shark to random coordinates within the boundaries set by minx,maxx, miny,maxy
        private void resetShark()
        {
            Random rand = new Random();
            
            sharkX = map(rand.NextDouble(), 0, 1, minx, maxx);
            sharkY = map(rand.NextDouble(), 0, 1, miny, maxy);
            
        }
        //Map a value from a range to another range. 
        //Ex. map(2, 1, 3, 6, 10) = 8
        //The value 2 in the range 1-3 is mapped to the range 6-10 so it is 8
        public static double map(double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            isActive = true;
            jaws[0].Stop();
            pictureBox1.Visible = false;
            timer2.Stop();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            MessageBox.Show("Congrats, your score is: " + score.ToString());
            score = 0;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

    }
    

    //LoopStream for looping audio tracks
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
