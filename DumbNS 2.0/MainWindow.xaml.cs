using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Data.SQLite;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DumbNS_2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int? SelectedDns { get; set; }
        public string? DnsName { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dumbData = Path.Combine(path, "DumbGroup");

            var db = Path.Combine(dumbData, "DnsData.sqlite");

            if (File.Exists(db))
            {
                ReadDatabase(CreateConnection());
            }
            else
            {
                Directory.CreateDirectory(dumbData);
                SQLiteConnection.CreateFile(db);

                SQLiteConnection sqlite_conn;
                sqlite_conn = CreateConnection();


                string sql = "CREATE TABLE data (name VARCHAR(20), dns1 VARCHAR(20), dns2 VARCHAR(20))";
                SQLiteCommand command = new SQLiteCommand(sql, sqlite_conn);
                command.ExecuteNonQuery();


                //Filling db with default dns table
                CreateTable(sqlite_conn, "Google", "8.8.8.8,8.8.4.4");
                CreateTable(sqlite_conn, "Cloudflare", "1.1.1.1,1.0.0.1");
                CreateTable(sqlite_conn, "Shecan", "178.22.122.100,185.51.200.2");
                CreateTable(sqlite_conn, "Electro", "78.157.42.100,78.157.42.101");
                CreateTable(sqlite_conn, "Radar+", "10.202.10.10,10.202.10.11");

                ReadDatabase(sqlite_conn);
            }


            async Task Start()
            {
                while (true)
                {
                    var CurrentInterface = GetActiveEthernetOrWifiNetworkInterface();

                    if (CurrentInterface == null)
                    {
                        pingLabel.Content = "No Connection. Retrying...";
                        statusBar.Content = "No adapter detected!";
                        statusBar.Background = Brushes.Red;

                        await Task.Delay(2000);
                        continue;
                    }

                    statusBar.Content = "Idle";
                    statusBar.Background = Brushes.Yellow;

                    Ping ping = new Ping();
                    //PingReply reply = ping.Send("8.8.8.8", 1000);
                    var reply = await ping.SendPingAsync("8.8.8.8", 1000);
                    string pingMs = reply.RoundtripTime.ToString();

                    if (pingMs == "0")
                    {
                        pingLabel.Content = "No Connection. Retrying...";
                        await Task.Delay(2000);
                    }
                    else
                    {
                        pingLabel.Content = "         " + pingMs + " ms";
                        await Task.Delay(2000);
                    }

                }
            }
            Start();
        }

        static SQLiteConnection CreateConnection()
        {

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dumbData = Path.Combine(path, "DumbGroup");

            var db = Path.Combine(dumbData, "DnsData.sqlite");

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection($"Data Source={db}; Version = 3; New = True; Compress = True; ");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                
            }
            return sqlite_conn;
        }

        public void CreateTable(SQLiteConnection conn, string name, string dns)
        {
            string dns1 = dns.Split(',')[0];
            string dns2 = dns.Split(',')[1];
            string sql = $"INSERT INTO data (name, dns1, dns2) VALUES ('{name}', '{dns1}', '{dns2}')";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.ExecuteNonQuery();
        }

        public void ReadDatabase(SQLiteConnection conn)
        {
            string sql = "SELECT * FROM data";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while(reader.Read())
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = reader["name"] + "\n" + reader["dns1"] + " , " + reader["dns2"];
                dnsSelector.Items.Add(comboBoxItem);
                //dnsSelector.Items.Add(reader["name"]);
            }
        }

        public static NetworkInterface GetActiveEthernetOrWifiNetworkInterface()
        {
            var Nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));

            return Nic;
        }

        public void SetDNS(string DnsString, string DnsName = "DNS")
        {
            var CurrentInterface = GetActiveEthernetOrWifiNetworkInterface();
            if (CurrentInterface == null)
            {
                statusBar.Content = "No adapter detected!";
                statusBar.Background = Brushes.Red;

                return;
            }

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Description"].ToString().Equals(CurrentInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = DnsString.Split(',');
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }

            statusBar.Background = Brushes.Green;
            statusBar.Content = DnsName + " Set!";
        }

        public void Dns_Selector_Changed(object sender, SelectionChangedEventArgs e)
        {
            SelectedDns = dnsSelector.SelectedIndex;

            ComboBoxItem cbi = (ComboBoxItem)dnsSelector.SelectedItem;
            
            if (cbi == null)
            {
                dnsSelector.SelectedIndex = 0;
                return;
            }

            DnsName = cbi.Content.ToString();
            DnsName = DnsName.Split('\n')[0];

        }

        private void Dns_Set_Button(object sender, RoutedEventArgs e)
        {
            statusBar.Background = new SolidColorBrush(Color.FromRgb(230, 230, 250));
            statusBar.Content = "Running...";
            if (DnsName == null || DnsName == "" || DnsName == " ")
            {
                statusBar.Background = Brushes.Red;
                statusBar.Content = "Didn't set";
            }
            else
            {
                SQLiteConnection sqlite_conn;
                sqlite_conn = CreateConnection();


                string sql = $"SELECT * FROM data WHERE name = '{DnsName}'";
                SQLiteCommand command = new SQLiteCommand(sql, sqlite_conn);

                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (string.IsNullOrEmpty(reader.GetString(2)))
                    {
                        string dns = reader.GetString(1);
                        SetDNS(dns, reader.GetString(0));
                    }
                    else
                    {
                        string dns = reader.GetString(1) + ',' + reader.GetString(2);
                        SetDNS(dns, reader.GetString(0));
                    }
                }
            }
        }

            private void Dns_Unset_Button(object sender, RoutedEventArgs e)
        {
            var CurrentInterface = GetActiveEthernetOrWifiNetworkInterface();
            if (CurrentInterface == null)
            {
                statusBar.Content = "No adapter detected!";
                statusBar.Background = Brushes.Red;

                return;
            }

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Description"].ToString().Equals(CurrentInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = null;
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }

            statusBar.Background = new SolidColorBrush(Color.FromRgb(230, 230, 250));
            statusBar.Content = "DNS Removed!";
        }

        public void GetDnsAdress()
        {
            dnsOrigin.Content = "";
            var CurrentInterface = GetActiveEthernetOrWifiNetworkInterface();
            if (CurrentInterface == null)
            {
                statusBar.Content = "No adapter detected!";
                statusBar.Background = Brushes.Red;

                return;
            }

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Description"].ToString().Equals(CurrentInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            string dnss = "";
                            foreach (string str in (Array)(objMO.Properties["DNSServerSearchOrder"].Value))
                            {
                                dnss += str + ",";
                            }
                            string[] twoDns = dnss.Split(',');

                            SQLiteConnection sqlite_conn;
                            sqlite_conn = CreateConnection();


                            string sql = $"SELECT * FROM data WHERE dns1 = '{twoDns[0]}' OR dns2 = '{twoDns[0]}'";
                            SQLiteCommand command = new SQLiteCommand(sql, sqlite_conn);

                            SQLiteDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dnsOrigin.Content = reader.GetString(0);
                            }

                            dnsOne.Content = twoDns[0];
                            dnsTwo.Content = twoDns[1];
                        }
                    }
                }
            }
        }

        private void Dns_Get_Button(object sender, RoutedEventArgs e)
        {
            GetDnsAdress();
        }

        public string IsIp(string ip)
        {
            string[] splitIp = ip.Split('.');
            if (splitIp.Length == 4 && splitIp.All(x => Int32.TryParse(x, out _)) && splitIp.All(x => Int32.Parse(x) < 256))
            {
                return ip;
            }
            else
            {
                statusBar.Content = "Enter a valid ip";
                statusBar.Background = Brushes.Orange;
                statusBar.Content = "Enter a valid ip";
                statusBar.Background = Brushes.Orange;
                statusBar.Content = "Enter a valid ip";
                statusBar.Background = Brushes.Orange;

                return null;
            }
        }

        public void Confirm_Dns_Button(object sender, RoutedEventArgs e)
        {
            if (dnsOneTextBox.Text == "")
            {
                string dnsOne = IsIp(dnsOneTextBox.Text);
                return;
            }

            if (dnsOneTextBox.Text != "" && dnsTwoTextBox.Text != "")
            {
                string dnsOne = IsIp(dnsOneTextBox.Text);
                string dnsTwo = IsIp(dnsTwoTextBox.Text);

                if (dnsOne != null && dnsTwo != null)
                {
                    string dns = dnsOne + "," + dnsTwo;
                    SetDNS(dns);
                }
            }

            if (dnsOneTextBox.Text != "" && dnsTwoTextBox.Text == "")
            {
                string dnsOne = IsIp(dnsOneTextBox.Text);

                if (dnsOne != null)
                {
                    SetDNS(dnsOne);
                }
            }

            dnsOneTextBox.Text = "";
            dnsTwoTextBox.Text = "";
        }

        public void Dns_Flush_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                Process netCmd = new Process();
                netCmd.StartInfo.FileName = "cmd.exe";
                netCmd.StartInfo.Arguments = "/c ipconfig /flushdns";
                netCmd.StartInfo.CreateNoWindow = true;
                netCmd.Start();

                statusBar.Content = "DNS Flushed";
                statusBar.Background = Brushes.Green;
                statusBar.Content = "DNS Flushed";
                statusBar.Background = Brushes.Green;

                return;

            } catch(Exception err)
            {
                statusBar.Content = "Error";
                statusBar.Background = Brushes.Red;

                return;
            }
            
        }

        public void Network_Reset_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                Process netCmd = new Process();
                netCmd.StartInfo.FileName = "cmd.exe";
                netCmd.StartInfo.Arguments = "/c netsh winsock reset";
                netCmd.StartInfo.CreateNoWindow = true;
                netCmd.Start();
                netCmd.WaitForExit();
                netCmd.StartInfo.FileName = "cmd.exe";
                netCmd.StartInfo.Arguments = "/c netsh int ip reset";
                netCmd.StartInfo.CreateNoWindow = true;
                netCmd.Start();
                netCmd.WaitForExit();
                netCmd.StartInfo.FileName = "cmd.exe";
                netCmd.StartInfo.Arguments = "/c ipconfig /release";
                netCmd.StartInfo.CreateNoWindow = true;
                netCmd.Start();
                netCmd.WaitForExit();
                netCmd.StartInfo.FileName = "cmd.exe";
                netCmd.StartInfo.Arguments = "/c ipconfig /renew";
                netCmd.StartInfo.CreateNoWindow = true;
                netCmd.Start();
                netCmd.WaitForExit();

                MessageBox.Show("Network Reset Successfully!");

                return;
            } catch(Exception err)
            {
                statusBar.Content = "Error";
                statusBar.Background = Brushes.Red;

                return;
            }
        }

        public void Add_Dns_Button(object sender, RoutedEventArgs e)
        {
            AddingDns addDns = new AddingDns(CreateConnection());
            addDns.ShowDialog();
            dnsSelector.Items.Clear();
            ReadDatabase(CreateConnection());
        }

        public void Remove_Dns_Button(object sender, RoutedEventArgs e)
        {
            string sql = $"DELETE FROM data WHERE name = '{DnsName}'";
            SQLiteCommand command = new SQLiteCommand(sql, CreateConnection());
            command.ExecuteNonQuery();

            dnsSelector.Items.Clear();
            ReadDatabase(CreateConnection());

            statusBar.Background = new SolidColorBrush(Color.FromRgb(230, 230, 250));
            statusBar.Content = "DNS Removed!";
        }

    }
}
