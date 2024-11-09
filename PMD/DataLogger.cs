using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Microsoft.Win32;

namespace PMD
{
    /*public class LoggingItem
    {

        public int ParentId;
        public int Id;
        public bool Enabled;
        public string Description;
        public string DescriptionShort;
        public string Unit;
        public double Value;
        public string HwinfoKey;

        public LoggingItem(int id, string desc, string desc_short, string unit)
        {
            Id = id;
            Description = desc;
            DescriptionShort = desc_short;
            Unit = unit;
            Enabled = true;
        }

        override public string ToString()
        {
            return Description;
        }

    }*/

    public class LoggingItem
    {
        public Sensor Sensor { get; set; }
        public bool Enabled;
        public string HwinfoKey;

        public LoggingItem(Sensor sensor)
        {
            Sensor = sensor;
            Enabled = true;
        }

        override public string ToString()
        {
            return Sensor.DescriptionLong;
        }

    }

    public class DataLogger
    {

        private const string HWINFO_REG_KEY = "Software\\HWiNFO64\\Sensors\\Custom";
        public string DEVICE_NAME;

        public List<LoggingItem> LoggingItemList;

        public bool IsLogging;

        int IdCounter;
        string FilePath;
        bool CsvMode;
        bool Hwinfo;

        private CultureInfo culture_info;

        public DataLogger(string device_name)
        {
            DEVICE_NAME = device_name;

            IsLogging = false;
            IdCounter = 0;
            LoggingItemList = new List<LoggingItem>();
            culture_info = new CultureInfo("en-US");
        }

        public void SetHwinfo(bool hwinfo)
        {
            Hwinfo = hwinfo;
        }

        public void SetCsv(string path, bool csv)
        {
            FilePath = path;
            CsvMode = csv;
        }

        public string GetFilePath()
        {
            return FilePath;
        }

        public void AddLogItem(Sensor sensor)
        {
            LoggingItemList.Add(new LoggingItem(sensor));
        }

        public bool RemoveLogItem(Sensor sensor)
        {
            if(LoggingItemList == null)
            {
                return false;
            }

            LoggingItem item = LoggingItemList.Find(x => x.Sensor == sensor);
            if(item != null) {
                LoggingItemList.Remove(item);
                return true;
            }

            return false;
        }

        public RegistryKey hwinfo_reg_key;

        public void Start()
        {
            if (!string.IsNullOrEmpty(FilePath) && CsvMode)
            {
                string header_line = "Timestamp,";
                foreach (LoggingItem logging_item in LoggingItemList)
                {
                    if (logging_item.Enabled)
                    {
                        header_line += logging_item.Sensor.DescriptionLong + ",";
                    }
                }
                header_line = header_line.Substring(0, header_line.Length - 1);
                header_line += Environment.NewLine;
                File.WriteAllText(FilePath, header_line);
            }

            if (Hwinfo)
            {
                try
                {
                    hwinfo_reg_key = Registry.CurrentUser.OpenSubKey(HWINFO_REG_KEY, true);
                    if (hwinfo_reg_key == null)
                    {
                        hwinfo_reg_key = Registry.CurrentUser.CreateSubKey(HWINFO_REG_KEY, true);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                if (hwinfo_reg_key == null)
                {
                    throw new Exception("Error accessing registry.");
                }

                try
                {

                    if (hwinfo_reg_key.OpenSubKey(DEVICE_NAME) != null)
                    {
                        hwinfo_reg_key.DeleteSubKeyTree("");
                    }

                    hwinfo_reg_key = hwinfo_reg_key.CreateSubKey(DEVICE_NAME);

                    if (hwinfo_reg_key == null)
                    {
                        throw new Exception("Error accessing registry.");
                    }

                    int temp_idx = 0;
                    int current_idx = 0;
                    int volt_idx = 0;
                    int power_idx = 0;
                    int fan_idx = 0;
                    int usage_idx = 0;
                    foreach (LoggingItem logging_item in LoggingItemList)
                    {

                        switch (logging_item.Sensor.Unit)
                        {
                            case "°C":
                                logging_item.HwinfoKey = $"Temp{temp_idx++}"; break;
                            case "A":
                                logging_item.HwinfoKey = $"Current{current_idx++}"; break;
                            case "V":
                                logging_item.HwinfoKey = $"Volt{volt_idx++}"; break;
                            case "W":
                                logging_item.HwinfoKey = $"Power{power_idx++}"; break;
                            case "RPM":
                                logging_item.HwinfoKey = $"Fan{fan_idx++}"; break;
                            case "%":
                                logging_item.HwinfoKey = $"Usage{usage_idx++}"; break;
                            default:
                                logging_item.Enabled = false; break;
                        }

                        if (logging_item.Enabled)
                        {
                            RegistryKey key = hwinfo_reg_key.CreateSubKey(logging_item.HwinfoKey);
                            if (key == null)
                            {
                                throw new Exception("Error accessing registry.");
                            }
                            key.SetValue("Name", logging_item.Sensor.DescriptionLong, RegistryValueKind.String);
                            key.SetValue("Value", "0", RegistryValueKind.String);
                        }

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

            }

            IsLogging = true;
        }

        public void WriteEntry()
        {

            string csv_str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + ",";
            
            // Build output
            foreach (LoggingItem logging_item in LoggingItemList)
            {
                if (logging_item.Enabled)
                {
                    string value = logging_item.Sensor.Value.ToString(culture_info);

                    if (CsvMode)
                    {
                        csv_str += value + ",";
                    }

                    if (Hwinfo)
                    {
                        RegistryKey key = hwinfo_reg_key.OpenSubKey(logging_item.HwinfoKey, true);
                        key.SetValue("Value", value, RegistryValueKind.String);
                    }

                }
            }

            // Write to file
            if (CsvMode)
            {
                csv_str = csv_str.Substring(0, csv_str.Length - 1);
                csv_str += Environment.NewLine;
                try
                {
                    File.AppendAllText(FilePath, csv_str);
                }
                catch { }
            }

        }

        public void Stop()
        {

            // Remove registry values
            if (Hwinfo)
            {
                RemoveHwinfoRegEntry();
            }

            IsLogging = false;
        }

        private void RemoveHwinfoRegEntry()
        {
            hwinfo_reg_key.DeleteSubKeyTree("");
        }
    }
}