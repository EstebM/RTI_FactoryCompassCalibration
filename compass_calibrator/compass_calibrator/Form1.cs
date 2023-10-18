using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Globalization;

#pragma warning disable IDE1006

namespace compass_calibrator
{
    public partial class Form1 : Form
    {
        static string string_from_usart = "";
        static bool GotData = false;
        static Boolean port_close_flag;
        double MagXvalue, MagYvalue, MagZvalue;
        double AccXvalue, AccYvalue, AccZvalue;
        int empty_serial_data_counter;
        string curDir;
        string portName;
        string symbol_mask = "1234567890.-";

        bool TimerEnabled = false;
        bool cleared = true;
        //static bool readTillEmpty = true;

        public Form1()
        {
            InitializeComponent();
        }

        private static string SaveTextFile(string DirName, string FilName, string strData, bool ShowError, bool Append)
        {
            DirectoryInfo di = new DirectoryInfo(DirName);
            string str = "";

            try
            {
                if (di.Exists)// Determine whether the directory exists.
                {
                    //di.Delete();// Delete the directory.
                }
                else// Try to create the directory.
                {
                    di.Create();
                }
            }
            catch
            {
                if (ShowError)
                    MessageBox.Show("No Directory. Can't save Text data!");
            }
            finally
            {
                str = DirName + "\\" + FilName;
                try
                {
                    if (Append)
                        System.IO.File.AppendAllText(str, strData);
                    else
                        System.IO.File.WriteAllText(str, strData);
                }
                catch (System.Exception ex)
                {
                    if (ShowError)
                        MessageBox.Show("Can't save Text data! " + str + " " + String.Format("exception: {0}", ex.GetType().ToString()));
                }
            }
            return str;
        }
        private static string ReadTextFile(string FilName)
        {
            String s = "";
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(FilName);
                s = sr.ReadToEnd();
                //close the file
                sr.Close();
            }
            catch { }

            return s;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //change regional standard to en-US
            CultureInfo en = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = en;

            this.Text = "Factory Compass Calibration 1.3";

            //current dir name
            curDir = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);

            stop_button.Enabled = false;
            point_buttons_enable(false);

            //Load previous comm port
            string FilName = curDir + "\\Setup.txt";
            portName = ReadTextFile(FilName);

            int item = 0;
            int selecteditem = 0;
            foreach (string s in SerialPort.GetPortNames())
            {
                item++;
                comboBox1.Items.Add(s);
                if (portName == s)
                {
                    comboBox1.Text = portName;
                    selecteditem = item;
                }
            }
            if(selecteditem > 0)
            {
                comboBox1.SelectedItem = selecteditem;
                start_button_Click(sender, e);
            }

            listBoxInitializeHardware.SelectedIndex = 1;
            textBoxSelectedOrientation.Text = listBoxInitializeHardware.SelectedItem.ToString();

            //groupBox1.Enabled = false;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //MagMaster.Form2.Close(sender, e);

            //MagMaster.Form2.Fclose(sender, e);

            MagMaster.Form2 frm_op = new MagMaster.Form2();
            frm_op.Owner = this;
            //frm_op.ShowDialog();
            frm_op.Fclose(sender, e);

