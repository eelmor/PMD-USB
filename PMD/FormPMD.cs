using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PMD {
    public partial class FormPMD : Form {

        List<MonitorGraph> graphList = new List<MonitorGraph>();
        DataLogger data_logger = null;

        List<IPMD_Device> deviceList = new List<IPMD_Device>();
        IPMD_Device selectedDevice = null;

        public FormPMD() {

            InitializeComponent();

            // Update title
            this.Text = "PMD " + Application.ProductVersion;

            // Populate options
            /*comboBoxTimeoutAction.Items.Add("Cycle");
            comboBoxTimeoutAction.Items.Add("OLED Off");
            comboBoxTimeoutAction.Items.Add("Disabled");
            comboBoxTimeoutAction.SelectedIndex = 0;

            comboBoxOled.Items.Add("On");
            comboBoxOled.Items.Add("Off");
            comboBoxOled.SelectedIndex = 1;

            comboBoxOledRotation.Items.Add("0 deg");
            comboBoxOledRotation.Items.Add("180 deg");
            comboBoxOledRotation.SelectedIndex = 0;

            comboBoxDisplaySpeed.Items.Add("0.0s");
            comboBoxDisplaySpeed.Items.Add("0.2s");
            comboBoxDisplaySpeed.Items.Add("0.4s");
            comboBoxDisplaySpeed.Items.Add("0.6s");
            comboBoxDisplaySpeed.Items.Add("0.8s");
            comboBoxDisplaySpeed.Items.Add("1.0s");
            comboBoxDisplaySpeed.Items.Add("1.2s");
            comboBoxDisplaySpeed.Items.Add("1.4s");

            comboBoxAveraging.Items.Add("29µs (1 sample)");
            comboBoxAveraging.Items.Add("1.87ms (64 samples)");
            comboBoxAveraging.Items.Add("119ms (4096 samples)");
            comboBoxAveraging.Items.Add("0.95s (32768 samples)");*/

            UpdateDeviceList();

        }

        private void StartMonitoring() {

            if (selectedDevice != null)
            {
                UpdateGraphTiming();
                selectedDevice.AllSensorsUpdated += SelectedDevice_AllSensorsUpdated;
                selectedDevice.StartMonitoring();
            }
        }

        private void SelectedDevice_AllSensorsUpdated()
        {
            if(data_logger != null && data_logger.IsLogging)
            {
                data_logger.WriteEntry();
            }
            foreach (MonitorGraph monitor_graph in graphList)
            {
                monitor_graph.Redraw();
            }
        }

        private void StopMonitoring()
        {
            if (selectedDevice != null)
            {
                selectedDevice.StopMonitoring();
                graphList.Clear();
            }
        }


        volatile bool update_device_list_lock = false;
        private void UpdateDeviceList()
        {
            
            if(update_device_list_lock)
            {
                return;
            }
            update_device_list_lock = true;

            new System.Threading.Thread(() =>
            {
                deviceList.Clear();

                deviceList.AddRange(PMD2_Device.GetAllDevices());
                deviceList.AddRange(PMD_USB_Device.GetAllDevices());

                comboBoxPorts.Invoke((MethodInvoker)delegate
                {
                    comboBoxPorts.Items.Clear();

                    foreach (IPMD_Device device in deviceList)
                    {

                        string description = device.Name;

                        if (device is PMD2_Device pmd2_device)
                        {
                            description += $" ({pmd2_device.Port})";
                        } 
                        else if (device is PMD_USB_Device pmd_usb_device)
                        {
                            description += $" ({pmd_usb_device.Port})";
                        }

                        comboBoxPorts.Items.Add(description);

                    }

                    if (comboBoxPorts.Items.Count > 0)
                    {
                        comboBoxPorts.SelectedIndex = 0;
                    }
                });

                update_device_list_lock = false;

            }).Start();

        }

        bool track = false;
        private void Monitor_graph_MouseMove(object sender, MouseEventArgs e) {
            if(track) {
                MonitorGraph monitor_graph = (MonitorGraph)sender;
            }
        }

        private void Monitor_graph_MouseLeave(object sender, EventArgs e) {
            if(track) {
                MonitorGraph monitor_graph = (MonitorGraph)sender;
                track = false;
            }
        }

        private void Monitor_graph_MouseEnter(object sender, EventArgs e) {
            track = true;
        }

        private void FormKTH_FormClosing(object sender, FormClosingEventArgs e) {
            if(data_logger != null && data_logger.IsLogging)
            {
                data_logger.Stop();
            }

            ClosePort();
        }

        private void buttonReset_Click(object sender, EventArgs e) {

        }

        private void buttonBootloader_Click(object sender, EventArgs e) {

        }

        private void buttonStorecfg_Click(object sender, EventArgs e) {
        }

        private void buttonApply_Click(object sender, EventArgs e) {

            //WriteConfigValues(false);
            //UpdateConfigValues();

        }

        private void buttonOpenPort_Click(object sender, EventArgs e) {
           
            if(selectedDevice == null && comboBoxPorts.Items.Count > 0 && comboBoxPorts.SelectedIndex >= 0 && comboBoxPorts.SelectedIndex < comboBoxPorts.Items.Count) {

                selectedDevice = deviceList[comboBoxPorts.SelectedIndex];

                labelFwVerValue.Text = (selectedDevice.FirmwareVersion).ToString("X2");

                buttonOpenPort.Text = "Disconnect";

                buttonApplyConfig.Enabled = true;
                buttonStorecfg.Enabled = true;
                buttonHwinfo.Enabled = true;
                buttonWriteToFile.Enabled = true;
                buttonFwu.Enabled = true;
                buttonLog.Enabled = true;
                buttonReset.Enabled = true;
                buttonBootloader.Enabled = true;

                data_logger = new DataLogger(selectedDevice.Name);

                //UpdateConfigValues();

                int graph_height = 100;
                int graph_width = panelMonitoring.Width / 2 - 20;

                foreach (Sensor sensor in selectedDevice.Sensors)
                {
                    MonitorGraph monitor_graph = new MonitorGraph(sensor, graph_width, graph_height);
                    monitor_graph.MaxPoints = (int)((double)numericUpDownDuration.Value * 1000.0 / monitoring_interval);
                    graphList.Add(monitor_graph);
                    panelMonitoring.Controls.Add(monitor_graph);
                    data_logger.AddLogItem(sensor);
                }

                StartMonitoring();

            } else {
                ClosePort();
            }
        }

        private void ClosePort()
        {
            if(selectedDevice != null)
            {
                selectedDevice.StopMonitoring();
            }

            selectedDevice = null;

            graphList.Clear();
            panelMonitoring.Controls.Clear();

            buttonOpenPort.Text = "Connect";
            buttonApplyConfig.Enabled = false;
            buttonStorecfg.Enabled = false;
            buttonHwinfo.Enabled = false;
            buttonWriteToFile.Enabled = false;
            buttonFwu.Enabled = false;
            buttonLog.Enabled = false;
            buttonReset.Enabled = false;
            buttonBootloader.Enabled = false;

        }

        private void buttonRefreshPorts_Click(object sender, EventArgs e) {
            
            UpdateDeviceList();

        }

        private void buttonLog_Click(object sender, EventArgs e) {
            
        }

        private void buttonWriteToFile_Click(object sender, EventArgs e)
        {

        }

        private void buttonFwu_Click(object sender, EventArgs e)
        {
        }

        private void buttonHwinfo_Click(object sender, EventArgs e)
        {
            if(data_logger == null)
            {
                return;
            }

            if(data_logger.IsLogging)
            {
                data_logger.Stop();
                buttonHwinfo.Text = "Start";
                return;
            }

            data_logger.SetHwinfo(true);
            try
            {
                data_logger.Start();
                buttonHwinfo.Text = "Stop";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        int monitoring_interval = 100;

        private void numericUpDownMonitoringInterval_ValueChanged(object sender, EventArgs e)
        {
            UpdateGraphTiming();
        }

        private void numericUpDownDuration_ValueChanged(object sender, EventArgs e)
        {
            UpdateGraphTiming();
        }

        private void UpdateGraphTiming()
        {
            int time = (int)numericUpDownDuration.Value;
            monitoring_interval = (int)numericUpDownMonitoringInterval.Value;
            if (monitoring_interval < 1) monitoring_interval = 1;
            double num_points = time / (monitoring_interval / 1000.0);

            foreach (MonitorGraph monitor_graph in graphList)
            {
                monitor_graph.MaxPoints = (int)num_points;
            }

            if (selectedDevice != null)
            {
                selectedDevice.MonitoringInterval = monitoring_interval;
            }
        }

        private void buttonCal_Click(object sender, EventArgs e)
        {
            if(selectedDevice is PMD2_Device pmd2_device)
            {
                FormCalPMD2 formCalPMD2 = new FormCalPMD2(pmd2_device);
                formCalPMD2.Show();
            }
        }
    }
}