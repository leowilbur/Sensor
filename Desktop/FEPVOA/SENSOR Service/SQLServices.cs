using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBear.Data;
using System.Data;

namespace FEPVOASensor
{
    public class SQLServices
    {
        public Gateway gate = new Gateway("FEPVOA");

        public bool SaveLogs_Sensor(string SensorName, decimal Temp, decimal Humid, out string msg)
        {
            bool rValue = true;
            msg = "";
            try
            {
                gate.DbHelper.ExecuteNonQuery(@"
                INSERT INTO [dbo].[SENSOR_DataLogs]
                           ([Time]
                           ,[Sensor]
                           ,[Temperature]
                           ,[Humidity])
                     VALUES
                           (@Time
                           ,@Sensor
                           ,@Temperature
                           ,@Humidity)", new object[] { DateTime.Now, SensorName, Temp, Humid });

            }
            catch (Exception ex)
            {
                rValue = false;
                msg = ex.Message;

            }
            return rValue;

        }

        public bool CheckProblemHumidTemp4Message(string SensorID, decimal Temp, decimal Humid, string ImageName, out string msg)
        {
            bool rValue = true;
            msg = "";
            try
            {
                string Msg = gate.DbHelper.ExecuteStoredProcedure("SENSOR_CheckRange", new string[] { "SensorID", "Temp", "Humid", "ImageName" }, new object[] { SensorID, Temp, Humid, ImageName }).Tables[0].Rows[0][0].ToString();
                if (string.IsNullOrEmpty(Msg))
                    rValue = false;
                else
                    msg = Msg;
            }
            catch (Exception ex)
            {
                rValue = false;
                msg = ex.Message;

            }
            return rValue;
        }

        public DataTable GetDataForChart(string SensorID, DateTime TimeB, DateTime TimeE)
        {
            return gate.DbHelper.Select(@"SELECT Time,Sensor,Temperature,Humidity
                                      FROM SENSOR_DataLogs WHERE Time > @TimeB AND Time < @TimeE AND Sensor = @SensorID", new object[] { TimeB, TimeE, SensorID }).Tables[0];
        }

        public void GettAvgTempAndHumidity(string SensorID, DateTime DateB, DateTime DateE, out decimal Temp, out decimal Humid, out string msg)
        {
            DataTable dt = gate.DbHelper.ExecuteStoredProcedure("SENSOR_GetAvgTempAndHumid", new string[] { "SensorID", "DateB", "DateE", "ImageName" }, new object[] {SensorID, DateB, DateE, DateE.ToString("yyyyMMddHHmmss") }).Tables[0];
            Temp = Convert.ToDecimal(dt.Rows[0]["AvgTemp"]);
            Humid = Convert.ToDecimal(dt.Rows[0]["AvgHumid"]);
            msg = string.Format(dt.Rows[0]["Msg"].ToString(), DateTime.Now.ToString("dd-MM-yyyy"), Temp, Humid);
        }

    }
}
