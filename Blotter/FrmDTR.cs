using AppSystem.Class;
using AppSystem.model;
using DPFP.Capture;
using DPFP.Verification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using MessagingToolkit.QRCode.Codec.Data;
using System.Drawing.Imaging;
using ZXing.Presentation;
using ZXing;
namespace AppSystem
{
    public partial class FrmDTR : Form, DPFP.Capture.EventHandler
    {
       
        private string PhoneNo;
        Capture capture = new Capture();
        Verification verify = new Verification();
        List<sp_biometric_loginResult> biometrics = new List<sp_biometric_loginResult>();
        private FilterInfoCollection captureDevice;
        private VideoCaptureDevice finalFrame;


       
        public FrmDTR()
        {
            InitializeComponent();
            capture.StartCapture();
            capture.EventHandler = this;
        }

        private void lblName_Click(object sender, EventArgs e)
        {

        }


        void SaveDtr()
        {

            labelNotification.Text = "";
            DataClasses1DataContext db = new DataClasses1DataContext(Tool.ConnectionString);

            var item = db.getSchedule(this.Tag.ToString()).FirstOrDefault();
            var c = db.CheckDTRifLOGGED(this.Tag.ToString()).FirstOrDefault();

            string[] scheduleArray = item.Column1.ToString().Split('/');
            string TimeIn = scheduleArray[0];
            string TimeOut = scheduleArray[1];
            //string approve = scheduleArray[2];

            string remark = "";

            DateTime dt_TimeIn = Convert.ToDateTime(TimeIn, CultureInfo.InvariantCulture);
            DateTime dt_TimeOut = Convert.ToDateTime(TimeOut, CultureInfo.InvariantCulture);

            string identifier = DateTime.Now.ToString("tt");
            int t = Convert.ToInt32(dt_TimeIn.ToString("HHmm"));
            int o = Convert.ToInt32(dt_TimeOut.ToString("HHmm"));
            int current = Convert.ToInt32(DateTime.Now.ToString("HHmm"));

            //if (identifier.Contains("AM"))
            //{

            //if (current > t)
            //{
            //    remark = "Late";
            //}

            //db.DtrINSERT(this.Tag.ToString(), remark);

            //}
            //else
            //{

            if (c.c <= 0)
            {
                if (current > t)
                {
                    remark = "Late";
                }

                db.DtrINSERT(this.Tag.ToString(), remark);
            }
            else
            {
                var requestCount = db.lookupRequest(this.Tag.ToString()).FirstOrDefault();
                if (current < o && requestCount.c.Equals(0))
                {
                    remark = "Early Out";
                    labelNotification.Text = "Too early to log-out, Please ask permission  to your  superior";
                    db.ExecuteCommand("INSERT INTO Request (Account, [For]) VALUES ({0}, {1})", this.Tag.ToString(), remark);
                }
                else
                {

                    db.DtrINSERT(this.Tag.ToString(), remark);
                }
            }


        }



        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            foreach (var i in biometrics)
            {
                if (FingerPrintScanner.VerificationResult(Sample, verify, i.biometrics.ToArray()).Verified)
                {
                    capture.StopCapture();
                    capture.StartCapture();
                    Invoke(new Action(() =>
                    {
                        if (this.Tag != null)
                        {

                            if (i.cn == Convert.ToInt32(this.Tag.ToString()))
                            {
                                SaveDtr();
                                DailyTimeRecordSELECT();

                                new Messaging().sendSms(PhoneNo.Replace("-", ""), lblName.Text + " \n Time: " + DateTime.Now);

                                lblUseFingerprint.Text = "Confirmed.";
                                this.Tag = null;
                            }
                        }


                    }));

                    //Insert DTR 


                }
            }
        }


        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {

        }

        void findAccountByRFID(string qrCode = "")
        {
            try
            {
                DataClasses1DataContext db = new DataClasses1DataContext(Tool.ConnectionString);
                var i = db.AccountSelectByRFID(qrCode).FirstOrDefault();
                this.Tag = i.cn;

                pb.Image = Tool.bytetoimage(i.AVATAR.ToArray());
                lblName.Text = i.AccountName;
                lblLrn.Text = i.LRN;
                lblGradeSection.Text = i.GradeSection_;
                PhoneNo = i.PhoneNo;
                lblUseFingerprint.Visible = true;
                lblUseFingerprint.Text = "Scan Fingerprint to confirm.";

            }
            catch (Exception e)
            {
                lblUseFingerprint.Visible = false;
                Console.WriteLine(e.Message);
                return;
            }


        }




