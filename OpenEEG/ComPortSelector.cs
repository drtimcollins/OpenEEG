using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace OpenEEG
{
    public partial class ComPortSelector : Form
    {
        public string selectedPort;

        public ComPortSelector()
        {
            InitializeComponent();
            // Populate list box
            PortListBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                PortListBox.Items.Add(port);
            }
            selectedPort = "";
        }

        private void Cancelbutton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OKbutton_Click(object sender, EventArgs e)
        {
            selectedPort = PortListBox.SelectedItem.ToString();
            Close();
        }
    }
}
