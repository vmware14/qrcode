using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppSystem
{
    public partial class frm_qrEncode : Form
    {
        frm_user frmUser;
        public frm_qrEncode(frm_user u)
        {
            InitializeComponent();
            frmUser = u;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            frmUser.txtQrCode.Text = textBox1.Text;
            MessagingToolkit.QRCode.Codec.QRCodeEncoder encoder = new MessagingToolkit.QRCode.Codec.QRCodeEncoder();
            encoder.QRCodeScale = 8;
            Bitmap bmp = encoder.Encode(textBox1.Text);
            pictureBox1.Image = bmp;



        }

        private void cmdExportQr_Click(object sender, EventArgs e)
        {
           
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "JPEG|*.jpg", ValidateNames = true })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                  
                    Bitmap bmp = new Bitmap(pictureBox1.Image);                   
                    bmp.Save(sfd.FileName, ImageFormat.Jpeg);
                }
            }
        }
    }
}
