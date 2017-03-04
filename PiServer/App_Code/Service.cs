using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]

public class Service : System.Web.Services.WebService
{
    public Service ()
    {
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public string GetSensorStatus()
    {
        SqlCommand cmd = new SqlCommand();
        SqlConnection cn = new SqlConnection();
        cn.ConnectionString = "Server = OFFICEPC\\MYSERVER; Database = RaspberryPiStuff; Trusted_Connection = true";
        cmd.Connection = cn;
        cmd.CommandText = "SELECT * FROM Status";
        SqlDataAdapter adap = new SqlDataAdapter();
        DataTable dt = new DataTable("DataTable");
        cn.Open();
        adap.SelectCommand = cmd;
        adap.Fill(dt);
        cn.Close();
        string jsonResult = JsonConvert.SerializeObject(dt);
        return jsonResult;
    }

    [WebMethod]
    public bool CheckForPhone()
    {
        Ping pingSender = new Ping();

        PingReply reply = pingSender.Send("192.168.1.128", 1000);

        if (reply.Status == IPStatus.Success)
            return true;
        else
            return false;
    }

    [WebMethod]
    public bool UpdateStatus(string sensor, string status)
    {
        SqlCommand cmd = new SqlCommand();
        SqlConnection cn = new SqlConnection();
        cn.ConnectionString = "Server = OFFICEPC\\MYSERVER; Database = RaspberryPiStuff; Trusted_Connection = true";
        cmd.Connection = cn;
        cmd.CommandText = string.Format("UPDATE Status SET Status = '{1}' WHERE Sensor = '{0}'", sensor, status);
        cn.Open();
        int rowsAffected = cmd.ExecuteNonQuery();
        cn.Close();
        if (rowsAffected > 0)
            return true;
        else
            return false;
    }
}