            string DirName = curDir;
            string FilName = "Setup.txt";
            string s = comboBox1.Text;
            SaveTextFile(DirName, FilName, s, true, false);
            close_port();
        }

        private void point_buttons_enable(Boolean b_enable)
        {
            if (b_enable)
            {
                buttonXplus_0.Enabled = true;
                buttonXplus_180.Enabled = true;
                buttonXminus_0.Enabled = true;
                buttonXminus_180.Enabled = true;
                buttonYplus_0.Enabled = true;
                buttonYplus_180.Enabled = true;
                buttonYminus_0.Enabled = true;
                buttonYminus_180.Enabled = true;
                buttonZplus_0.Enabled = true;
                buttonZplus_180.Enabled = true;
                buttonZminus_0.Enabled = true;
                buttonZminus_180.Enabled = true;
            }
            else
            {
                buttonXplus_0.Enabled = false;
                buttonXplus_180.Enabled = false;
                buttonXminus_0.Enabled = false;
                buttonXminus_180.Enabled = false;
                buttonYplus_0.Enabled = false;
                buttonYplus_180.Enabled = false;
                buttonYminus_0.Enabled = false;
                buttonYminus_180.Enabled = false;
                buttonZplus_0.Enabled = false;
                buttonZplus_180.Enabled = false;
                buttonZminus_0.Enabled = false;
                buttonZminus_180.Enabled = false;
            }
        }
        private void SendCom(SerialPort Port, string txt)
        {
            if (Port.IsOpen)
            {
                string message1 = txt + '\r';
                //string message3;

                try
                {   
                    Port.Write(message1);
                }
                catch { }// (System.Exception ex)
                //{
                //    message3 = String.Format("caughtC: {0}", ex.GetType().ToString());
                //}
            }
        }

        private bool ADCPStarted = false;
        private void start_button_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    port.PortName = comboBox1.SelectedItem.ToString();
                    //port opening
                    port_close_flag = false;
                    port.ReadTimeout = 20;
                    port.Open();
                    port.BaudRate = 115200;

                    stop_button.Enabled = true;
                    point_buttons_enable(true);
                    start_button.Enabled = false;
                    comboBox1.Enabled = false;
                    //for the handler
                    port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    //timer activation
                    timer1.Enabled = true;
                    TimerEnabled = true;
                        //timer1.Interval = 200;
                        //Application.DoEvents();
                }
                else
                {
                    textBoxStatus.Text = "Serial Port Not Selected";
                }
            }
            catch
            {
                close_port();
                MessageBox.Show("Selected port in use", "Warning!");
            }
        }
        private void stop_button_Click(object sender, EventArgs e)
        {
            close_port();
        }

        void close_port()
        {   
            //timer1.Enabled = false;
            TimerEnabled = false;
            //port closing
            port_close_flag = true;
            stop_button.Enabled = false;
            point_buttons_enable(false);
            Thread.Sleep(500);
            port.Close();
            start_button.Enabled = true;
            comboBox1.Enabled = true;
            empty_serial_data_counter = 0;
            string_from_usart = "";
            GotData = false;
            MagXvalue = 0;
            MagYvalue = 0;
            MagZvalue = 0;
            AccXvalue = 0;
            AccYvalue = 0;
            AccZvalue = 0;
            Xlabel.Text = "X = " + MagXvalue.ToString("0.####");
            Ylabel.Text = "Y = " + MagYvalue.ToString("0.####");
            Zlabel.Text = "Z = " + MagZvalue.ToString("0.####");
            labelAccX.Text = "X = " + AccXvalue.ToString("0.####");
            labelAccY.Text = "Y = " + AccYvalue.ToString("0.####");
            labelAccZ.Text = "Z = " + AccZvalue.ToString("0.####");
            ADCPStarted = false;
            textBoxStatus.Text = "Disconnected";
            button1.BackColor = Color.FromArgb(255, 240, 240, 240);//= Color.White;
            button2.BackColor = Color.FromArgb(255, 240, 240, 240);//Color.White;
        }
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            bool gotit = false;
            try
            {
                if (!port_close_flag)
                {
                    SerialPort sp = (SerialPort)sender;
                    string indata = sp.ReadLine();
                    gotit = true;
                    string_from_usart = indata;

                    GotData = true;

                    indata = sp.ReadLine();
                    string_from_usart += indata;
                }
            }
            catch 
            {
                if(!gotit)
                    GotData = false;
            }
        }
        private void var_refresh()
        {   
            try
            {   
                if (GotData)
                {   
                    try
                    {
                        GotData = false;
                        string str = string_from_usart;
                        string[] serial_values = str.Split(',');
                        if (serial_values[0] != "" && serial_values[1] != "" && serial_values[2] != ""
                         && serial_values[3] != "" && serial_values[4] != "" && serial_values[5] != "")
                        {
                            try
                            {
                                MagXvalue = double.Parse(serial_values[0]);
                                MagYvalue = double.Parse(serial_values[1]);
                                MagZvalue = double.Parse(serial_values[2]);
                                AccXvalue = double.Parse(serial_values[3]);
                                AccYvalue = double.Parse(serial_values[4]);
                                AccZvalue = double.Parse(serial_values[5]);

                                empty_serial_data_counter = 0;
                                textBoxStatus.Text = "Connected";
                            }
                            catch 
                            {
                                GotData = false;
                            }
                        }
                    }
                    catch 
                    {
                        GotData = false;
                    }
                }
                else
                {
                    if (string_from_usart.Contains("Timeout"))
                    {
                        textBoxStatus.Text = "IMU Timeout";
                        empty_serial_data_counter = 0;
                    }
                    else
                    {
                        empty_serial_data_counter++;
                        if (empty_serial_data_counter >= 20)
                        {
                            textBoxStatus.Text = "No serial data";
                            textBoxSN.Text = "NA";
                            ADCPStarted = false;
                            //close_port();
                            //MessageBox.Show("No serial data.", "Warning!");
                            //ADCPStarted = false;
                            empty_serial_data_counter = 0;
                        }
                    }
                }
            }
            catch
            {
                textBoxStatus.Text = "Serial port error";
                close_port();
                //MessageBox.Show("Serial port error.", "Warning!");
                ADCPStarted = false;
            }
        }

        private void indication()
        {
            Xlabel.Text = "X = " + MagXvalue.ToString("0.####");
            Ylabel.Text = "Y = " + MagYvalue.ToString("0.####");
            Zlabel.Text = "Z = " + MagZvalue.ToString("0.####");
            labelAccX.Text = "X = " + AccXvalue.ToString("0.####");
            labelAccY.Text = "Y = " + AccYvalue.ToString("0.####");
            labelAccZ.Text = "Z = " + AccZvalue.ToString("0.####");
        }
        //Application.DoEvents();
        int TimerState = 0;
        int TimerStatus = -2;

        int tcount = 20;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (TimerEnabled)
            {
                if (!ADCPStarted)
                {
                    TimerState = 0;
                    //groupBox1.Enabled = false;
                    tcount = 20;
                }
                switch (TimerState)
                {
                    case 0:
                        empty_serial_data_counter = 0;
                        textBoxStatus.Text = "Starting ADCP";
                        textBoxSN.Text = "NA";
                        //port.DiscardOutBuffer();
                        //port.DiscardInBuffer();

                        ADCPStarted = true;
                        string_from_usart = "";
                        SendCom(port, "STOP");
                        TimerState = 1;
                        break;
                    case 1:
                        tcount--;
                        if (string_from_usart.Contains("STOP" + '\u0006'))
                        {
                            TimerState = 10;
                            ADCPStarted = true;
                            tcount = 0;
                        }
                        else
                        {   
                            if (tcount < 0)
                            {
                                textBoxStatus.Text = "ADCP Not Detected";
                                textBoxSN.Text = "NA";
                                ADCPStarted = false;
                            }
                        }
                        break;
                    case 10:
                        textBoxStatus.Text = "Downloading ADCP SN";
                        SendCom(port, "ENGBR050C00");
                        int cnt = 50;
                        while(!string_from_usart.Contains("SER#"))
                        {
                            cnt--;
                            if (cnt < 0)
                                break;
                            System.Threading.Thread.Sleep(100);
                        }
                        //System.Threading.Thread.Sleep(1000);

                        //ENGBR050C00+
                        //35 30 31 30 33 2D 30 30 20 52 45 56 3A 58 44 20 53 45 52 23 30 31 30 38 20 20 20 20 20 20 20 20 
                        //50103-00 REV:XD SER#0108
                        if (string_from_usart.Contains("SER#"))
                        {
                            textBoxStatus.Text = "Connected";
                            int i = string_from_usart.IndexOf("SER#");
                            i += 4;
                            string SN = string_from_usart.Substring(i,4);

                            try
                            {
                                int iSN = Convert.ToInt32(SN);
                                textBoxSN.Text = iSN.ToString();//SN;
                            }
                            catch
                            {
                                textBoxSN.Text = SN;
                            }

                            string_from_usart = "";
                            SendCom(port, "ENGATT 2");

                            ADCPStarted = true;
                            TimerState = 12;
                            //groupBox1.Enabled = true;
                        }
                        else
                        {
                            //TimerEnabled = false;
                            textBoxSN.Text = "NA";
                            ADCPStarted = false;
                            //MessageBox.Show("Serial Number Not Set","Warning");
                            //TimerEnabled = true;
                        }
                        break;
                    case 11:
                        break;
                    case 12:
                        var_refresh();
                        indication();
                        break;
                }
                bool ok = false;
                if (textBoxStatus.Text == "Connected")
                    ok = true;
                TimerStatus++;
                if (TimerStatus > 2)
                    TimerStatus = -2;
                if (TimerStatus > 0)
                {
                    if(ok)
                        button1.BackColor = Color.Green;
                    else
                        button1.BackColor = Color.Red;
                    button2.BackColor = Color.White;
                }
                else
                {
                    if (ok)
                        button2.BackColor = Color.Green;
                    else
                        button2.BackColor = Color.Red;
                    button1.BackColor = Color.White;
                }
            }
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }    
        }
        private void buttonXplus_0_Click(object sender, EventArgs e)
        {
            textBoxXplus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxXplus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxXplus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccXplus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccXplus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccXplus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonXplus_180_Click(object sender, EventArgs e)
        {
            textBoxXplus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxXplus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxXplus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccXplus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccXplus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccXplus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonXminus_0_Click(object sender, EventArgs e)
        {
            textBoxXminus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxXminus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxXminus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccXminus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccXminus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccXminus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonXminus_180_Click(object sender, EventArgs e)
        {
            textBoxXminus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxXminus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxXminus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccXminus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccXminus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccXminus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonYplus_0_Click(object sender, EventArgs e)
        {
            textBoxYplus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxYplus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxYplus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccYplus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccYplus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccYplus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonYplus_180_Click(object sender, EventArgs e)
        {
            textBoxYplus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxYplus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxYplus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccYplus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccYplus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccYplus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonZplus_0_Click(object sender, EventArgs e)
        {
            textBoxZplus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxZplus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxZplus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccZplus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccZplus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccZplus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonZplus_180_Click(object sender, EventArgs e)
        {
            textBoxZplus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxZplus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxZplus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccZplus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccZplus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccZplus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonYminus_0_Click(object sender, EventArgs e)
        {
            textBoxYminus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxYminus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxYminus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccYminus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccYminus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccYminus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonYminus_180_Click(object sender, EventArgs e)
        {
            textBoxYminus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxYminus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxYminus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccYminus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccYminus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccYminus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonZminus_0_Click(object sender, EventArgs e)
        {
            textBoxZminus_X_0.Text = MagXvalue.ToString("0.###");
            textBoxZminus_Y_0.Text = MagYvalue.ToString("0.###");
            textBoxZminus_Z_0.Text = MagZvalue.ToString("0.###");
            textBoxAccZminus_X_0.Text = AccXvalue.ToString("0.###");
            textBoxAccZminus_Y_0.Text = AccYvalue.ToString("0.###");
            textBoxAccZminus_Z_0.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void buttonZminus_180_Click(object sender, EventArgs e)
        {
            textBoxZminus_X_180.Text = MagXvalue.ToString("0.###");
            textBoxZminus_Y_180.Text = MagYvalue.ToString("0.###");
            textBoxZminus_Z_180.Text = MagZvalue.ToString("0.###");
            textBoxAccZminus_X_180.Text = AccXvalue.ToString("0.###");
            textBoxAccZminus_Y_180.Text = AccYvalue.ToString("0.###");
            textBoxAccZminus_Z_180.Text = AccZvalue.ToString("0.###");
            ClearAll();
        }

        private void ClearAll()
        {
            textBox_matrixX_x.Clear();
            textBox_matrixX_y.Clear();
            textBox_matrixX_z.Clear();
            textBox_matrixY_x.Clear();
            textBox_matrixY_y.Clear();
            textBox_matrixY_z.Clear();
            textBox_matrixZ_x.Clear();
            textBox_matrixZ_y.Clear();
            textBox_matrixZ_z.Clear();
            textBox_biasX.Clear();
            textBox_biasY.Clear();
            textBox_biasZ.Clear();

            textBoxAcc_matrixX_x.Clear();
            textBoxAcc_matrixX_y.Clear();
            textBoxAcc_matrixX_z.Clear();
            textBoxAcc_matrixY_x.Clear();
            textBoxAcc_matrixY_y.Clear();
            textBoxAcc_matrixY_z.Clear();
            textBoxAcc_matrixZ_x.Clear();
            textBoxAcc_matrixZ_y.Clear();
            textBoxAcc_matrixZ_z.Clear();
            textBoxAcc_biasX.Clear();
            textBoxAcc_biasY.Clear();
            textBoxAcc_biasZ.Clear();

            buttonWriteMagValues.Enabled = false;
            buttonWriteAccValues.Enabled = false;

            cleared = true;
        }
        private void CalculateButton_Click(object sender, EventArgs e)
        {
            cleared = false;
            try
            {
                calculate_transformation_matrix(true);
                buttonWriteMagValues.Enabled = true;
            }
            catch
            {
                textBox_matrixX_x.Clear();
                textBox_matrixX_y.Clear();
                textBox_matrixX_z.Clear();
                textBox_matrixY_x.Clear();
                textBox_matrixY_y.Clear();
                textBox_matrixY_z.Clear();
                textBox_matrixZ_x.Clear();
                textBox_matrixZ_y.Clear();
                textBox_matrixZ_z.Clear();
                textBox_biasX.Clear();
                textBox_biasY.Clear();
                textBox_biasZ.Clear();
                MessageBox.Show("Incorrect Magnetometer input data!", "Warning!");
                buttonWriteMagValues.Enabled = false;
            }
            try
            {
                calculate_transformation_matrix(false);
                buttonWriteAccValues.Enabled = true;
            }
            catch
            {
                textBoxAcc_matrixX_x.Clear();
                textBoxAcc_matrixX_y.Clear();
                textBoxAcc_matrixX_z.Clear();
                textBoxAcc_matrixY_x.Clear();
                textBoxAcc_matrixY_y.Clear();
                textBoxAcc_matrixY_z.Clear();
                textBoxAcc_matrixZ_x.Clear();
                textBoxAcc_matrixZ_y.Clear();
                textBoxAcc_matrixZ_z.Clear();
                textBoxAcc_biasX.Clear();
                textBoxAcc_biasY.Clear();
                textBoxAcc_biasZ.Clear();
                MessageBox.Show("Incorrect Accelerometer input data!", "Warning!");
                buttonWriteAccValues.Enabled = false;
            }
        }

        private void calculate_transformation_matrix(bool MagMat)
        {
            if (MagMat)
            {
                //Axis X--------------------------------------------------------------------------------------------------
                double[] Xplus_center = new double[3];
                //Centers of the circles
                Xplus_center[0] = (double.Parse(textBoxXplus_X_0.Text) + double.Parse(textBoxXplus_X_180.Text)) / 2;
                Xplus_center[1] = (double.Parse(textBoxXplus_Y_0.Text) + double.Parse(textBoxXplus_Y_180.Text)) / 2;
                Xplus_center[2] = (double.Parse(textBoxXplus_Z_0.Text) + double.Parse(textBoxXplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Xminus_center = new double[3];
                Xminus_center[0] = (double.Parse(textBoxXminus_X_0.Text) + double.Parse(textBoxXminus_X_180.Text)) / 2;
                Xminus_center[1] = (double.Parse(textBoxXminus_Y_0.Text) + double.Parse(textBoxXminus_Y_180.Text)) / 2;
                Xminus_center[2] = (double.Parse(textBoxXminus_Z_0.Text) + double.Parse(textBoxXminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Xvector = new double[3];
                Xvector[0] = Xplus_center[0] - Xminus_center[0];
                Xvector[1] = Xplus_center[1] - Xminus_center[1];
                Xvector[2] = Xplus_center[2] - Xminus_center[2];

                //Axis Y--------------------------------------------------------------------------------------------------
                double[] Yplus_center = new double[3];
                //Centers of the circles
                Yplus_center[0] = (double.Parse(textBoxYplus_X_0.Text) + double.Parse(textBoxYplus_X_180.Text)) / 2;
                Yplus_center[1] = (double.Parse(textBoxYplus_Y_0.Text) + double.Parse(textBoxYplus_Y_180.Text)) / 2;
                Yplus_center[2] = (double.Parse(textBoxYplus_Z_0.Text) + double.Parse(textBoxYplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Yminus_center = new double[3];
                Yminus_center[0] = (double.Parse(textBoxYminus_X_0.Text) + double.Parse(textBoxYminus_X_180.Text)) / 2;
                Yminus_center[1] = (double.Parse(textBoxYminus_Y_0.Text) + double.Parse(textBoxYminus_Y_180.Text)) / 2;
                Yminus_center[2] = (double.Parse(textBoxYminus_Z_0.Text) + double.Parse(textBoxYminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Yvector = new double[3];
                Yvector[0] = Yplus_center[0] - Yminus_center[0];
                Yvector[1] = Yplus_center[1] - Yminus_center[1];
                Yvector[2] = Yplus_center[2] - Yminus_center[2];

                //Axis Z--------------------------------------------------------------------------------------------------
                double[] Zplus_center = new double[3];
                //Centers of the circles
                Zplus_center[0] = (double.Parse(textBoxZplus_X_0.Text) + double.Parse(textBoxZplus_X_180.Text)) / 2;
                Zplus_center[1] = (double.Parse(textBoxZplus_Y_0.Text) + double.Parse(textBoxZplus_Y_180.Text)) / 2;
                Zplus_center[2] = (double.Parse(textBoxZplus_Z_0.Text) + double.Parse(textBoxZplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Zminus_center = new double[3];
                Zminus_center[0] = (double.Parse(textBoxZminus_X_0.Text) + double.Parse(textBoxZminus_X_180.Text)) / 2;
                Zminus_center[1] = (double.Parse(textBoxZminus_Y_0.Text) + double.Parse(textBoxZminus_Y_180.Text)) / 2;
                Zminus_center[2] = (double.Parse(textBoxZminus_Z_0.Text) + double.Parse(textBoxZminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Zvector = new double[3];
                Zvector[0] = Zplus_center[0] - Zminus_center[0];
                Zvector[1] = Zplus_center[1] - Zminus_center[1];
                Zvector[2] = Zplus_center[2] - Zminus_center[2];

                // Rotation matrix--------------------------------------------------------------------------------------
                // rotation_matrix[a][b], a - number of the rows, b - number of the columbs
                double[][] rotation_matrix = new double[3][];
                rotation_matrix[0] = new double[3];
                rotation_matrix[1] = new double[3];
                rotation_matrix[2] = new double[3];
                //Deviding by main value, for example for X axis - deviding by X coordinate, for Y axis by Y coordinate, for Z axis by Z cordinate
                rotation_matrix[0][0] = Xvector[0] / Xvector[0]; rotation_matrix[0][1] = Yvector[0] / Yvector[1]; rotation_matrix[0][2] = Zvector[0] / Zvector[2];
                rotation_matrix[1][0] = Xvector[1] / Xvector[0]; rotation_matrix[1][1] = Yvector[1] / Yvector[1]; rotation_matrix[1][2] = Zvector[1] / Zvector[2];
                rotation_matrix[2][0] = Xvector[2] / Xvector[0]; rotation_matrix[2][1] = Yvector[2] / Yvector[1]; rotation_matrix[2][2] = Zvector[2] / Zvector[2];
                //Matrix inversion
                rotation_matrix = InvertMatrix(rotation_matrix);

                //Determinating of the corrected by ratation matrix centers of the circles 
                Xplus_center = MatrixVectorMultiply(rotation_matrix, Xplus_center);
                Xminus_center = MatrixVectorMultiply(rotation_matrix, Xminus_center);
                Yplus_center = MatrixVectorMultiply(rotation_matrix, Yplus_center);
                Yminus_center = MatrixVectorMultiply(rotation_matrix, Yminus_center);
                Zplus_center = MatrixVectorMultiply(rotation_matrix, Zplus_center);
                Zminus_center = MatrixVectorMultiply(rotation_matrix, Zminus_center);

                //Determinating of the elipsoid center---------------------------------------------------------------------------
                double[] center = new double[3];
                center[0] = (Xplus_center[0] + Xminus_center[0] + Yplus_center[0] + Yminus_center[0] + Zplus_center[0] + Zminus_center[0]) / 6;
                center[1] = (Xplus_center[1] + Xminus_center[1] + Yplus_center[1] + Yminus_center[1] + Zplus_center[1] + Zminus_center[1]) / 6;
                center[2] = (Xplus_center[2] + Xminus_center[2] + Yplus_center[2] + Yminus_center[2] + Zplus_center[2] + Zminus_center[2]) / 6;

                //Determinating of the radius of the future sphere-----------------------------------------------------------------------
                double x_length = Math.Abs(Xplus_center[0] - Xminus_center[0]) / 2;
                double y_length = Math.Abs(Yplus_center[1] - Yminus_center[1]) / 2;
                double z_length = Math.Abs(Zplus_center[2] - Zminus_center[2]) / 2;
                double[] Xplus_0 = new double[3];
                Xplus_0[0] = double.Parse(textBoxXplus_X_0.Text);
                Xplus_0[1] = double.Parse(textBoxXplus_Y_0.Text);
                Xplus_0[2] = double.Parse(textBoxXplus_Z_0.Text);
                Xplus_0 = MatrixVectorMultiply(rotation_matrix, Xplus_0);
                double[] Yplus_0 = new double[3];
                Yplus_0[0] = double.Parse(textBoxYplus_X_0.Text);
                Yplus_0[1] = double.Parse(textBoxYplus_Y_0.Text);
                Yplus_0[2] = double.Parse(textBoxYplus_Z_0.Text);
                Yplus_0 = MatrixVectorMultiply(rotation_matrix, Yplus_0);
                double[] Zplus_0 = new double[3];
                Zplus_0[0] = double.Parse(textBoxZplus_X_0.Text);
                Zplus_0[1] = double.Parse(textBoxZplus_Y_0.Text);
                Zplus_0[2] = double.Parse(textBoxZplus_Z_0.Text);
                Zplus_0 = MatrixVectorMultiply(rotation_matrix, Zplus_0);
                double x_abs = Math.Sqrt(x_length * x_length + Xplus_0[1] * Xplus_0[1] + Xplus_0[2] * Xplus_0[2]);
                double y_abs = Math.Sqrt(Yplus_0[0] * Yplus_0[0] + y_length * y_length + Yplus_0[2] * Yplus_0[2]);
                double z_abs = Math.Sqrt(Zplus_0[0] * Zplus_0[0] + Zplus_0[1] * Zplus_0[1] + z_length * z_length);
                //sphere radius
                double sphere_radius = (x_abs + y_abs + z_abs) / 3;

                //Scales for the each axis------------------------------------------------
                //Diameter of the sphere
                double diameter = sphere_radius * 2;
                double kx = Math.Abs(diameter / (Xplus_center[0] - Xminus_center[0]));
                double ky = Math.Abs(diameter / (Yplus_center[1] - Yminus_center[1]));
                double kz = Math.Abs(diameter / (Zplus_center[2] - Zminus_center[2]));

                //Multiplying elements of matrix by scales
                rotation_matrix[0][0] = rotation_matrix[0][0] * kx; rotation_matrix[0][1] = rotation_matrix[0][1] * ky; rotation_matrix[0][2] = rotation_matrix[0][2] * kz;
                rotation_matrix[1][0] = rotation_matrix[1][0] * kx; rotation_matrix[1][1] = rotation_matrix[1][1] * ky; rotation_matrix[1][2] = rotation_matrix[1][2] * kz;
                rotation_matrix[2][0] = rotation_matrix[2][0] * kx; rotation_matrix[2][1] = rotation_matrix[2][1] * ky; rotation_matrix[2][2] = rotation_matrix[2][2] * kz;

                //Bias
                double[] bias = new double[3];
                bias[0] = center[0];
                bias[1] = center[1];
                bias[2] = center[2];

                //Indication
                //Transformation matrix
                textBox_matrixX_x.Text = rotation_matrix[0][0].ToString("0.###"); textBox_matrixY_x.Text = rotation_matrix[0][1].ToString("0.###"); textBox_matrixZ_x.Text = rotation_matrix[0][2].ToString("0.###");
                textBox_matrixX_y.Text = rotation_matrix[1][0].ToString("0.###"); textBox_matrixY_y.Text = rotation_matrix[1][1].ToString("0.###"); textBox_matrixZ_y.Text = rotation_matrix[1][2].ToString("0.###");
                textBox_matrixX_z.Text = rotation_matrix[2][0].ToString("0.###"); textBox_matrixY_z.Text = rotation_matrix[2][1].ToString("0.###"); textBox_matrixZ_z.Text = rotation_matrix[2][2].ToString("0.###");
                //Bias
                textBox_biasX.Text = bias[0].ToString("0.###");
                textBox_biasY.Text = bias[1].ToString("0.###");
                textBox_biasZ.Text = bias[2].ToString("0.###");
            }
            else
            {
                //Axis X--------------------------------------------------------------------------------------------------
                double[] Xplus_center = new double[3];
                //Centers of the circles
                Xplus_center[0] = (double.Parse(textBoxAccXplus_X_0.Text) + double.Parse(textBoxAccXplus_X_180.Text)) / 2;
                Xplus_center[1] = (double.Parse(textBoxAccXplus_Y_0.Text) + double.Parse(textBoxAccXplus_Y_180.Text)) / 2;
                Xplus_center[2] = (double.Parse(textBoxAccXplus_Z_0.Text) + double.Parse(textBoxAccXplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Xminus_center = new double[3];
                Xminus_center[0] = (double.Parse(textBoxAccXminus_X_0.Text) + double.Parse(textBoxAccXminus_X_180.Text)) / 2;
                Xminus_center[1] = (double.Parse(textBoxAccXminus_Y_0.Text) + double.Parse(textBoxAccXminus_Y_180.Text)) / 2;
                Xminus_center[2] = (double.Parse(textBoxAccXminus_Z_0.Text) + double.Parse(textBoxAccXminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Xvector = new double[3];
                Xvector[0] = Xplus_center[0] - Xminus_center[0];
                Xvector[1] = Xplus_center[1] - Xminus_center[1];
                Xvector[2] = Xplus_center[2] - Xminus_center[2];

                //Axis Y--------------------------------------------------------------------------------------------------
                double[] Yplus_center = new double[3];
                //Centers of the circles
                Yplus_center[0] = (double.Parse(textBoxAccYplus_X_0.Text) + double.Parse(textBoxAccYplus_X_180.Text)) / 2;
                Yplus_center[1] = (double.Parse(textBoxAccYplus_Y_0.Text) + double.Parse(textBoxAccYplus_Y_180.Text)) / 2;
                Yplus_center[2] = (double.Parse(textBoxAccYplus_Z_0.Text) + double.Parse(textBoxAccYplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Yminus_center = new double[3];
                Yminus_center[0] = (double.Parse(textBoxAccYminus_X_0.Text) + double.Parse(textBoxAccYminus_X_180.Text)) / 2;
                Yminus_center[1] = (double.Parse(textBoxAccYminus_Y_0.Text) + double.Parse(textBoxAccYminus_Y_180.Text)) / 2;
                Yminus_center[2] = (double.Parse(textBoxAccYminus_Z_0.Text) + double.Parse(textBoxAccYminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Yvector = new double[3];
                Yvector[0] = Yplus_center[0] - Yminus_center[0];
                Yvector[1] = Yplus_center[1] - Yminus_center[1];
                Yvector[2] = Yplus_center[2] - Yminus_center[2];

                //Axis Z--------------------------------------------------------------------------------------------------
                double[] Zplus_center = new double[3];
                //Centers of the circles
                Zplus_center[0] = (double.Parse(textBoxAccZplus_X_0.Text) + double.Parse(textBoxAccZplus_X_180.Text)) / 2;
                Zplus_center[1] = (double.Parse(textBoxAccZplus_Y_0.Text) + double.Parse(textBoxAccZplus_Y_180.Text)) / 2;
                Zplus_center[2] = (double.Parse(textBoxAccZplus_Z_0.Text) + double.Parse(textBoxAccZplus_Z_180.Text)) / 2;
                //Centers of the circles
                double[] Zminus_center = new double[3];
                Zminus_center[0] = (double.Parse(textBoxAccZminus_X_0.Text) + double.Parse(textBoxAccZminus_X_180.Text)) / 2;
                Zminus_center[1] = (double.Parse(textBoxAccZminus_Y_0.Text) + double.Parse(textBoxAccZminus_Y_180.Text)) / 2;
                Zminus_center[2] = (double.Parse(textBoxAccZminus_Z_0.Text) + double.Parse(textBoxAccZminus_Z_180.Text)) / 2;
                //Vector from the center of minus circle to the center of plus circle
                double[] Zvector = new double[3];
                Zvector[0] = Zplus_center[0] - Zminus_center[0];
                Zvector[1] = Zplus_center[1] - Zminus_center[1];
                Zvector[2] = Zplus_center[2] - Zminus_center[2];

                // Rotation matrix--------------------------------------------------------------------------------------
                // rotation_matrix[a][b], a - number of the rows, b - number of the columbs
                double[][] rotation_matrix = new double[3][];
                rotation_matrix[0] = new double[3];
                rotation_matrix[1] = new double[3];
                rotation_matrix[2] = new double[3];
                //Deviding by main value, for example for X axis - deviding by X coordinate, for Y axis by Y coordinate, for Z axis by Z cordinate
                rotation_matrix[0][0] = Xvector[0] / Xvector[0]; rotation_matrix[0][1] = Yvector[0] / Yvector[1]; rotation_matrix[0][2] = Zvector[0] / Zvector[2];
                rotation_matrix[1][0] = Xvector[1] / Xvector[0]; rotation_matrix[1][1] = Yvector[1] / Yvector[1]; rotation_matrix[1][2] = Zvector[1] / Zvector[2];
                rotation_matrix[2][0] = Xvector[2] / Xvector[0]; rotation_matrix[2][1] = Yvector[2] / Yvector[1]; rotation_matrix[2][2] = Zvector[2] / Zvector[2];
                //Matrix inversion
                rotation_matrix = InvertMatrix(rotation_matrix);

                //Determinating of the corrected by ratation matrix centers of the circles 
                Xplus_center = MatrixVectorMultiply(rotation_matrix, Xplus_center);
                Xminus_center = MatrixVectorMultiply(rotation_matrix, Xminus_center);
                Yplus_center = MatrixVectorMultiply(rotation_matrix, Yplus_center);
                Yminus_center = MatrixVectorMultiply(rotation_matrix, Yminus_center);
                Zplus_center = MatrixVectorMultiply(rotation_matrix, Zplus_center);
                Zminus_center = MatrixVectorMultiply(rotation_matrix, Zminus_center);

                //Determinating of the elipsoid center---------------------------------------------------------------------------
                double[] center = new double[3];
                center[0] = (Xplus_center[0] + Xminus_center[0] + Yplus_center[0] + Yminus_center[0] + Zplus_center[0] + Zminus_center[0]) / 6;
                center[1] = (Xplus_center[1] + Xminus_center[1] + Yplus_center[1] + Yminus_center[1] + Zplus_center[1] + Zminus_center[1]) / 6;
                center[2] = (Xplus_center[2] + Xminus_center[2] + Yplus_center[2] + Yminus_center[2] + Zplus_center[2] + Zminus_center[2]) / 6;

                //Determinating of the radius of the future sphere-----------------------------------------------------------------------
                double x_length = Math.Abs(Xplus_center[0] - Xminus_center[0]) / 2;
                double y_length = Math.Abs(Yplus_center[1] - Yminus_center[1]) / 2;
                double z_length = Math.Abs(Zplus_center[2] - Zminus_center[2]) / 2;
                double[] Xplus_0 = new double[3];
                Xplus_0[0] = double.Parse(textBoxAccXplus_X_0.Text);
                Xplus_0[1] = double.Parse(textBoxAccXplus_Y_0.Text);
                Xplus_0[2] = double.Parse(textBoxAccXplus_Z_0.Text);
                Xplus_0 = MatrixVectorMultiply(rotation_matrix, Xplus_0);
                double[] Yplus_0 = new double[3];
                Yplus_0[0] = double.Parse(textBoxAccYplus_X_0.Text);
                Yplus_0[1] = double.Parse(textBoxAccYplus_Y_0.Text);
                Yplus_0[2] = double.Parse(textBoxAccYplus_Z_0.Text);
                Yplus_0 = MatrixVectorMultiply(rotation_matrix, Yplus_0);
                double[] Zplus_0 = new double[3];
                Zplus_0[0] = double.Parse(textBoxAccZplus_X_0.Text);
                Zplus_0[1] = double.Parse(textBoxAccZplus_Y_0.Text);
                Zplus_0[2] = double.Parse(textBoxAccZplus_Z_0.Text);
                Zplus_0 = MatrixVectorMultiply(rotation_matrix, Zplus_0);
                double x_abs = Math.Sqrt(x_length * x_length + Xplus_0[1] * Xplus_0[1] + Xplus_0[2] * Xplus_0[2]);
                double y_abs = Math.Sqrt(Yplus_0[0] * Yplus_0[0] + y_length * y_length + Yplus_0[2] * Yplus_0[2]);
                double z_abs = Math.Sqrt(Zplus_0[0] * Zplus_0[0] + Zplus_0[1] * Zplus_0[1] + z_length * z_length);
                //sphere radius
                double sphere_radius = (x_abs + y_abs + z_abs) / 3;

                //Scales for the each axis------------------------------------------------
                //Diameter of the sphere
                double diameter = sphere_radius * 2;
                double kx = Math.Abs(diameter / (Xplus_center[0] - Xminus_center[0]));
                double ky = Math.Abs(diameter / (Yplus_center[1] - Yminus_center[1]));
                double kz = Math.Abs(diameter / (Zplus_center[2] - Zminus_center[2]));

                //Multiplying elements of matrix by scales
                rotation_matrix[0][0] = rotation_matrix[0][0] * kx; rotation_matrix[0][1] = rotation_matrix[0][1] * ky; rotation_matrix[0][2] = rotation_matrix[0][2] * kz;
                rotation_matrix[1][0] = rotation_matrix[1][0] * kx; rotation_matrix[1][1] = rotation_matrix[1][1] * ky; rotation_matrix[1][2] = rotation_matrix[1][2] * kz;
                rotation_matrix[2][0] = rotation_matrix[2][0] * kx; rotation_matrix[2][1] = rotation_matrix[2][1] * ky; rotation_matrix[2][2] = rotation_matrix[2][2] * kz;

                //Bias
                double[] bias = new double[3];
                bias[0] = center[0];
                bias[1] = center[1];
                bias[2] = center[2];

                //Indication
                //Transformation matrix
                textBoxAcc_matrixX_x.Text = rotation_matrix[0][0].ToString("0.###"); textBoxAcc_matrixY_x.Text = rotation_matrix[0][1].ToString("0.###"); textBoxAcc_matrixZ_x.Text = rotation_matrix[0][2].ToString("0.###");
                textBoxAcc_matrixX_y.Text = rotation_matrix[1][0].ToString("0.###"); textBoxAcc_matrixY_y.Text = rotation_matrix[1][1].ToString("0.###"); textBoxAcc_matrixZ_y.Text = rotation_matrix[1][2].ToString("0.###");
                textBoxAcc_matrixX_z.Text = rotation_matrix[2][0].ToString("0.###"); textBoxAcc_matrixY_z.Text = rotation_matrix[2][1].ToString("0.###"); textBoxAcc_matrixZ_z.Text = rotation_matrix[2][2].ToString("0.###");
                //Bias
                textBoxAcc_biasX.Text = bias[0].ToString("0.###");
                textBoxAcc_biasY.Text = bias[1].ToString("0.###");
                textBoxAcc_biasZ.Text = bias[2].ToString("0.###");
            }
        }

        public static double[] MatrixVectorMultiply(double[][] matrixA, double[] vectorB)
        {
            int aRows = matrixA.Length; int aCols = matrixA[0].Length;
            int bRows = vectorB.Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");
            double[] result = new double[aRows];
            for (int i = 0; i < aRows; ++i) // each row of A
                for (int k = 0; k < aCols; ++k)
                    result[i] += matrixA[i][k] * vectorB[k];
            return result;
        }

        public static double[][] InvertMatrix(double[][] A)
        {
            int n = A.Length;
            //e will represent each column in the identity matrix
            double[] e;
            //x will hold the inverse matrix to be returned
            double[][] x = new double[n][];
            for (int i = 0; i < n; i++)
            {
                x[i] = new double[A[i].Length];
            }
            /*
            * solve will contain the vector solution for the LUP decomposition as we solve
            * for each vector of x.  We will combine the solutions into the double[][] array x.
            * */
            double[] solve;

            //Get the LU matrix and P matrix (as an array)
            Tuple<double[][], int[]> results = LUPDecomposition(A);

            double[][] LU = results.Item1;
            int[] P = results.Item2;

            /*
            * Solve AX = e for each column ei of the identity matrix using LUP decomposition
            * */
            for (int i = 0; i < n; i++)
            {
                e = new double[A[i].Length];
                e[i] = 1;
                solve = LUPSolve(LU, P, e);
                for (int j = 0; j < solve.Length; j++)
                {
                    x[j][i] = solve[j];
                }
            }
            return x;
        }

        public static double[] LUPSolve(double[][] LU, int[] pi, double[] b)
        {
            int n = LU.Length - 1;
            double[] x = new double[n + 1];
            double[] y = new double[n + 1];
            double suml = 0;
            double sumu = 0;
            double lij = 0;

            /*
            * Solve for y using formward substitution
            * */
            for (int i = 0; i <= n; i++)
            {
                suml = 0;
                for (int j = 0; j <= i - 1; j++)
                {
                    /*
                    * Since we've taken L and U as a singular matrix as an input
                    * the value for L at index i and j will be 1 when i equals j, not LU[i][j], since
                    * the diagonal values are all 1 for L.
                    * */
                    if (i == j)
                    {
                        lij = 1;
                    }
                    else
                    {
                        lij = LU[i][j];
                    }
                    suml = suml + (lij * y[j]);
                }
                y[i] = b[pi[i]] - suml;
            }
            //Solve for x by using back substitution
            for (int i = n; i >= 0; i--)
            {
                sumu = 0;
                for (int j = i + 1; j <= n; j++)
                {
                    sumu = sumu + (LU[i][j] * x[j]);
                }
                x[i] = (y[i] - sumu) / LU[i][i];
            }
            return x;
        }

        public static Tuple<double[][], int[]> LUPDecomposition(double[][] A)
        {
            int n = A.Length - 1;
            /*
            * pi represents the permutation matrix.  We implement it as an array
            * whose value indicates which column the 1 would appear.  We use it to avoid 
            * dividing by zero or small numbers.
            * */
            int[] pi = new int[n + 1];
            double p = 0;
            int kp = 0;
            int pik = 0;
            int pikp = 0;
            double aki = 0;
            double akpi = 0;

            //Initialize the permutation matrix, will be the identity matrix
            for (int j = 0; j <= n; j++)
            {
                pi[j] = j;
            }

            for (int k = 0; k <= n; k++)
            {
                /*
                * In finding the permutation matrix p that avoids dividing by zero
                * we take a slightly different approach.  For numerical stability
                * We find the element with the largest 
                * absolute value of those in the current first column (column k).  If all elements in
                * the current first column are zero then the matrix is singluar and throw an
                * error.
                * */
                p = 0;
                for (int i = k; i <= n; i++)
                {
                    if (Math.Abs(A[i][k]) > p)
                    {
                        p = Math.Abs(A[i][k]);
                        kp = i;
                    }
                }
                if (p == 0)
                {
                    throw new Exception("singular matrix");
                }
                /*
                * These lines update the pivot array (which represents the pivot matrix)
                * by exchanging pi[k] and pi[kp].
                * */
                pik = pi[k];
                pikp = pi[kp];
                pi[k] = pikp;
                pi[kp] = pik;

                /*
                * Exchange rows k and kpi as determined by the pivot
                * */
                for (int i = 0; i <= n; i++)
                {
                    aki = A[k][i];
                    akpi = A[kp][i];
                    A[k][i] = akpi;
                    A[kp][i] = aki;
                }

                /*
                    * Compute the Schur complement
                    * */
                for (int i = k + 1; i <= n; i++)
                {
                    A[i][k] = A[i][k] / A[k][k];
                    for (int j = k + 1; j <= n; j++)
                    {
                        A[i][j] = A[i][j] - (A[i][k] * A[k][j]);
                    }
                }
            }
            return Tuple.Create(A, pi);
        }

        /*
        private void help_image_form(string help_image_massge, string help_image_link)
        {
            axis_image_view frm_op = new axis_image_view();
            frm_op.Owner = this;
            frm_op.text = help_image_massge;
            frm_op.image_link = help_image_link;
            frm_op.ShowDialog();
        }
        */
        private void button9_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "X_plus_0.png");
            MessageBox.Show("X axis Up\r\nY axis South","Orientation 1");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "X_plus_180.png");
            MessageBox.Show("X axis Up\r\nY axis North", "Orientation 2");
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Y_plus_0.png");
            MessageBox.Show("Y axis Up\r\nX axis South", "Orientation 5");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Y_plus_180.png");
            MessageBox.Show("Y axis Up\r\nX axis North", "Orientation 6");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Z_plus_0.png");
            MessageBox.Show("Z axis Up\r\nX axis South", "Orientation 9");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Z_plus_180.png");
            MessageBox.Show("Z axis Up\r\nX axis North", "Orientation 10");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "X_minus_0.png");
            MessageBox.Show("X axis Down\r\nY axis South", "Orientation 3");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "X_minus_180.png");
            MessageBox.Show("X axis Down\r\nY axis North", "Orientation 4");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Y_minus_0.png");
            MessageBox.Show("Y axis Down\r\nX axis South", "Orientation 7");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Y_minus_180.png");
            MessageBox.Show("Y axis Down\r\nX axis North", "Orientation 8");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 0°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Z_minus_0.png");
            MessageBox.Show("Z axis Down\r\nX axis South", "Orientation 11");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //help_image_form("and click \"Point 180°\" button or enter data manually.", curDir + "\\MagMaster Files\\images\\" + "Z_minus_180.png");
            MessageBox.Show("Z axis Down\r\nX axis North", "Orientation 12");
        }

        private void help_button_Click(object sender, EventArgs e)
        {
            //serial_port_help_from frm_op = new serial_port_help_from();
            //frm_op.Owner = this;
            //frm_op.help_text = System.IO.File.ReadAllText(curDir + "\\MagMaster Files\\texts\\" + "sphelp.txt"); 
            //frm_op.ShowDialog();
            string str = "";
            str += "115200 Baud\r\n\r\n";
            str += "8 Bits\r\n\r\n";
            str += "1 Stop Bit\r\n\r\n";
            str += "No Parity";
            MessageBox.Show(str , "Serial Port Settings");
        }
        private void buttonLoadRawValues_Click(object sender, EventArgs e)
        {   
            Stream stream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //string dtstr = DateTime.Now.ToString("yyyyMMddHHmmss");
            //string FileName = textBoxSN.Text + "_Raw_" + dtstr + ".txt";
            string DirName = FolderPath + "\\RTI\\FactoryCompassCalibration";
            openFileDialog1.InitialDirectory = DirName;// curDir;
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //string FileName = openFileDialog1.SafeFileName;
                try
                {
                    if ((stream = openFileDialog1.OpenFile()) != null)
                    {
                        string str = File.ReadAllText(openFileDialog1.FileName);
                        string[] values = str.Split(',','\r','\n');

                        if (values.Count() == 85)
                        {
                            int i = 0;
                            textBoxXplus_X_0.Text = values[i++];
                            textBoxXplus_Y_0.Text = values[i++];
                            textBoxXplus_Z_0.Text = values[i++];
                            textBoxAccXplus_X_0.Text = values[i++];
                            textBoxAccXplus_Y_0.Text = values[i++];
                            textBoxAccXplus_Z_0.Text = values[i++];
                            i++;
                            textBoxXplus_X_180.Text = values[i++];
                            textBoxXplus_Y_180.Text = values[i++];
                            textBoxXplus_Z_180.Text = values[i++];
                            textBoxAccXplus_X_180.Text = values[i++];
                            textBoxAccXplus_Y_180.Text = values[i++];
                            textBoxAccXplus_Z_180.Text = values[i++];
                            i++;
                            textBoxXminus_X_0.Text = values[i++];
                            textBoxXminus_Y_0.Text = values[i++];
                            textBoxXminus_Z_0.Text = values[i++];
                            textBoxAccXminus_X_0.Text = values[i++];
                            textBoxAccXminus_Y_0.Text = values[i++];
                            textBoxAccXminus_Z_0.Text = values[i++];
                            i++;
                            textBoxXminus_X_180.Text = values[i++];
                            textBoxXminus_Y_180.Text = values[i++];
                            textBoxXminus_Z_180.Text = values[i++];
                            textBoxAccXminus_X_180.Text = values[i++];
                            textBoxAccXminus_Y_180.Text = values[i++];
                            textBoxAccXminus_Z_180.Text = values[i++];
                            i++;

                            textBoxYplus_X_0.Text = values[i++];
                            textBoxYplus_Y_0.Text = values[i++];
                            textBoxYplus_Z_0.Text = values[i++];
                            textBoxAccYplus_X_0.Text = values[i++];
                            textBoxAccYplus_Y_0.Text = values[i++];
                            textBoxAccYplus_Z_0.Text = values[i++];
                            i++;
                            textBoxYplus_X_180.Text = values[i++];
                            textBoxYplus_Y_180.Text = values[i++];
                            textBoxYplus_Z_180.Text = values[i++];
                            textBoxAccYplus_X_180.Text = values[i++];
                            textBoxAccYplus_Y_180.Text = values[i++];
                            textBoxAccYplus_Z_180.Text = values[i++];
                            i++;
                            textBoxYminus_X_0.Text = values[i++];
                            textBoxYminus_Y_0.Text = values[i++];
                            textBoxYminus_Z_0.Text = values[i++];
                            textBoxAccYminus_X_0.Text = values[i++];
                            textBoxAccYminus_Y_0.Text = values[i++];
                            textBoxAccYminus_Z_0.Text = values[i++];
                            i++;
                            textBoxYminus_X_180.Text = values[i++];
                            textBoxYminus_Y_180.Text = values[i++];
                            textBoxYminus_Z_180.Text = values[i++];
                            textBoxAccYminus_X_180.Text = values[i++];
                            textBoxAccYminus_Y_180.Text = values[i++];
                            textBoxAccYminus_Z_180.Text = values[i++];
                            i++;

                            textBoxZplus_X_0.Text = values[i++];
                            textBoxZplus_Y_0.Text = values[i++];
                            textBoxZplus_Z_0.Text = values[i++];
                            textBoxAccZplus_X_0.Text = values[i++];
                            textBoxAccZplus_Y_0.Text = values[i++];
                            textBoxAccZplus_Z_0.Text = values[i++];
                            i++;
                            textBoxZplus_X_180.Text = values[i++];
                            textBoxZplus_Y_180.Text = values[i++];
                            textBoxZplus_Z_180.Text = values[i++];
                            textBoxAccZplus_X_180.Text = values[i++];
                            textBoxAccZplus_Y_180.Text = values[i++];
                            textBoxAccZplus_Z_180.Text = values[i++];
                            i++;
                            textBoxZminus_X_0.Text = values[i++];
                            textBoxZminus_Y_0.Text = values[i++];
                            textBoxZminus_Z_0.Text = values[i++];
                            textBoxAccZminus_X_0.Text = values[i++];
                            textBoxAccZminus_Y_0.Text = values[i++];
                            textBoxAccZminus_Z_0.Text = values[i++];
                            i++;
                            textBoxZminus_X_180.Text = values[i++];
                            textBoxZminus_Y_180.Text = values[i++];
                            textBoxZminus_Z_180.Text = values[i++];
                            textBoxAccZminus_X_180.Text = values[i++];
                            textBoxAccZminus_Y_180.Text = values[i++];
                            textBoxAccZminus_Z_180.Text = values[i++];
                            i++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    string exceptionmessage = String.Format("Read File Error A: {0}", ex.GetType().ToString());
                    MessageBox.Show(exceptionmessage);
                }
                if (stream != null)
                    stream.Close();
                ClearAll();
            }
        }

        private void buttonWriteMagValues_Click(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {   
                TimerEnabled = false;

                string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string DirName = FolderPath + "\\RTI\\FactoryCompassCalibration";
                string FileName;                
                string dtstr = DateTime.Now.ToString("yyyyMMddHHmmss");

                textBoxStatus.Text = "Uploading Mag Cal Values";
                Application.DoEvents();

                SendCom(port, "STOP");//stop adcp
                int count = 100;
                while (!string_from_usart.Contains("STOP"))
                {
                    count--;
                    if (count < 0)
                        break;
                    System.Threading.Thread.Sleep(100);
                    Application.DoEvents();
                }

                if (string_from_usart.Contains("STOP" + '\u0006'))
                {
                    string str = "ENGATT 3,";

                    str += textBox_matrixX_x.Text + ",";
                    str += textBox_matrixY_x.Text + ",";
                    str += textBox_matrixZ_x.Text + ",";

                    str += textBox_matrixX_y.Text + ",";
                    str += textBox_matrixY_y.Text + ",";
                    str += textBox_matrixZ_y.Text + ",";

                    str += textBox_matrixX_z.Text + ",";
                    str += textBox_matrixY_z.Text + ",";
                    str += textBox_matrixZ_z.Text + ",";

                    str += textBox_biasX.Text + ",";
                    str += textBox_biasY.Text + ",";
                    str += textBox_biasZ.Text;

                    SendCom(port, str);
                    System.Threading.Thread.Sleep(1000);
                    if (string_from_usart.Contains("ENGATT 3,") && string_from_usart.Contains('\u0006'))
                    {
                        SendCom(port, "ENGATT 5");
                        System.Threading.Thread.Sleep(1000);
                        if (!string_from_usart.Contains("result = 0"))
                        {
                            MessageBox.Show("ADCP NV Write Error", "Warning!");
                        }
                        else
                        {
                            FileName = "ATT_MAG_MATRIX_SN" + textBoxSN.Text + "_" + dtstr + ".txt";
                            SaveTextFile(DirName, FileName, str, true, false);
                        }
                    }
                    else
                    {
                        MessageBox.Show("ADCP Magnetometer Matrix Write Error", "Warning!");
                    }
                }
                ADCPStarted = false;
                TimerEnabled = true;
            }
            else
            {
                MessageBox.Show("Port Disconnected", "Warning!");
            }
        }

        private void buttonProcedure_Click(object sender, EventArgs e)
        {
            //MagMaster.Form2.ShowInstructions();

            //axis_image_view frm_op = new axis_image_view();
            //frm_op.Owner = this;
            //frm_op.ShowDialog();

            MagMaster.Form2 frm_op = new MagMaster.Form2();
            frm_op.Owner = this;
            //frm_op.ShowDialog();
            frm_op.ShowInstuctions(sender, e);
        }

        private void buttonInitIMU_Click(object sender, EventArgs e)
        {
            //ENGXGSTS response ENGXGSTS 6A07,6A
            //ENGMGSTS response ENGMGSTS 13,C

            TimerEnabled = false;
            ADCPStarted = false;
            
            string_from_usart = "";

            textBoxInitializeHardware.Text = "";
            empty_serial_data_counter = 0;
            textBoxStatus.Text = "Retrieving IMU Status";
            textBoxSN.Text = "NA";
            SendCom(port, "STOP");
            int count = 20;
            while (!string_from_usart.Contains("STOP"))
            {
                count--;
                if (count < 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(100);
                Application.DoEvents();
            }
            if (string_from_usart.Contains("STOP" + '\u0006'))
            {
                SendCom(port, "ENGXGSTS");//get ACC status
                System.Threading.Thread.Sleep(200);
                string strA = string_from_usart;
                textBoxInitializeHardware.Text = strA;
                Application.DoEvents();
                SendCom(port, "ENGMGSTS");//get MAG status
                System.Threading.Thread.Sleep(200);
                string strB = string_from_usart;
                textBoxInitializeHardware.Text = strA + "\r\n" + strB + "\r\n";
                Application.DoEvents();

                bool AccReady;//, MagReady;

                string strC;
                string strD;
                if (strA.Contains("ENGXGSTS 6A"))// 6A07,6A"))
                {
                    strC = "ACC Ready";
                    AccReady = true;
                }
                else
                {
                    strC = "ACC NOT Ready";
                    AccReady = false;
                }
                if (strB.Contains("ENGMGSTS ") && (strB.Contains(",C") || strB.Contains(",30")))
                {
                    strD = "MAG Ready";
                    //MagReady = true;
                }
                else
                {
                    strD = "MAG NOT Ready";
                    //MagReady = false;
                }

                textBoxInitializeHardware.Text += strC +", " + strD + "\r\n";
                Application.DoEvents();

                //initialize ATT
                string strE = "ATT Init FAIL";
                if (AccReady)
                {
                    string_from_usart = "";
                    SendCom(port, "ENGCFGDFL");
                    System.Threading.Thread.Sleep(200);
                    if (string_from_usart.Contains("ENGCFGDFL" + '\u0006'))
                    {
                        string_from_usart = "";
                        SendCom(port, "ENGCFGSAV");
                        System.Threading.Thread.Sleep(200);
                        if (string_from_usart.Contains("ENGCFGSAV" + '\u0006'))
                        {
                            strE = "ATT Init OK";
                        }
                    }
                }
                textBoxInitializeHardware.Text += strE + "\r\n";
                Application.DoEvents();

                /*
                //initialize MAG
                string strF = "MAG Init FAIL";
                if (MagReady)
                {
                    string_from_usart = "";
                    SendCom(port, "ENGMAGOTP");
                    System.Threading.Thread.Sleep(200);
                    if (string_from_usart.Contains('\u0006'))
                    {
                        string_from_usart = "";
                        SendCom(port, "ENGMGCFG 3,0,0,0,0,0,0,0,0,0");
                        System.Threading.Thread.Sleep(200);
                        if (string_from_usart.Contains("ENGMGCFG"))
                        {
                            if (string_from_usart.Contains('\u0006'))
                                strF = "MAG Init OK";
                        }
                    }
                }
                textBoxInitializeHardware.Text += ", " + strF + "\r\n";
                */
                Application.DoEvents();

                int Orientation = listBoxInitializeHardware.SelectedIndex;
                //set orientation
                SendCom(port, "ENGATT 8," + Orientation.ToString());
                int ACKcount = 100;
                string ip = "";
                while (!ip.Contains("Okay") && !string_from_usart.Contains("Okay") && !string_from_usart.Contains("Factory") && !string_from_usart.Contains("Error"))
                {   
                    ACKcount--;
                    if (ACKcount < 0)
                        break;

                    try
                    {
                        ip = port.ReadLine();
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                //System.Threading.Thread.Sleep(2000);
                //Application.DoEvents();

                if (string_from_usart.Contains("Error") || ip.Contains("Error"))
                {
                    SendCom(port, "ENGATT 5");
                    ACKcount = 100;
                    while (!string_from_usart.Contains("Factory"))
                    {
                        ACKcount--;
                        if (ACKcount < 0)
                            break;
                        System.Threading.Thread.Sleep(100);
                    }
                    //System.Threading.Thread.Sleep(2000);
                    SendCom(port, "ENGATT 9");
                    ACKcount = 100;
                    while (!string_from_usart.Contains("Field"))
                    {
                        ACKcount--;
                        if (ACKcount < 0)
                            break;
                        System.Threading.Thread.Sleep(100);
                    }
                    //System.Threading.Thread.Sleep(2000);
                    SendCom(port, "ENGATT 8," + Orientation.ToString());
                    System.Threading.Thread.Sleep(2000);
                    ACKcount = 100;
                    ip = "";
                    while (ip.Contains("Okay") && !string_from_usart.Contains("Okay") && !string_from_usart.Contains("Factory") && !string_from_usart.Contains("Error"))
                    {
                        ACKcount--;
                        if (ACKcount < 0)
                            break;
                        //System.Threading.Thread.Sleep(100);
                        ip = port.ReadLine();
                    }
                }

                //string indata = port.ReadLine();

                ACKcount = 100;
                //while (!string_from_usart.Contains("Okay") && !indata.Contains("Okay"))
                while (!string_from_usart.Contains("Okay") && !ip.Contains("Okay"))
                {
                    ACKcount--;
                    if (ACKcount < 0)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                //if (string_from_usart.Contains("Okay") || indata.Contains("Okay"))
                if (string_from_usart.Contains("Okay") || ip.Contains("Okay"))
                {
                    textBoxInitializeHardware.Text += listBoxInitializeHardware.SelectedItem.ToString();
                }
                else
                {
                    textBoxInitializeHardware.Text += "Set Orientation FAILED";
                }
                Application.DoEvents();
            }
            else
            {
                textBoxInitializeHardware.Text = "ADCP Not Ready";
            }
            TimerEnabled = true;
        }

        private void listBoxInitializeHardware_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxSelectedOrientation.Text = listBoxInitializeHardware.SelectedItem.ToString();
        }

        private void SaveRawData( string DirName, string FileName)
        {
            DirectoryInfo di = new DirectoryInfo(DirName);

            try
            {
                if (di.Exists)// Determine whether the directory exists.
                {
                    //di.Delete();// Delete the directory.
                }
                else// Try to create the directory.
                {
                    di.Create();
                }
            }
            catch
            {
                MessageBox.Show("No Directory. Can't save data!");
            }
            finally
            {
                try
                {
                    string str = "";
                    str += textBoxXplus_X_0.Text + ",";
                    str += textBoxXplus_Y_0.Text + ",";
                    str += textBoxXplus_Z_0.Text + ",";
                    str += textBoxAccXplus_X_0.Text + ",";
                    str += textBoxAccXplus_Y_0.Text + ",";
                    str += textBoxAccXplus_Z_0.Text + "\r\n";

                    str += textBoxXplus_X_180.Text + ",";
                    str += textBoxXplus_Y_180.Text + ",";
                    str += textBoxXplus_Z_180.Text + ",";
                    str += textBoxAccXplus_X_180.Text + ",";
                    str += textBoxAccXplus_Y_180.Text + ",";
                    str += textBoxAccXplus_Z_180.Text + "\r\n";

                    str += textBoxXminus_X_0.Text + ",";
                    str += textBoxXminus_Y_0.Text + ",";
                    str += textBoxXminus_Z_0.Text + ",";
                    str += textBoxAccXminus_X_0.Text + ",";
                    str += textBoxAccXminus_Y_0.Text + ",";
                    str += textBoxAccXminus_Z_0.Text + "\r\n";

                    str += textBoxXminus_X_180.Text + ",";
                    str += textBoxXminus_Y_180.Text + ",";
                    str += textBoxXminus_Z_180.Text + ",";
                    str += textBoxAccXminus_X_180.Text + ",";
                    str += textBoxAccXminus_Y_180.Text + ",";
                    str += textBoxAccXminus_Z_180.Text + "\r\n";


                    str += textBoxYplus_X_0.Text + ",";
                    str += textBoxYplus_Y_0.Text + ",";
                    str += textBoxYplus_Z_0.Text + ",";
                    str += textBoxAccYplus_X_0.Text + ",";
                    str += textBoxAccYplus_Y_0.Text + ",";
                    str += textBoxAccYplus_Z_0.Text + "\r\n";

                    str += textBoxYplus_X_180.Text + ",";
                    str += textBoxYplus_Y_180.Text + ",";
                    str += textBoxYplus_Z_180.Text + ",";
                    str += textBoxAccYplus_X_180.Text + ",";
                    str += textBoxAccYplus_Y_180.Text + ",";
                    str += textBoxAccYplus_Z_180.Text + "\r\n";

                    str += textBoxYminus_X_0.Text + ",";
                    str += textBoxYminus_Y_0.Text + ",";
                    str += textBoxYminus_Z_0.Text + ",";
                    str += textBoxAccYminus_X_0.Text + ",";
                    str += textBoxAccYminus_Y_0.Text + ",";
                    str += textBoxAccYminus_Z_0.Text + "\r\n";

                    str += textBoxYminus_X_180.Text + ",";
                    str += textBoxYminus_Y_180.Text + ",";
                    str += textBoxYminus_Z_180.Text + ",";
                    str += textBoxAccYminus_X_180.Text + ",";
                    str += textBoxAccYminus_Y_180.Text + ",";
                    str += textBoxAccYminus_Z_180.Text + "\r\n";


                    str += textBoxZplus_X_0.Text + ",";
                    str += textBoxZplus_Y_0.Text + ",";
                    str += textBoxZplus_Z_0.Text + ",";
                    str += textBoxAccZplus_X_0.Text + ",";
                    str += textBoxAccZplus_Y_0.Text + ",";
                    str += textBoxAccZplus_Z_0.Text + "\r\n";

                    str += textBoxZplus_X_180.Text + ",";
                    str += textBoxZplus_Y_180.Text + ",";
                    str += textBoxZplus_Z_180.Text + ",";
                    str += textBoxAccZplus_X_180.Text + ",";
                    str += textBoxAccZplus_Y_180.Text + ",";
                    str += textBoxAccZplus_Z_180.Text + "\r\n";

                    str += textBoxZminus_X_0.Text + ",";
                    str += textBoxZminus_Y_0.Text + ",";
                    str += textBoxZminus_Z_0.Text + ",";
                    str += textBoxAccZminus_X_0.Text + ",";
                    str += textBoxAccZminus_Y_0.Text + ",";
                    str += textBoxAccZminus_Z_0.Text + "\r\n";

                    str += textBoxZminus_X_180.Text + ",";
                    str += textBoxZminus_Y_180.Text + ",";
                    str += textBoxZminus_Z_180.Text + ",";
                    str += textBoxAccZminus_X_180.Text + ",";
                    str += textBoxAccZminus_Y_180.Text + ",";
                    str += textBoxAccZminus_Z_180.Text + "\r\n";

                    System.IO.File.WriteAllText(DirName + "\\" + FileName, str);
                }
                catch (Exception ex)
                {
                    string exceptionmessage = String.Format("Write File Error A: {0}", ex.GetType().ToString());
                    MessageBox.Show(exceptionmessage);
                }
            }
        }

        private void buttonClearRawValues_Click(object sender, EventArgs e)
        {
            textBoxXplus_X_0.Text = "";
            textBoxXplus_Y_0.Text = "";
            textBoxXplus_Z_0.Text = "";
            textBoxAccXplus_X_0.Text = "";
            textBoxAccXplus_Y_0.Text = "";
            textBoxAccXplus_Z_0.Text = "";

            textBoxXplus_X_180.Text = "";
            textBoxXplus_Y_180.Text = "";
            textBoxXplus_Z_180.Text = "";
            textBoxAccXplus_X_180.Text = "";
            textBoxAccXplus_Y_180.Text = "";
            textBoxAccXplus_Z_180.Text = "";

            textBoxXminus_X_0.Text = "";
            textBoxXminus_Y_0.Text = "";
            textBoxXminus_Z_0.Text = "";
            textBoxAccXminus_X_0.Text = "";
            textBoxAccXminus_Y_0.Text = "";
            textBoxAccXminus_Z_0.Text = "";

            textBoxXminus_X_180.Text = "";
            textBoxXminus_Y_180.Text = "";
            textBoxXminus_Z_180.Text = "";
            textBoxAccXminus_X_180.Text = "";
            textBoxAccXminus_Y_180.Text = "";
            textBoxAccXminus_Z_180.Text = "";

            textBoxYplus_X_0.Text = "";
            textBoxYplus_Y_0.Text = "";
            textBoxYplus_Z_0.Text = "";
            textBoxAccYplus_X_0.Text = "";
            textBoxAccYplus_Y_0.Text = "";
            textBoxAccYplus_Z_0.Text = "";

            textBoxYplus_X_180.Text = "";
            textBoxYplus_Y_180.Text = "";
            textBoxYplus_Z_180.Text = "";
            textBoxAccYplus_X_180.Text = "";
            textBoxAccYplus_Y_180.Text = "";
            textBoxAccYplus_Z_180.Text = "";

            textBoxYminus_X_0.Text = "";
            textBoxYminus_Y_0.Text = "";
            textBoxYminus_Z_0.Text = "";
            textBoxAccYminus_X_0.Text = "";
            textBoxAccYminus_Y_0.Text = "";
            textBoxAccYminus_Z_0.Text = "";

            textBoxYminus_X_180.Text = "";
            textBoxYminus_Y_180.Text = "";
            textBoxYminus_Z_180.Text = "";
            textBoxAccYminus_X_180.Text = "";
            textBoxAccYminus_Y_180.Text = "";
            textBoxAccYminus_Z_180.Text = "";

            textBoxZplus_X_0.Text = "";
            textBoxZplus_Y_0.Text = "";
            textBoxZplus_Z_0.Text = "";
            textBoxAccZplus_X_0.Text = "";
            textBoxAccZplus_Y_0.Text = "";
            textBoxAccZplus_Z_0.Text = "";

            textBoxZplus_X_180.Text = "";
            textBoxZplus_Y_180.Text = "";
            textBoxZplus_Z_180.Text = "";
            textBoxAccZplus_X_180.Text = "";
            textBoxAccZplus_Y_180.Text = "";
            textBoxAccZplus_Z_180.Text = "";

            textBoxZminus_X_0.Text = "";
            textBoxZminus_Y_0.Text = "";
            textBoxZminus_Z_0.Text = "";
            textBoxAccZminus_X_0.Text = "";
            textBoxAccZminus_Y_0.Text = "";
            textBoxAccZminus_Z_0.Text = "";

            textBoxZminus_X_180.Text = "";
            textBoxZminus_Y_180.Text = "";
            textBoxZminus_Z_180.Text = "";
            textBoxAccZminus_X_180.Text = "";
            textBoxAccZminus_Y_180.Text = "";
            textBoxAccZminus_Z_180.Text = "";

            ClearAll();

        }

        private void buttonSaveRawValues_Click(object sender, EventArgs e)
        {
            string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string DirName = FolderPath + "\\RTI\\FactoryCompassCalibration";
            string dtstr = DateTime.Now.ToString("yyyyMMddHHmmss");
            string FileName = textBoxSN.Text + "_Raw_" + dtstr + ".txt";
            

            Cursor.Current = Cursors.WaitCursor;

            SaveRawData(DirName, FileName);

            Cursor.Current = Cursors.Default;

            textBoxRawDataStorage.Text = DirName + "\\" + FileName;

            ClearAll();
        }

        private void buttonWriteAccValues_Click(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                TimerEnabled = false;

                string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string DirName = FolderPath + "\\RTI\\FactoryCompassCalibration";
                string FileName;
                string dtstr = DateTime.Now.ToString("yyyyMMddHHmmss");

                textBoxStatus.Text = "Uploading Acc Cal Values";
                Application.DoEvents();

                SendCom(port, "STOP");//stop adcp
                int count = 100;
                while (!string_from_usart.Contains("STOP"))
                {
                    count--;
                    if (count < 0)
                        break;
                    System.Threading.Thread.Sleep(100);
                    Application.DoEvents();
                }

                if (string_from_usart.Contains("STOP" + '\u0006'))
                {
                    string str = "ENGATT 4,";

                    str += textBoxAcc_matrixX_x.Text + ",";
                    str += textBoxAcc_matrixY_x.Text + ",";
                    str += textBoxAcc_matrixZ_x.Text + ",";

                    str += textBoxAcc_matrixX_y.Text + ",";
                    str += textBoxAcc_matrixY_y.Text + ",";
                    str += textBoxAcc_matrixZ_y.Text + ",";

                    str += textBoxAcc_matrixX_z.Text + ",";
                    str += textBoxAcc_matrixY_z.Text + ",";
                    str += textBoxAcc_matrixZ_z.Text + ",";

                    str += textBoxAcc_biasX.Text + ",";
                    str += textBoxAcc_biasY.Text + ",";
                    str += textBoxAcc_biasZ.Text;

                    SendCom(port, str);
                    System.Threading.Thread.Sleep(1000);
                    if (string_from_usart.Contains("ENGATT 4,") && string_from_usart.Contains('\u0006'))
                    {
                        SendCom(port, "ENGATT 5");
                        System.Threading.Thread.Sleep(1000);
                        if (!string_from_usart.Contains("result = 0"))
                        {
                            MessageBox.Show("ADCP NV Write Error", "Warning!");
                        }
                        else
                        {
                            FileName = "ATT_ACC_MATRIX_SN" + textBoxSN.Text + "_" + dtstr + ".txt";
                            SaveTextFile(DirName, FileName, str, true, false);
                        }
                    }
                    else
                    {
                        MessageBox.Show("ADCP Accelerometer Matrix Write Error", "Warning!");
                    }
                }
                ADCPStarted = false;
                TimerEnabled = true;
            }
            else
            {
                MessageBox.Show("Port Disconnected", "Warning!");
            }
        }

        private void textBoxXplus_X_0_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tx = (TextBox)sender;
            if ((symbol_mask.IndexOf(e.KeyChar) != -1) || (e.KeyChar == 8))
            {
                if ((e.KeyChar == '.') && (tx.Text.IndexOf(".") != -1))
                    e.Handled = true;
                if ((e.KeyChar == '-') && (tx.Text.IndexOf("-") != -1))
                    e.Handled = true;
            }
            else
                e.Handled = true;
            if(!cleared)
                ClearAll();
        }
        private void textBoxXplus_X_0_TextChanged(object sender, EventArgs e)
        {
            TextBox tx = (TextBox)sender;
            string strbox = tx.Text;

            string symbol_st = ".";
            if (strbox.Length >= symbol_st.Length)
            {
                string substrbox = strbox.Substring(0, symbol_st.Length);
                if (substrbox == symbol_st) strbox = "" + strbox.Substring(symbol_st.Length, strbox.Length - symbol_st.Length);
            }

            symbol_st = "-.";
            if (strbox.Length >= symbol_st.Length)
            {
                string substrbox = strbox.Substring(0, symbol_st.Length);
                if (substrbox == symbol_st) strbox = "-" + strbox.Substring(symbol_st.Length, strbox.Length - symbol_st.Length);
            }

            symbol_st = "-";
            if (strbox.Length >= symbol_st.Length)
            {
                string substrbox = strbox.Substring(0, symbol_st.Length);
                if (substrbox != symbol_st) strbox = strbox.Replace("-", "");
            }

            tx.Text = strbox;
            if (!cleared)
                ClearAll();
        }
        


    }
}
