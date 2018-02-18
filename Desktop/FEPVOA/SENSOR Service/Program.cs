using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraCharts;

namespace FEPVOASensor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            new Main();
            Application.Run();
        }
    }

    public class Main
    {
        public Main()
        {
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                MSG_Error = "Another proccess already running!";
                System.Threading.Thread.Sleep(2000);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }

            _Timer.Enabled = true;
            _Timer.Tick += _Timer_Tick;

            _TimerEveryDate.Enabled = true;
            _TimerEveryDate.Tick += _TimerEveryDate_Tick;

            SensorEmailSetting();
            LastSendMail_BB = DateTime.Now;
            LastSaveDataLog_BB = DateTime.Now;
            LastSendMail_HCM = DateTime.Now;
            LastSaveDataLog_HCM = DateTime.Now;
            ReportTimeFormat = ReadSetting("DailyReportTime");
        }

        #region Members
        SQLServices _SQLServices = new SQLServices();
        MailService _MailService = new MailService();
        Timer _Timer = new Timer { Interval = Convert.ToInt32(ReadSetting("Time_Check")) * 1000, Enabled = false };
        Timer _TimerEveryDate = new Timer { Interval = 1000, Enabled = false };
        public System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
        static ASCIIEncoding encoding = new ASCIIEncoding();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string MSG_Info
        {
            set
            {
                Console.WriteLine(value);
                log.Info(value);
            }
        }

        string MSG_Error
        {
            set
            {
                Console.WriteLine(value);
                log.Error(value);
            }
        }
        #endregion

        #region Event

        void _Timer_Tick(object sender, EventArgs e)
        {
            decimal Temp, Humid;
            string msg = "";

            //BauBang
            try
            {
                //1.Get Data
                GetDataFromSensor(Sensor_BB.IP, Sensor_BB.Port, Sensor_BB.SerialNo, out Temp, out Humid);

                if ((DateTime.Now - LastSaveDataLog_BB) >= new TimeSpan(0, 10, 0)) //10 Minutes
                {
                    if (_SQLServices.SaveLogs_Sensor(Sensor_BB.SerialNo, Temp, Humid, out msg))
                    {
                        MSG_Info = "Sensor " + Sensor_BB.SerialNo + " - Temp: " + string.Format("{0:0.0}", Temp) + " , - Humidity: " + string.Format("{0:0.0}", Humid);
                        LastSaveDataLog_BB = DateTime.Now;
                    }
                    else
                        MSG_Error = msg;
                }
                msg = "";

                //2. Check Humidity And Temperature
                if (_SQLServices.CheckProblemHumidTemp4Message(Sensor_BB.SerialNo, Temp, Humid, DateTime.Now.ToString("yyyyMMddHHmmss"), out msg))
                {

                    //3. Send Email Maximum 1 Emal/Hour
                    if ((DateTime.Now - LastSendMail_BB) >= new TimeSpan(1, 0, 0)) //1 Hour
                    {
                        MSG_Info = msg;
                        DateTime Now = DateTime.Now;
                        Chart2ImageDetails(Sensor_BB.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                        System.Threading.Thread.Sleep(1000);
                        string Path = string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\ChartDetails_{0}_{1}.jpg", Sensor_BB.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                        if (_MailService.SendTaskMailNet(ReadSetting("EmailTo"), "FEPV MIS - BauBang Sensor Alarm At: " + Now.ToString("yyyy-MM-dd HH:mm:ss"), msg, Path, out msg))
                        {
                            LastSendMail_BB = Now;
                            MSG_Info = "Send mail success to: " + ReadSetting("EmailTo");
                        }
                        else
                        {
                            MSG_Error = msg;
                        }
                    }
                }
                else
                    MSG_Error = msg;
            }
            catch (Exception) { }


            //HCM City
            try
            {
                Temp = 0; Humid = 0;
                msg = "";

                //1.Get Data
                GetDataFromSensor(Sensor_HCM.IP, Sensor_HCM.Port, Sensor_HCM.SerialNo, out Temp, out Humid);

                if ((DateTime.Now - LastSaveDataLog_HCM) >= new TimeSpan(0, 10, 0)) //10 Minutes
                {
                    if (_SQLServices.SaveLogs_Sensor(Sensor_HCM.SerialNo, Temp, Humid, out msg))
                    {
                        MSG_Info = "Sensor " + Sensor_HCM.SerialNo + " - Temp: " + string.Format("{0:0.0}", Temp) + " , - Humidity: " + string.Format("{0:0.0}", Humid);
                        LastSaveDataLog_BB = DateTime.Now;
                    }
                    else
                        MSG_Error = msg;
                }
                msg = "";

                //2. Check Humidity And Temperature
                if (_SQLServices.CheckProblemHumidTemp4Message(Sensor_HCM.SerialNo, Temp, Humid, DateTime.Now.ToString("yyyyMMddHHmmss"), out msg))
                {

                    //3. Send Email Maximum 1 Emal/Hour
                    if ((DateTime.Now - LastSendMail_HCM) >= new TimeSpan(1, 0, 0)) //1 Hour
                    {
                        MSG_Info = msg;
                        DateTime Now = DateTime.Now;
                        Chart2ImageDetails(Sensor_HCM.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                        System.Threading.Thread.Sleep(1000);
                        string Path = string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\ChartDetails_{0}_{1}.jpg", Sensor_HCM.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                        if (_MailService.SendTaskMailNet(ReadSetting("EmailTo"), "FEPV MIS - HCM Sensor Alarm At: " + Now.ToString("yyyy-MM-dd HH:mm:ss"), msg, Path, out msg))
                        {
                            LastSendMail_HCM = Now;
                            MSG_Info = "Send mail success to: " + ReadSetting("EmailTo");
                        }
                        else
                        {
                            MSG_Error = msg;
                        }
                    }
                }
                else
                    MSG_Error = msg;
            }
            catch (Exception) { }
        }

        void _TimerEveryDate_Tick(object sender, EventArgs e)
        {

            if (DateTime.Now.ToString("HH:mm:ss") == ReportTimeFormat)
            {
                //BauBang
                try
                {
                    DateTime Now = DateTime.Now;
                    decimal AvgTemp = 0, AvgHumid = 0;
                    string msg = "";
                    _SQLServices.GettAvgTempAndHumidity(Sensor_BB.SerialNo, Now.Date, Now, out AvgTemp, out AvgHumid, out msg);
                    MSG_Info = msg;
                    Chart2Image(Sensor_BB.SerialNo, Now);
                    System.Threading.Thread.Sleep(1000);
                    string Path = string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\Chart_{0}_{1}.jpg", Sensor_BB.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                    if (_MailService.SendTaskMailNet(ReadSetting("EmailTo"), "FEPV MIS - BauBang Sensor Daily Report " + Now.ToString("yyyy-MM-dd HH:mm:ss"), msg, Path, out msg))
                        MSG_Info = "Send mail success to: " + ReadSetting("EmailTo");
                    else
                        MSG_Error = msg;
                }
                catch (Exception) { }

                //HCM City
                try
                {
                    DateTime Now = DateTime.Now;
                    decimal AvgTemp = 0, AvgHumid = 0;
                    string msg = "";
                    _SQLServices.GettAvgTempAndHumidity(Sensor_HCM.SerialNo, Now.Date, Now, out AvgTemp, out AvgHumid, out msg);
                    MSG_Info = msg;
                    Chart2Image(Sensor_HCM.SerialNo, Now);
                    System.Threading.Thread.Sleep(1000);
                    string Path = string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\Chart_{0}_{1}.jpg", Sensor_HCM.SerialNo, Now.ToString("yyyyMMddHHmmss"));
                    if (_MailService.SendTaskMailNet(ReadSetting("EmailTo"), "FEPV MIS - HCM City Sensor Daily Report " + Now.ToString("yyyy-MM-dd HH:mm:ss"), msg, Path, out msg))
                        MSG_Info = "Send mail success to: " + ReadSetting("EmailTo");
                    else
                        MSG_Error = msg;
                }
                catch (Exception) { }
            }
        }

        #endregion

        DateTime LastSendMail_BB { get; set; }

        DateTime LastSaveDataLog_BB { get; set; }

        DateTime LastSendMail_HCM { get; set; }

        DateTime LastSaveDataLog_HCM { get; set; }

        string ReportTimeFormat { get; set; }

        bool GetDataFromSensor(string IP, int Port, string SerialNo, out decimal Temp, out decimal Humid)
        {
            Temp = 0;
            Humid = 0;
            bool rValue = false;

            try
            {
                // 1.Connect
                if (client.Connected)
                    client.Close();

                client = new System.Net.Sockets.TcpClient();
                client.Connect(IP, Port);
                System.IO.Stream stream = client.GetStream();

                // 2. send
                //string str2 = "{(HE20151114gn)}~";//BauBang
                //string str2 = "{(HE20151120gn)}~";//TP HCM
                string str2 = "{(" + SerialNo + "gn)}~";
                byte[] data = encoding.GetBytes(str2);

                stream.Write(data, 0, data.Length);

                // 3. receive
                data = new byte[29];
                stream.Read(data, 0, 29);
                byte[] byteTemp = new byte[4] { data[16], data[17], data[18], data[19] };
                int temp = BitConverter.ToInt32(byteTemp, 0);

                byte[] byteHumi = new byte[4] { data[20], data[21], data[22], data[23] };
                int humi = BitConverter.ToInt32(byteHumi, 0);

                Temp = (decimal)temp / 10;
                Humid = (decimal)humi / 10;
                rValue = true;
            }
            catch (Exception ex) { MSG_Error = ex.Message; }

            return rValue;
        }

        /// <summary>
        /// Chart From 00:00:00 to 23:59:59
        /// </summary>
        void Chart2Image(string SensorID, DateTime Now)
        {
            ChartControl lineChart = new ChartControl();
            lineChart.Size = new Size(1000, 500);

            Series series1 = new Series("Temperature (C)", ViewType.Line);
            Series series2 = new Series("Humidity (%)", ViewType.Line);

            DataTable dt = _SQLServices.GetDataForChart(SensorID, Now.Date, Now);

            foreach (DataRow dr in dt.Rows)
            {
                series1.Points.Add(new SeriesPoint(Convert.ToDateTime(dr["Time"]), Convert.ToDouble(dr["Temperature"])));
                series2.Points.Add(new SeriesPoint(Convert.ToDateTime(dr["Time"]), Convert.ToDouble(dr["Humidity"])));
            }

            lineChart.Series.Add(series2);
            lineChart.Series.Add(series1);

            series1.ArgumentScaleType = ScaleType.DateTime;
            XYDiagram diagram = (XYDiagram)lineChart.Diagram;
            diagram.AxisX.DateTimeMeasureUnit = DateTimeMeasurementUnit.Minute;
            diagram.AxisX.DateTimeOptions.Format = DateTimeFormat.ShortTime;

            //((LineSeriesView)series1.View).LineMarkerOptions.Kind = MarkerKind.Circle;
            //((LineSeriesView)series1.View).LineMarkerOptions.Visible = false;
            ((LineSeriesView)series1.View).LineStyle.DashStyle = DashStyle.Solid;
            ((LineSeriesView)series1.View).Color = Color.Red;
            ((LineSeriesView)series2.View).LineStyle.DashStyle = DashStyle.Solid;
            ((LineSeriesView)series2.View).Color = Color.Blue;

            lineChart.Titles.Add(new ChartTitle());
            lineChart.Titles[0].Text = "Sensor Data Today: " + DateTime.Now.ToString("dd-MM-yyyy");

            lineChart.Dock = DockStyle.Fill;
            lineChart.ExportToImage(string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\Chart_{0}_{1}.jpg", SensorID, Now.ToString("yyyyMMddHHmmss")), System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        /// <summary>
        /// Chart From Last 1 Hour
        /// </summary>
        void Chart2ImageDetails(string SensorID, string PathName)
        {
            ChartControl lineChart = new ChartControl();
            lineChart.Size = new Size(1000, 500);

            Series series1 = new Series("Temperature (C)", ViewType.Line);
            Series series2 = new Series("Humidity (%)", ViewType.Line);

            DataTable dt = _SQLServices.GetDataForChart(SensorID, DateTime.Now.AddHours(-1), DateTime.Now);

            foreach (DataRow dr in dt.Rows)
            {
                series1.Points.Add(new SeriesPoint(Convert.ToDateTime(dr["Time"]), Convert.ToDouble(dr["Temperature"])));
                series2.Points.Add(new SeriesPoint(Convert.ToDateTime(dr["Time"]), Convert.ToDouble(dr["Humidity"])));
            }

            lineChart.Series.Add(series2);
            lineChart.Series.Add(series1);

            series1.ArgumentScaleType = ScaleType.DateTime;
            XYDiagram diagram = (XYDiagram)lineChart.Diagram;
            diagram.AxisX.DateTimeMeasureUnit = DateTimeMeasurementUnit.Minute;
            diagram.AxisX.DateTimeOptions.Format = DateTimeFormat.ShortTime;

            //((LineSeriesView)series1.View).LineMarkerOptions.Kind = MarkerKind.Circle;
            ((LineSeriesView)series1.View).LineStyle.DashStyle = DashStyle.Solid;
            ((LineSeriesView)series2.View).LineStyle.DashStyle = DashStyle.Solid;

            lineChart.Titles.Add(new ChartTitle());
            lineChart.Titles[0].Text = string.Format("Details From {0} To {1} {2} ", DateTime.Now.AddHours(-1).ToString("HH:mm:ss"), DateTime.Now.ToString("HH:mm:ss"), DateTime.Now.ToString("dd-MM-yyyy"));

            // Add the chart to the form.
            lineChart.Dock = DockStyle.Fill;
            lineChart.ExportToImage(string.Format(@"C:\inetpub\wwwroot\SENSOR_Image\ChartDetails_{0}_{1}.jpg", SensorID, PathName), System.Drawing.Imaging.ImageFormat.Jpeg);
        }


        void SensorEmailSetting()
        {
            _MailService._EmailSetting = new EmailSetting
            {
                Host = ReadSetting("Host"),
                Domain = ReadSetting("Domain"),
                Port = Convert.ToInt32(ReadSetting("Port")),
                User = ReadSetting("User"),
                Pass = ReadSetting("Pass"),
                EmailFrom = ReadSetting("EmailFrom")
            };
        }

        SensorParameter Sensor_BB
        {
            get
            {
                return new SensorParameter
                    {
                        SerialNo = ReadSetting("Sensor_SerialNo_BB"),
                        IP = ReadSetting("Sensor_IP_BB"),
                        Port = Convert.ToInt32(ReadSetting("Sensor_Port_BB"))
                    };
            }
        }

        SensorParameter Sensor_HCM
        {
            get
            {
                return new SensorParameter
                {
                    SerialNo = ReadSetting("Sensor_SerialNo_HCM"),
                    IP = ReadSetting("Sensor_IP_HCM"),
                    Port = Convert.ToInt32(ReadSetting("Sensor_Port_HCM"))
                };
            }
        }

        static string ReadSetting(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[key] ?? "Not Found";
        }
    }

    public class SensorParameter
    {
        public string SerialNo { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
    }

}
