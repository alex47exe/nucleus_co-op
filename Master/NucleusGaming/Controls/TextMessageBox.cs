﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nucleus.Gaming
{
    public partial class TextMessageBox : Form
    {
        public string UserText => textBox1.Text;

        public TextMessageBox()
        {
            InitializeComponent();
            BackgroundImage = new Bitmap(Globals.Theme + "other_backgrounds.jpg");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }
    }
}
