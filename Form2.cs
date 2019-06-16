using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;


namespace tnt_sender
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //string[] values = textBox1.Text.ToString().Split(':');
            IntPtr hwnd = (IntPtr)Convert.ToInt32(textBox1.Text); //values[1].ToString()
            my_graphics.MakeScreen(hwnd, "screen_text.png");
            pictureBox1.Load("screen_text.png");
         }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //MessageBox.Show();
            Graphics g = pictureBox1.CreateGraphics();
            // g.DrawEllipse(new Pen(Brushes.Red, 1), e.X, e.Y, 5,5 );
            Bitmap img = new Bitmap(pictureBox1.Image);
            Bitmap img2 = new Bitmap(100, 20);
            Color pixel = img.GetPixel(e.X, e.Y);

            textBox3.Text = e.X.ToString() + "," + e.Y.ToString();
            int color_counter = 0;

            MessageBox.Show(color_counter.ToString());
            CopyImageRect(e.X, e.Y);

            textBox_template.Text = "!" + e.X.ToString() + "|" + e.Y.ToString() + "|" + textBox_fname.Text + "|2000";
        }


        private void CopyImageRect(int x, int y)
        {
            Rectangle rectangle = new Rectangle(x, y, 100, 20);
            var pic = (Bitmap)pictureBox1.Image;
            pictureBox2.Image = pic.Clone(rectangle, PixelFormat.Format16bppRgb555);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            pictureBox3.Load("images\\step3.png");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            pictureBox2.Image.Save("images\\" + textBox_fname.Text);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }



        private void button1_Click(object sender, EventArgs e)
        {
            

        }
    }
}
