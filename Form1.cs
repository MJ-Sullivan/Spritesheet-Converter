using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ImageMagick;

namespace GifMaker
{
    public partial class Form1 : Form
    {
        private bool _fileLoaded = false;
        private string _fileName;
        private int _lineNumber = 1;

        private int _imageWidth;
        private int _imageHeight;
        private double _aRatio;
        private double _zoom = 1;

        private int _frameWidth;
        private int _frameHeight;
        private Image _frameImage;

        private Image _image;

        Timer t = new Timer();

        int _frameRate = 5;

        int _numFrames;
        int _frameTracker;

        MagickImageCollection _gifCollection;
        string _saveName;

        public Form1()
        {
            InitializeComponent();

            this.AllowDrop = true;
            this.DragDrop += new DragEventHandler(DropFile);
            this.DragEnter += new DragEventHandler(DragFile);
            pictureBox1.MouseWheel += new MouseEventHandler(ScrollHandler);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            t.Tick += new EventHandler(t_Tick);
            t.Start();
        }

        private void Reset()
        {
            _fileLoaded = false;
            _fileName = "";

            _zoom = 1;

            _frameWidth = 0;
            _frameHeight = 0;

            _frameRate = 5;

            _numFrames = 0;
            _frameTracker = 0;

            _saveName = "";

            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;

            button1.Enabled = false;
            button3.Enabled = false;

            this.AllowDrop = true;
        }

        private void Reload()
        {
            _fileLoaded = false;

            _frameTracker = 0;

            LoadImage();
        }

        private void t_Tick(object sender, EventArgs e)
        {
            if (_fileLoaded)
            {
                t.Interval = 1000 / _frameRate;
                NextFrame();
            }
        }

        private void NextFrame()
        {
            _frameTracker++;
            if (_frameTracker >= _numFrames)
                _frameTracker = 0;

            _frameImage = ((Bitmap)_image).Clone(new Rectangle(0, _frameHeight * _frameTracker, _frameWidth, _frameHeight), _image.PixelFormat);

            DrawZoomedImage();
            
        }

        private void DrawZoomedImage()
        {
            Bitmap result = new Bitmap((int)(_frameWidth * _zoom), (int)(_frameHeight * _zoom));
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage(_frameImage, 0, 0, (int)(_frameWidth * _zoom), (int)(_frameHeight * _zoom));
            }

            pictureBox1.Image = result;
        }

        private void ConvertToGif()
        {
            _gifCollection = new MagickImageCollection();
            MagickImage image = new MagickImage(_fileName);

            var frames = image.CropToTiles(_frameWidth, _frameHeight);

            int frameNum = 0;

            foreach (MagickImage frame in frames)
            {
                frame.Page = new MagickGeometry(0, 0, 0, 0);
                _gifCollection.Add(frame);
                _gifCollection[frameNum].AnimationIterations = 0;   // GIF infinitely loops
                _gifCollection[frameNum].GifDisposeMethod = GifDisposeMethod.Background;

                _gifCollection[frameNum].AnimationDelay = 100 / _frameRate;

                frameNum++;
            }

            WriteToOutput("Spritesheet converted...");
            WriteToOutput("GIF : [" + _gifCollection.Count + " frames, " + _frameRate + " fps]");

            button3.Enabled = true;
        }

        private void SaveGif()
        {
            string[] presentFiles =  Directory.GetFiles(".\\GIFs");

            int duplicateNum = 0;
            string savePath = CleanFileName(_saveName);

            // If file already present with chosen name, appends number to name and checks again for file clashes, incrementing number until free name found.
            bool isNameFree = false;
            while (!isNameFree)
            {
                isNameFree = true;
                foreach (string file in presentFiles)
                {
                    string cleanedFileName = CleanFileName(file) + ".gif";
                    if (cleanedFileName == (savePath + ".gif"))
                    {
                        isNameFree = false;

                        duplicateNum++;
                        savePath = CleanFileName(_saveName) + "(" + duplicateNum + ")";
                        break;
                    }
                }
            }

            _gifCollection.Write(".\\GIFs\\" + savePath + ".gif");
            WriteToOutput("SAVE SUCCESS");
            WriteToOutput("Saved .gif file as \"" + savePath + ".gif\"");
        }

        private void WriteToOutput(string str)
        {
            richTextBox2.AppendText(_lineNumber + ": " + str + "\n");
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            _lineNumber++;
        }

