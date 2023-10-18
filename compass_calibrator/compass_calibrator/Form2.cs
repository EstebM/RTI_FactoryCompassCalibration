using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MagMaster
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        //private void Form2_Load(object sender, EventArgs e)
        public void ShowInstuctions(object sender, EventArgs e)
        {
            string str = "";

            str += "Compass Calibration Procedure:\r\n\r\n";
            str += " 1. Bolt the ADCP to the compass calibration fixture. Note Beam 0 orientation.\r\n\r\n";
            str += " 2. Use a magnetic compass to rotate the calibration table to face North.\r\n\r\n";
            str += " 3. Use a bubble level to check/adjust the calibration table to be level.\r\n\r\n";
            str += " 4. Select the compass physical orientation from the Drop Down Box above the 'Initialize ATT' Button.\r\n\r\n";
            str += " 5. Click 'Initialize ATT' to zero out the previous calibration and set the physical orientation.\r\n\r\n";
            str += " 6. Verify the message box shows: \r\n\r\n";
            str += "      ACC Ready, MAG Ready\r\n";
            str += "      ATT Init OK\r\n\r\n";
            str += " 7. Click 'Clear Raw Values' to zero out the previous raw data values.\r\n\r\n";
            str += " 8. Position the ADCP to the proper Orientation for Step n. Current step # to the North-Top.\r\n\r\n";
            str += " 9. Wait for the raw values to stablize.\r\n\r\n";
            str += "10. Click Button n for Step n.\r\n\r\n";
            str += "11. Repeat steps 8 though 10 for each of the 11 remaining Orientations\r\n\r\n";
            str += "12. Click on 'Calculate Transformation Matrix and Bias from Raw Values'.\r\n\r\n";
            str += "13. Click on 'Upload Mag Values to ADCP'.\r\n\r\n";
            str += "14. Click on 'Upload Acc Values to ADCP'.\r\n\r\n";
            str += "15. Click on 'Save Raw Values to File'.\r\n\r\n";
            str += "Compass Calibration is Complete.\r\n\r\n";
            str += "\r\n\r\n";

            str += "Compass Calibration Notes:\r\n\r\n";
            str += "A. The ADCP serial port must be set to 115200 Baud, 8 bits, 1 stop bit, no parity.\r\n\r\n";
            str += "B. Make sure that the ADCP is powered up.\r\n\r\n";
            str += "C. If the Status is displaying 'Disconnected' select a comm port and then click the Connect button.\r\n\r\n";
            str += "D. If the Status is displaying 'No serial data' the ADCP is not powered or is not connected to the port.\r\n\r\n";
            str += "E. You may need to click Disconnect and then select another comm port to find the ADCP.\r\n\r\n";
            str += "F. Always wait for Status to indicate Connected before starting the calibration.\r\n\r\n";
            str += "G. Start with step 1 followed by step 2 through step 12.\r\n\r\n";
            str += "H. The calibration fixture should rotated to postion the current step # to the North-Top.\r\n\r\n";
            str += "I. The step #'s are written on the calibration fixture.\r\n\r\n";

            str += "\r\n\r\n";

            textBox1.Text = str;
            Application.DoEvents();
            textBox1.Text += " ";

            this.Show();
        }

        public void Fclose(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
