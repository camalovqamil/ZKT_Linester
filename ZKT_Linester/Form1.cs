using BioMetrixCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZKT_Linester
{
    public partial class Main : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public Main()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        SqlConnection baglanti = new SqlConnection(ConfigurationManager.ConnectionStrings["IshCedveliConnectionString"].ConnectionString);
        SqlCommand komut = new SqlCommand();
        SqlCommand komut2 = new SqlCommand();

        void baglan()
        {
            if (baglanti.State == ConnectionState.Closed)
            {
                baglanti.Open();
            }
            else
            {
                baglanti.Close();
                baglanti.Open();
            }
        }

        DeviceManipulator manipulator = new DeviceManipulator();
        public ZkemClient objZkeeper;
        private bool isDeviceConnected = false;

        public bool IsDeviceConnected
        {
            get { return isDeviceConnected; }
            set
            {
                isDeviceConnected = value;
                if (isDeviceConnected)
                {
                    ShowStatusBar("Cihaza bağlanti uğurlu oldu !!", true);
                    btnBaglan.Enabled = false;
                    btnLogYukle.Enabled = true;
                    //btnConnect.Text = "Disconnect";
                    //ToggleControls(true);
                }
                else
                {
                    ShowStatusBar("The device is diconnected !!", true);
                    objZkeeper.Disconnect();
                    
                }
            }
        }

        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        //ShowStatusBar("The device is switched off", true);
                        //DisplayEmpty();
                        //btnConnect.Text = "Connect";
                        //ToggleControls(false);
                        break;
                    }

                default:
                    break;
            }

        }

        private void btnBaglan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!backgroundWorkerBaglan.IsBusy)
            {            
                backgroundWorkerBaglan.RunWorkerAsync();
            }            
        }

        public void ShowStatusBar(string message, bool type)
        {
            if (message.Trim() == string.Empty)
            {
                txtMesaj.Text = "";
                return;
            }

            
            txtMesaj.Text = message;
            txtMesaj.ForeColor = Color.White;

            if (type)
                txtMesaj.BackColor = Color.FromArgb(79, 208, 154);
            else
                txtMesaj.BackColor = Color.FromArgb(230, 112, 134);
        }

        private void btnLogYukle_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!backgroundWorkerLoglariYukle.IsBusy)
            {
                backgroundWorkerLoglariYukle.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Prosses davam edir! ");
            }

            //while (this.backgroundWorkerLoglariYukle.IsBusy)
            //{
            //    progressBar1.Increment(1);
            //    //Application.DoEvents();
            //}
        }

        private void backgroundWorkerBaglan_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                ShowStatusBar(string.Empty, true);

                if (IsDeviceConnected)
                {
                    IsDeviceConnected = false;
                    this.Cursor = Cursors.Default;

                    return;
                }

                string ipAddress = txtIP.Text.Trim();
                string port = txtPort.Text.Trim();
                if (ipAddress == string.Empty || port == string.Empty)
                    throw new Exception("Cihazın IP ünvanı və Portu məcburidir !!");

                int portNumber = 4370;
                if (!int.TryParse(port, out portNumber))
                    throw new Exception("Port nömrəsi etibarlı deyil");

                bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
                if (!isValidIpA)
                    throw new Exception("Cihaz IP-si etibarsızdır !!");

                isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
                if (!isValidIpA)
                    throw new Exception("Bu cihaz " + ipAddress + ":" + port + " cavab vermədi!!");

                objZkeeper = new ZkemClient(RaiseDeviceEvent);
                IsDeviceConnected = objZkeeper.Connect_Net(ipAddress, portNumber);

                if (IsDeviceConnected)
                {
                    string deviceInfo = manipulator.FetchDeviceInfo(objZkeeper, int.Parse(txtMN.Text.Trim()));
                    txtCihazMelumati.Text = deviceInfo;
                }

            }
            catch (Exception ex)
            {
                ShowStatusBar(ex.Message, false);
            }
            this.Cursor = Cursors.Default;         
        }

       

        private void backgroundWorkerLoglariYukle_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                ShowStatusBar(string.Empty, true);

                ICollection<MachineInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, int.Parse(txtMN.Text.Trim()));

                if (lstMachineInfo != null && lstMachineInfo.Count > 0)
                {
                    ShowStatusBar(lstMachineInfo.Count + " sayda sətir tapıldı !", true);
                    progressBar1.Maximum = lstMachineInfo.Count;
                    gridControl1.DataSource = lstMachineInfo;

                    User_View();
                }
                else
                    ShowStatusBar("Heç bir sətir tapılmadı !", true);
            }
            catch (Exception ex)
            {
                ShowStatusBar(ex.Message, true);
            }
        }

        List<int> UserID = new List<int>();
        List<DateTime> ChekTime = new List<DateTime>();
        private void User_View()
        {
            UserID.Clear();
            ChekTime.Clear();
            progressBar1.Value = 0;

            baglan();
            komut.Connection = baglanti;
            try
            {
                for (int i = 0; i < gridView1.RowCount; i++)
                {
                    komut.CommandText = "select *from USERINFO where BADGENUMBER = '" + Convert.ToInt32(gridView1.GetRowCellValue(i, ("IndRegID"))) + "'";
                    IDataReader dr = komut.ExecuteReader();
                    if (dr.Read())
                    {
                        UserID.Add(Convert.ToInt32(dr["USERID"].ToString()));
                        ChekTime.Add(Convert.ToDateTime(gridView1.GetRowCellValue(i, "DateTimeRecord").ToString()));
                    }
                    dr.Close();
                    komut.Dispose();
                    progressBar1.Value = i;
                }

                MessageBox.Show(gridView1.RowCount.ToString() + " sayda sətir hazırdır !");
                btnYaddaSaxla.Enabled = true;
            }
            catch (Exception xx)
            {
                MessageBox.Show(xx.Message);
            }

        }

        private void btnYaddaSaxla_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!backgroundWorkerLoguYaddaSaxla.IsBusy)
            {
                backgroundWorkerLoguYaddaSaxla.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Prosses davam edir! ");
            }

        }

        private void backgroundWorkerLoguYaddaSaxla_DoWork_1(object sender, DoWorkEventArgs e)
        {
            progressBar1.Value = 0;
            int k = 0;
           
            baglan();
            komut.Connection = baglanti;
            try
            {
                for (int i = 0; i < gridView1.RowCount; i++)
                {
                    komut2.Connection = baglanti;
                    komut2.CommandText = "select *from CHECKINOUT where USERID='" + UserID[i] + "' and CHECKTIME='" + ChekTime[i].ToString("yyyy-MM-dd HH:mm:ss") + "'";
                    IDataReader dr = komut2.ExecuteReader();
                    if (!dr.Read())
                    {
                        dr.Close();
                        komut2.Dispose();

                        komut.CommandText = @"INSERT INTO [dbo].[CHECKINOUT]
                                                           ([USERID]
                                                           ,[CHECKTIME])
                                                     VALUES
                                                           ('" + UserID[i] + "', '" + ChekTime[i].ToString("yyyy-MM-dd HH:mm:ss") + "')";

                        komut.ExecuteNonQuery();
                        komut.Dispose();
                        k++;
                    }
                    else
                    {
                        dr.Close();
                        komut2.Dispose();
                    }

                    progressBar1.Value = i;
                }

                MessageBox.Show(k.ToString() + " sayda yeni sətir yadda saxlandı !");
                btnYaddaSaxla.Enabled = true;
            }
            catch (Exception xx)
            {

            }
        }
    }
}