        private bool CheckFileValid()
        {
            MagickImage testFile;

            try
            {

                using (testFile = new MagickImage(_fileName))
                { }
            } 
            catch (Exception e)
            {
                _fileName = "";
                WriteToOutput("Error : Failed to load file");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns file name with directory and file path removed
        /// </summary>
        /// <returns></returns>
        private string CleanFileName(string fileName)
        {
            string[] pathSections = fileName.Split('\\');
            string[] fileEnding = pathSections[pathSections.Length - 1].Split('.');
            string cleanedName = fileEnding[0];

            return cleanedName;
        }

        private void LoadImage()
        {
            _fileLoaded = true;
            pictureBox1.Visible = true;
            using (var tempImage = new Bitmap(_fileName))
            {
                _image = new Bitmap(tempImage);
            }

            this.AllowDrop = false;

            WriteToOutput("Image file successfully loaded");
            WriteToOutput("Dimensions : [" + _image.Width + ", " + _image.Height + "]");

            pictureBox1.Image = (Bitmap)_image.Clone();

            // If reloading, ignore resetting values
            if (textBox4.Text != CleanFileName(_fileName) + ".gif")
            {
                _frameWidth = pictureBox1.Image.Width;
                _imageWidth = pictureBox1.Image.Width;
                _frameHeight = pictureBox1.Image.Height;
                _imageHeight = pictureBox1.Image.Height;
                _numFrames = 1;
                _aRatio = _imageWidth / (double)_imageHeight;

                textBox1.Text = _frameWidth.ToString();
                textBox2.Text = _frameHeight.ToString();
                textBox3.Text = "5";

                // file name
                textBox4.Text = CleanFileName(_fileName) + ".gif";
            }

            // Enable user inputs
            textBox1.ReadOnly = false;
            textBox2.ReadOnly = false;
            textBox3.ReadOnly = false;
            textBox4.ReadOnly = false;

            button1.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
        }

        private void ResizeFrame()
        {
            try
            {
                int newWidth = Convert.ToInt32(textBox1.Text);
                if (newWidth <= _imageWidth && newWidth > 0)
                    _frameWidth = newWidth;
                else
                    _frameWidth = _imageWidth;
            }
            catch (Exception ex)
            {
                return;
            }
            try
            {
                int newHeight = Convert.ToInt32(textBox2.Text);
                if (newHeight <= _imageHeight && newHeight > 0)
                    _frameHeight = newHeight;
                else
                    _frameHeight = _imageHeight;
            }
            catch (Exception ex)
            {
                return;
            }

            Bitmap bmp = (Bitmap)_image;
            _frameImage = bmp.Clone(new Rectangle(0, 0, _frameWidth, _frameHeight), pictureBox1.Image.PixelFormat);

            DrawZoomedImage();

            if (_frameHeight > 0 && _frameHeight <= _imageHeight)
                _numFrames = _imageHeight / _frameHeight;
        }

        // Generate gif
        private void button1_Click(object sender, EventArgs e)
        {
            ConvertToGif();
        }

        private void DragFile(object sender, EventArgs e)
        {
            DragEventArgs args = (DragEventArgs)e;
            if(args.Data.GetDataPresent(DataFormats.FileDrop)) args.Effect = DragDropEffects.Copy;
        }

        private void DropFile(object sender, EventArgs e)
        {
            DragEventArgs args = (DragEventArgs)e;
            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);

            if (_fileLoaded)
                return;

            _fileName = files[0];

            WriteToOutput("Loading \"" + _fileName + "\"...");

            if (CheckFileValid())
            {
                LoadImage();
            }
        }

        // Load Spritesheet
        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName = openFileDialog1.FileName;

                WriteToOutput("Loading \"" + _fileName + "\"...");

                if (CheckFileValid())
                {
                    LoadImage();
                }
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        // Save gif
        private void button3_Click(object sender, EventArgs e)
        {
            SaveGif();
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        // Frame width
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ResizeFrame();
        }

        // Frame height
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            ResizeFrame();
        }

        // Framerate
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(textBox3.Text) > 0 && Convert.ToInt32(textBox3.Text) < 144)
                {
                    _frameRate = Convert.ToInt32(textBox3.Text);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void ScrollHandler(object sender, EventArgs e)
        {
            MouseEventArgs args = (MouseEventArgs)e;
            if (_fileLoaded)
            {
                if ((_zoom + args.Delta * 0.001) >= 1)
                {
                    _zoom += args.Delta * 0.001;
                }
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            _saveName = textBox4.Text;
        }

        // Reset button
        private void button4_Click(object sender, EventArgs e)
        {
            Reset();
            button4.Enabled = false;
            button5.Enabled = false;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = ".gif";
            pictureBox1.Hide();

        }

        // Reload button
        private void button5_Click(object sender, EventArgs e)
        {
            Reload();
        }
    }
}
