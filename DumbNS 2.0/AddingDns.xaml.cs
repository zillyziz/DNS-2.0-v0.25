using System.Windows;
using System.Data.SQLite;
using System.Linq;
using System;

namespace DumbNS_2._0
{
    /// <summary>
    /// Interaction logic for AddingDns.xaml
    /// </summary>
    public partial class AddingDns : Window
    {
        public SQLiteConnection SqltConnection { get; set; }
        public AddingDns(SQLiteConnection connec)
        {
            InitializeComponent();
            this.DataContext = this;

            this.SqltConnection = connec;
        }

        public void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(dnsNameTextBox.Text))
            {
                MessageBox.Show("Enter name");
            }
            else if (string.IsNullOrEmpty(dnsOneTextBox.Text))
            {
                MessageBox.Show("Enter dns 1");
            }
            else if (!string.IsNullOrEmpty(dnsOneTextBox.Text) && !string.IsNullOrEmpty(dnsTwoTextBox.Text))
            {
                string dns1 = IsIp(dnsOneTextBox.Text);
                string dns2 = IsIp(dnsTwoTextBox.Text);

                if (dns1 == null || dns2 == null)
                {
                    MessageBox.Show("Enter a valid ip");
                    return;
                }

                string dnsName = dnsNameTextBox.Text;
                string firstDns = dnsOneTextBox.Text;
                string secondDns = dnsTwoTextBox.Text;
                string checkName = CheckDatabase(SqltConnection, dnsName);

                if (!string.IsNullOrEmpty(checkName))
                {
                    MessageBox.Show("Name already exists or invalid");
                }
                else
                {
                    string sql = $"INSERT INTO data (name, dns1, dns2) VALUES ('{dnsName}', '{firstDns}', '{secondDns}')";
                    SQLiteCommand command = new SQLiteCommand(sql, SqltConnection);
                    command.ExecuteNonQuery();
                    MessageBox.Show("Added");
                }
            }
            else if (!string.IsNullOrEmpty(dnsOneTextBox.Text) && string.IsNullOrEmpty(dnsTwoTextBox.Text))
            {
                string dns1 = IsIp(dnsOneTextBox.Text);

                if (dns1 == null)
                {
                    MessageBox.Show("Enter a valid ip");
                    return;
                }

                string dnsName = dnsNameTextBox.Text;
                string dns = $"{dnsOneTextBox.Text}";
                string checkName = CheckDatabase(SqltConnection, dnsName);

                if (!string.IsNullOrEmpty(checkName))
                {
                    MessageBox.Show("Name already exists or invalid");
                }
                else
                {
                    string sql = $"INSERT INTO data (name, dns1, dns2) VALUES ('{dnsName}', '{dns}', '')";
                    SQLiteCommand command = new SQLiteCommand(sql, SqltConnection);
                    command.ExecuteNonQuery();
                    MessageBox.Show("Added");
                }
            }
            else
            {
                MessageBox.Show("Unknown Error");
            }
        }

        public string CheckDatabase(SQLiteConnection conn, string name)
        {
            string dnsName = "";
            string sql = $"SELECT * FROM data WHERE name = '{name}'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                dnsName = reader.GetString(0);
            }
            return dnsName;
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
                return null;
            }
        }
    }
}