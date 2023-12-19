﻿using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormImportIP : Form
    {
        public static readonly Regex rMatchIP = new(@"(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*\((?<Location>[^\)]*)\)|^[^\d]+(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})(?<Location>[^\d|<]+)\d+ms|^\s*(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*$|(?<IP>([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})\s*\((?<Location>[^\)]*)\)", RegexOptions.Multiline);
        public String host = string.Empty;
        public DataTable dt;

        public FormImportIP()
        {
            InitializeComponent();

            dt = new DataTable();
            dt.Columns.Add("IP", typeof(string));
            dt.PrimaryKey = new DataColumn[] { dt.Columns.Add("IpFilter", typeof(string)) };
            dt.Columns.Add("Location", typeof(string));
            dt.Columns.Add("IpLong", typeof(ulong));
            comboBox1.SelectedIndex = 0;
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = ((LinkLabel)sender).Text;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void LinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string hosts = Regex.Replace(comboBox1.Text, @"\s.+$", "").Trim();
            Clipboard.SetDataObject(hosts);
            MessageBox.Show("域名(" + hosts + ")已复制到剪贴板", "复制域名", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string content = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            string[] array = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length >= 1)
            {
                if (String.Equals(array[0].Trim(), "Akamai", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.host = "Akamai";
                }
                else
                {
                    foreach (string str in array)
                    {
                        string tmp = str.Trim();
                        if (Regex.IsMatch(tmp, @"^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$"))
                        {
                            this.host = tmp.ToLowerInvariant();
                            switch (this.host)
                            {
                                case "atum.hac.lp1.d4c.nintendo.net":
                                    this.host = "Akamai";
                                    break;
                                default:
                                    if (Regex.IsMatch(this.host, @"\.akamaihd\.net$"))
                                    {
                                        this.host = "Akamai";
                                    }
                                    break;
                            }
                            break;
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(this.host))
            {
                MessageBox.Show("提交内容不符合条件。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Match result = rMatchIP.Match(content);
            while (result.Success)
            {
                if (IPAddress.TryParse(result.Groups["IP"].Value, out IPAddress? address))
                {
                    string location = result.Groups["Location"].Value.Trim();
                    ulong ipLong = IpToLong(address);
                    string ip = address.ToString();
                    string IpFilter = address.AddressFamily == AddressFamily.InterNetwork ? Regex.Replace(ip, @"\d{0,3}$", "") : Regex.Replace(GetFullIPv6(ip), @"(:[\da-fA-F]{4}){4}$", "");
                    DataRow? dr = dt.Rows.Find(IpFilter);
                    if (dr == null)
                    {
                        dr = dt.NewRow();
                        dr["IP"] = ip;
                        dr["IpFilter"] = IpFilter;
                        dr["Location"] = Regex.Replace(location, @" ([-a-zA-Z0-9]+\.)+[a-zA-Z0-9]{2,}", "");
                        dr["IpLong"] = ipLong;
                        dt.Rows.Add(dr);
                    }
                }
                result = result.NextMatch();
            }
            this.Close();
        }

        private void LinkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "文本文件(*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new(openFileDialog.FileName);
                textBox1.Text = sr.ReadToEnd();
                sr.Close();
            }
        }

        private static ulong IpToLong(IPAddress address)
        {
            ulong num;
            byte[] addrBytes = address.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                List<byte> byteList = new(addrBytes);
                byteList.Reverse();
                addrBytes = byteList.ToArray();
            }
            if (addrBytes.Length > 8) //IPv6
            {
                num = BitConverter.ToUInt64(addrBytes, 8);
                num <<= 64;
                num += BitConverter.ToUInt64(addrBytes, 0);
            }
            else //IPv4
            {
                num = BitConverter.ToUInt32(addrBytes, 0);
            }
            return num;
        }

        private static string GetFullIPv6(string ip)
        {
            if (ip == "::")
            {
                return "0000:0000:0000:0000:0000:0000:0000:0000";
            }
            if (ip.EndsWith("::"))
            {
                ip += "0";
            }
            var arrs = ip.Split(':');
            var symbol = "::";
            var arrleng = arrs.Length;
            while (arrleng < 8)
            {
                symbol += ":";
                arrleng++;
            }
            ip = ip.Replace("::", symbol);
            var fullip = "";
            var arr = ip.Split(':');
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i].Length < 4)
                {
                    arr[i] = arr[i].PadLeft(4, '0');
                }
                fullip += arr[i] + ':';
            }
            return fullip[..^1].ToUpper();
        }
    }
}