        private void FrmDTR_Load(object sender, EventArgs e)
        {
            this.ActiveControl = txtRfidScan;
            Process.Start(Tool.RFID_Reader_Path);

            try
            {
                biometrics = new DataClasses1DataContext(Tool.ConnectionString).sp_biometric_login().ToList();

            }
            catch (Exception)
            {
                new frm_server().ShowDialog();
            }







            txtRfidScan.Focus();

            DailyTimeRecordSELECT();



            captureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in captureDevice)
            {
                comboBox1.Items.Add(Device.Name);
            }

            comboBox1.SelectedIndex = 0;
            finalFrame = new VideoCaptureDevice();




            finalFrame = new VideoCaptureDevice(captureDevice[comboBox1.SelectedIndex].MonikerString);
            finalFrame.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
            finalFrame.Start();


            timer1.Enabled = true;

        }



        void DailyTimeRecordSELECT()
        {

            if (this.Tag != null)
            {


                List<dtr> dtrList = new List<dtr>();
                DataClasses1DataContext db = new DataClasses1DataContext(Tool.ConnectionString);
                var list = db.DailyTimeRecordSELCT(this.Tag.ToString()).ToList();





                foreach (var i in list)
                {
                    string timeout = Convert.ToDateTime(i.TimeOUT).ToShortTimeString();
                    if (timeout == "12:00 AM")
                    {
                        timeout = "";
                    }

                    if (!dtrList.Any(t => t.AccountName == i.AccountName))
                    {
                        dtrList.Add(new dtr
                        {
                            RecID = i.RecID,
                            AccountName = i.AccountName,
                            TimeIN = i.TimeIN,
                            TimeOUT = i.TimeOUT,
                            Total = i.Total.ToString(),
                            RowRemark = i.RowRemark,
                            DateCreated = i.DateCreated
                        });
                    }





                }
                dg_DTR.Refresh();
                dg_DTR.DataSource = dtrList;
            }
        }

        private void FrmDTR_FormClosing(object sender, FormClosingEventArgs e)
        {
            (Application.OpenForms["Main"] as Main).Show();

            Process[] GetPArry = Process.GetProcesses();
            foreach (Process testProcess in GetPArry)
            {
                string ProcessName = testProcess.ProcessName;

                ProcessName = ProcessName.ToLower();
                if (ProcessName.CompareTo("nfcUID") == 0)
                    testProcess.Kill();
            }


            if (finalFrame.IsRunning == true)
            {
                finalFrame.Stop();
            }



        }

        private void dg_DTR_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtRfidScan_TextChanged(object sender, EventArgs e)
        {
            if (txtRfidScan.Text.Length == 8)
            {
                findAccountByRFID(txtRfidScan.Text);
                txtRfidScan.Clear();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTime.Text = DateTime.Now.ToLongTimeString();


            if (pictureBox1.Image != null)
            {
                ZXing.BarcodeReader Reader = new ZXing.BarcodeReader();
                Result result = Reader.Decode((Bitmap)pictureBox1.Image);
                try
                {
                    string qrCode = result.ToString().Trim();
                    if (qrCode != "")
                    {
                        timer1.Enabled = false;
                    

                        Invoke(new Action(() =>
                        {

                            findAccountByRFID(qrCode);

                            
                                    SaveDtr();
                                    DailyTimeRecordSELECT();

                                    new Messaging().sendSms(PhoneNo.Replace("-", ""), lblName.Text + " \n Time: " + DateTime.Now);

                                    lblUseFingerprint.Text = "Confirmed.";
                                    this.Tag = null;
                                
                      


                        }));




                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }

            }




        }

        private void lblUseFingerprint_Click(object sender, EventArgs e)
        {

        }

        private void FrmDTR_FormClosed(object sender, FormClosedEventArgs e)
        {
            //capture.StopCapture();
        }

        private void lblTime_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveDtr();
            DailyTimeRecordSELECT();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            findAccountByRFID(textBox1.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dg_DTR.Refresh();
        }



        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }



    }
}
