using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using test;

namespace chuan_qi
{
    public partial class Form1 : Form
    {

        //Robot Server
        bool serverStarted = false;
        private TcpListener robotListener;
        bool showLog = true;
        private TcpClient robotClient;
        NetworkStream robotStream;
        private byte[] robotBuf = new byte[1024];
        string x_finlly;
        string z_finlly;
        string gap_finlly;
        //Tracker Client
        private TcpClient trackerClient;
        NetworkStream trackerStream;
        private byte[] trackerBuf = new byte[1024];
        //
        private byte[] toRobot = new byte[1024];
        //
        int laserstatus = 0;
        int pokou = 0;
        int laserOn = 0;
        double preX = 0;
        double preZ = 0;
        double pgap = 0;
        int trackerMode = 0;
        private String toHexString(String msg)
        {
            if (null == msg) return "";
            byte[] bufHEX = Encoding.Default.GetBytes(msg);

            String msgHEX = "";
            foreach (byte b in bufHEX)
            {
                
                String value = Convert.ToString(b, 16);
               
                if (!Convert.ToBoolean(value.CompareTo("0")))
                {
                    value = "30";
                    
                }
                msgHEX += (value.Length == 2 ? value : "0" + value)+ " " ;
                
            }
            return msgHEX;
        }
        private int sendMsg(NetworkStream stream, byte[] buf, int len)
        {
            if ((null == stream) || (null == buf))
            {
                return -1;
            }

            try
            {
                stream.Write(buf, 0, len);
            }
            catch (Exception e)
            {
                showErrorMessage(stream.ToString() + "send error:" + e.ToString());
                return -2;
            }
            return 0;
        }
        //显示信息
        private void showMessage(String msg)
        {
            if (showLog)
            {
                try
                {
                    infoText.AppendText(System.DateTime.Now.ToLongTimeString() + "|  " + msg);
                    infoText.AppendText("\r\n");
                    infoText.ScrollToCaret();
                }
                catch (Exception)
                { }
            }
        }
        //显示错误信息
        private void showErrorMessage(String msg)
        {
            try
            {
                infoText.AppendText(System.DateTime.Now.ToLongTimeString() + "|  " + msg);
                infoText.AppendText("\r\n");
                infoText.ScrollToCaret();
            }
            catch (Exception)
            { }
        }
        //
        private void startTrackerClient()
        {
            new Thread(scanTrackerClient).Start();
        }
        private void scanTrackerClient()
        {
            int myPort = 5020;
            if ("" != textBox1.Text)
            {
                myPort = int.Parse(textBox1.Text);
            }

            try
            {
                showMessage("连接tracker...." + textBoxRobotServerIP.Text + ":" + myPort);
                trackerClient = new TcpClient(textBoxRobotServerIP.Text, myPort);
                trackerStream = trackerClient.GetStream();
                showMessage("tracker连接成功！");
                laserstatus = 1;
                laserOn = 1;
            }
            catch (Exception e)
            {
                showErrorMessage("tracker接收失败:" + e.ToString());
            }

            //int receivedBufLength = 0;
            //while ((null != trackerClient)&& (trackerClient.Connected))
            //{
            //    try
            //    {
            //        Array.Clear(trackerBuf, 0, trackerBuf.Length);
            //        receivedBufLength = trackerStream.Read(trackerBuf, 0, trackerBuf.Length);
            //    }
            //    catch (Exception e)
            //    {
            //        showErrorMessage("tracker读取错误！" + e.ToString());
            //        return;
            //    }
            //    if (receivedBufLength > 0)
            //    {
            //        showMessage(Encoding.Default.GetString(trackerBuf).Substring(0, receivedBufLength));
            //        parserTrackerInfo(trackerBuf, receivedBufLength);
            //    }
            //    else
            //    {
            //        showErrorMessage("tracker无法读取错误！");
            //        return;
            //    }
            //}
            //showErrorMessage("tracker client关闭！");
        }
        
        //如果收到机器人指令，发给track
        private void parserRobotMsg(String msg)
        {
            if (null == msg) return;
            // string xmlString = "<?xml version=\"1.0\" encoding=\"UTF - 8\"?><snd ts=\"xxxx\" rts=\"xxxx\"><lon n=\"123\"/></snd>";
            XmlOperator xmlOperator = new XmlOperator(msg);
            string[] command = xmlOperator.GetFirstChildNodeByTagName("snd");
            //for (int i = 0; i < command.Length; i++)
            //{
            //    if (command[i] != null)
            //        Console.WriteLine("{0}={1}", i, command[i]);
            //}
           
            try
            {
                if (command[0] == "elsr")
                {             
                    startTrackerClient();
                    label10.Text = "断开";
                    laserstatus = 0;
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;  
                   // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'e';
                    toRobot[23] = (byte)'l';
                    toRobot[24] = (byte)'s';
                    toRobot[25] = (byte)'r';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';           
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                    // Console.WriteLine(ts);
                }
                else if(command[0] == "lon")
                {
                    trackerOpenLaser();
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'l';
                    toRobot[23] = (byte)'o';
                    toRobot[24] = (byte)'n';               
                    toRobot[25] = (byte)' ';
                    toRobot[26] = (byte)'r';
                    toRobot[27] = (byte)'=';
                    toRobot[28] = (byte)'"';
                    toRobot[29] = (byte)'1';
                    toRobot[30] = (byte)'"';
                    toRobot[31] = (byte)'/';
                    toRobot[32] = (byte)'>';
                    toRobot[33] = (byte)'<';
                    toRobot[34] = (byte)'/';
                    toRobot[35] = (byte)'r';
                    toRobot[36] = (byte)'e';
                    toRobot[37] = (byte)'p';
                    toRobot[38] = (byte)'>';
                    sendMsg(robotStream, toRobot, 39);

                }
                else if(command[0] == "selj")
                {
                    trackerSetMode(Convert.ToInt32(command[1]));
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'s';
                    toRobot[23] = (byte)'e';
                    toRobot[24] = (byte)'l';
                    toRobot[25] = (byte)'j';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                }
                else if (command[0] == "btrk")
                {

                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'b';
                    toRobot[23] = (byte)'t';
                    toRobot[24] = (byte)'r';
                    toRobot[25] = (byte)'k';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                }
                else if (command[0] == "gcp")
                {
                    Console.WriteLine("gcp");
                    trackerGetStatus();
                    Console.WriteLine("444444444");
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    
                    char[] str_x = x_finlly.ToCharArray();//推荐
                    char[] str_z = z_finlly.ToCharArray();//推荐
                    char[] str_gap = gap_finlly.ToCharArray();//推荐
                    Console.WriteLine("5555555");
                    toRobot[0] = (byte)'<';
                     toRobot[1] = (byte)'r';
                     toRobot[2] = (byte)'e';
                     toRobot[3] = (byte)'p';
                     toRobot[4] = (byte)' ';
                     toRobot[5] = (byte)'t';
                     toRobot[6] = (byte)'s';
                     toRobot[7] = (byte)'=';
                     toRobot[8] = (byte)'"';
                     toRobot[9] = (byte)st[0];
                     toRobot[10] = (byte)st[1];
                     toRobot[11] = (byte)st[2];
                     toRobot[12] = (byte)st[3];
                     toRobot[13] = (byte)st[4];
                     toRobot[14] = (byte)st[5];
                     toRobot[15] = (byte)st[6];
                     toRobot[16] = (byte)st[7];
                     toRobot[17] = (byte)st[8];
                     toRobot[18] = (byte)st[9];
                     toRobot[19] = (byte)'"';
                     toRobot[20] = (byte)'>';
                     toRobot[21] = (byte)'<';
                     toRobot[22] = (byte)'g';
                     toRobot[23] = (byte)'c';
                     toRobot[24] = (byte)'p';

                     toRobot[25] = (byte)' ';
                     toRobot[26] = (byte)'r';
                     toRobot[27] = (byte)'=';
                     toRobot[28] = (byte)'"';
                     toRobot[29] = (byte)'1';
                     toRobot[30] = (byte)'"';
                     toRobot[31] = (byte)'/';
                     toRobot[32] = (byte)'>';

                     toRobot[33] = (byte)'<';
                     toRobot[34] = (byte)'t';
                     toRobot[35] = (byte)'p';
                     toRobot[36] = (byte)' ';

                     toRobot[37] = (byte)'x';
                     toRobot[38] = (byte)'=';
                     toRobot[39] = (byte)'"';
                     toRobot[40] = (byte)str_x[0];
                     toRobot[41] = (byte)str_x[1];
                     toRobot[42] = (byte)str_x[2];
                     toRobot[43] = (byte)str_x[3];
                     toRobot[44] = (byte)str_x[4];
                     toRobot[45] = (byte)str_x[5];
                     toRobot[46] = (byte)'"';
                     toRobot[47] = (byte)' ';

                     toRobot[48] = (byte)'y';
                     toRobot[49] = (byte)'=';
                     toRobot[50] = (byte)'"';
                     toRobot[51] = (byte)'0';
                     toRobot[52] = (byte)'0';
                     toRobot[53] = (byte)'0';
                     toRobot[54] = (byte)'.';
                     toRobot[55] = (byte)'0';
                     toRobot[56] = (byte)'0';
                     toRobot[57] = (byte)'"';
                     toRobot[58] = (byte)' ';

                     toRobot[59] = (byte)'z';
                     toRobot[60] = (byte)'=';
                     toRobot[61] = (byte)'"';
                     toRobot[62] = (byte)str_z[0];
                     toRobot[63] = (byte)str_z[1];
                     toRobot[64] = (byte)str_z[2];
                     toRobot[65] = (byte)str_z[3];
                     toRobot[66] = (byte)str_z[4];
                     toRobot[67] = (byte)str_z[5];
                     toRobot[68] = (byte)'"';
                     toRobot[69] = (byte)' ';

                     toRobot[70] = (byte)'a';
                     toRobot[71] = (byte)'=';
                     toRobot[72] = (byte)'"';
                     toRobot[73] = (byte)'0';
                     toRobot[74] = (byte)'0';
                     toRobot[75] = (byte)'0';
                     toRobot[76] = (byte)'.';
                     toRobot[77] = (byte)'0';
                     toRobot[78] = (byte)'0';
                     toRobot[79] = (byte)'"';
                     toRobot[80] = (byte)' ';

                     toRobot[81] = (byte)'b';
                     toRobot[82] = (byte)'=';
                     toRobot[83] = (byte)'"';
                     toRobot[84] = (byte)'0';
                     toRobot[85] = (byte)'0';
                     toRobot[86] = (byte)'0';
                     toRobot[87] = (byte)'.';
                     toRobot[88] = (byte)'0';
                     toRobot[89] = (byte)'0';
                     toRobot[90] = (byte)'"';
                     toRobot[91] = (byte)' ';

                     toRobot[92] = (byte)'c';
                     toRobot[93] = (byte)'=';
                     toRobot[94] = (byte)'"';
                     toRobot[95] = (byte)'0';
                     toRobot[96] = (byte)'0';
                     toRobot[97] = (byte)'0';
                     toRobot[98] = (byte)'.';
                     toRobot[99] = (byte)'0';
                     toRobot[100] = (byte)'0';
                     toRobot[101] = (byte)'"';
                     toRobot[102] = (byte)' ';

                     toRobot[103] = (byte)'/';
                     toRobot[104] = (byte)'>';  
                     toRobot[105] = (byte)'<';
                     toRobot[106] = (byte)'g';
                     toRobot[107] = (byte)'p';
                     toRobot[108] = (byte)' ';
                     toRobot[109] = (byte)'a';
                     toRobot[110] = (byte)'r';
                     toRobot[111] = (byte)'e';
                     toRobot[112] = (byte)'a';
                     toRobot[113] = (byte)'=';
                     toRobot[114] = (byte)'"';
                     toRobot[115] = (byte)'0';
                     toRobot[116] = (byte)'0';
                     toRobot[117] = (byte)'0';
                     toRobot[118] = (byte)'.';
                     toRobot[119] = (byte)'0';
                     toRobot[120] = (byte)'0';
                     toRobot[121] = (byte)'"';
                     toRobot[122] = (byte)' ';

                     toRobot[123] = (byte)'g';
                     toRobot[124] = (byte)'a';
                     toRobot[125] = (byte)'p';
                     toRobot[126] = (byte)'=';
                     toRobot[127] = (byte)'"';
                     toRobot[128] = (byte)str_gap[0];
                     toRobot[129] = (byte)str_gap[1];
                     toRobot[130] = (byte)str_gap[2];
                     toRobot[131] = (byte)str_gap[3];
                     toRobot[132] = (byte)str_gap[4];
                     toRobot[133] = (byte)str_gap[5];
                     toRobot[134] = (byte)'"';
                     toRobot[135] = (byte)' ';

                     toRobot[136] = (byte)'m';
                     toRobot[137] = (byte)'i';
                     toRobot[138] = (byte)'s';
                     toRobot[139] = (byte)'m';
                     toRobot[140] = (byte)'=';
                     toRobot[141] = (byte)'"';
                     toRobot[142] = (byte)'0';
                     toRobot[143] = (byte)'0';
                     toRobot[144] = (byte)'0';
                     toRobot[145] = (byte)'.';
                     toRobot[146] = (byte)'0';
                     toRobot[147] = (byte)'0';
                     toRobot[148] = (byte)'"';
                     toRobot[149] = (byte)'/';
                     toRobot[150] = (byte)'>';



                     toRobot[151] = (byte)'<';
                     toRobot[152] = (byte)'/';
                     toRobot[153] = (byte)'g';
                     toRobot[154] = (byte)'c';
                     toRobot[155] = (byte)'p';
                     toRobot[156] = (byte)'>';

                     toRobot[157] = (byte)'<';
                     toRobot[158] = (byte)'/';
                     toRobot[159] = (byte)'r';
                     toRobot[160] = (byte)'e';
                     toRobot[161] = (byte)'p';
                     toRobot[162] = (byte)'>';
                     
                    sendMsg(robotStream, toRobot, 163);
                    Console.WriteLine("66666666666");


                }
                else if (command[0] == "etrk")
                {
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'e';
                    toRobot[23] = (byte)'t';
                    toRobot[24] = (byte)'r';
                    toRobot[25] = (byte)'k';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                }
                else if (command[0] == "loff")
                {
                    trackerCLoseLaser();
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'l';
                    toRobot[23] = (byte)'o';
                    toRobot[24] = (byte)'f';
                    toRobot[25] = (byte)'f';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                }
                else if (command[0] == "dlsr")
                {
                    stopTrackerClient();
                    label10.Text = "连接";
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'d';
                    toRobot[23] = (byte)'l';
                    toRobot[24] = (byte)'s';
                    toRobot[25] = (byte)'r';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    toRobot[32] = (byte)'/';
                    toRobot[33] = (byte)'>';
                    toRobot[34] = (byte)'<';
                    toRobot[35] = (byte)'/';
                    toRobot[36] = (byte)'r';
                    toRobot[37] = (byte)'e';
                    toRobot[38] = (byte)'p';
                    toRobot[39] = (byte)'>';
                    sendMsg(robotStream, toRobot, 40);
                }
                else if (command[0] == "ere")
                {
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';
                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'e';
                    toRobot[23] = (byte)'r';
                    toRobot[24] = (byte)'e';                 
                    toRobot[25] = (byte)' ';
                    toRobot[26] = (byte)'r';
                    toRobot[27] = (byte)'=';
                    toRobot[28] = (byte)'"';
                    toRobot[29] = (byte)'1';
                    toRobot[30] = (byte)'"';
                    toRobot[31] = (byte)'/';
                    toRobot[32] = (byte)'>';
                    toRobot[33] = (byte)'<';
                    toRobot[34] = (byte)'/';
                    toRobot[35] = (byte)'r';
                    toRobot[36] = (byte)'e';
                    toRobot[37] = (byte)'p';
                    toRobot[38] = (byte)'>';
                    sendMsg(robotStream, toRobot, 39);
                }
                else if(command[0] == "gets")
                {
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();
                    char[] str_pokou = pokou.ToString().PadLeft(3, '0').ToCharArray();//推荐
                    char[] str_laserOn = (laserOn.ToString()).ToCharArray();
                    char[] str_laserstatus = (laserstatus.ToString()).ToCharArray();
                  

                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';

                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'g';
                    toRobot[23] = (byte)'e';
                    toRobot[24] = (byte)'t';
                    toRobot[25] = (byte)'s';
                    toRobot[26] = (byte)' ';
                    toRobot[27] = (byte)'r';
                    toRobot[28] = (byte)'=';
                    toRobot[29] = (byte)'"';
                    toRobot[30] = (byte)'1';
                    toRobot[31] = (byte)'"';
                    
                    toRobot[32] = (byte)' ';
                    toRobot[33] = (byte)'s';
                    toRobot[34] = (byte)'t';
                    toRobot[35] = (byte)'a';
                    toRobot[36] = (byte)'t';
                    toRobot[37] = (byte)'u';
                    toRobot[38] = (byte)'s';

                    toRobot[39] = (byte)'=';
                    toRobot[40] = (byte)'"';
                    toRobot[41] = (byte)'1';
                    toRobot[42] = (byte)str_pokou[0];
                    toRobot[43] = (byte)str_pokou[1];
                    toRobot[44] = (byte)str_pokou[2];
                    toRobot[45] = (byte)str_laserOn[0];
                    toRobot[46] = (byte)str_laserstatus[0];
                    toRobot[47] = (byte)'"';
                    toRobot[48] = (byte)'/';
                    toRobot[49] = (byte)'>';
                    toRobot[50] = (byte)'<';
                    toRobot[51] = (byte)'/';
                    toRobot[52] = (byte)'r';
                    toRobot[53] = (byte)'e';
                    toRobot[54] = (byte)'p';
                    toRobot[55] = (byte)'>';
                    sendMsg(robotStream, toRobot, 56);
                }
                else if (command[0] == "ack")
                {
                    TimeSpan t1 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    double tr = t1.TotalSeconds;
                    // string[] str = new string[10];
                    int ts = (int)tr;
                    string str = ts.ToString();
                    char[] st = str.ToCharArray();

                    toRobot[0] = (byte)'<';
                    toRobot[1] = (byte)'r';
                    toRobot[2] = (byte)'e';
                    toRobot[3] = (byte)'p';
                    toRobot[4] = (byte)' ';
                    toRobot[5] = (byte)'t';
                    toRobot[6] = (byte)'s';
                    toRobot[7] = (byte)'=';
                    toRobot[8] = (byte)'"';
                    toRobot[9] = (byte)st[0];
                    toRobot[10] = (byte)st[1];
                    toRobot[11] = (byte)st[2];
                    toRobot[12] = (byte)st[3];
                    toRobot[13] = (byte)st[4];
                    toRobot[14] = (byte)st[5];
                    toRobot[15] = (byte)st[6];
                    toRobot[16] = (byte)st[7];
                    toRobot[17] = (byte)st[8];
                    toRobot[18] = (byte)st[9];
                    toRobot[19] = (byte)'"';
                    toRobot[20] = (byte)'>';

                    toRobot[21] = (byte)'<';
                    toRobot[22] = (byte)'a';
                    toRobot[23] = (byte)'c';
                    toRobot[24] = (byte)'k';                   
                    toRobot[25] = (byte)' ';
                    toRobot[26] = (byte)'r';
                    toRobot[27] = (byte)'=';
                    toRobot[28] = (byte)'"';
                    toRobot[29] = (byte)'1';
                    toRobot[30] = (byte)'"';
                    toRobot[31] = (byte)'/';
                    toRobot[32] = (byte)'>';
                    toRobot[33] = (byte)'<';
                    toRobot[34] = (byte)'/';
                    toRobot[35] = (byte)'r';
                    toRobot[36] = (byte)'e';
                    toRobot[37] = (byte)'p';
                    toRobot[38] = (byte)'>';
                    sendMsg(robotStream, toRobot, 39);
                }



            }


            catch (Exception e)
            {
                showErrorMessage("Paser Robot Message fail:" + e.ToString());
            }  

        }

        //新建线程
        private void startRobotServer()
        {
            new Thread(robotServer).Start();
        }
        private void robotServer()
        {
            IPAddress ip = IPAddress.Parse(textBoxRobotServerIP.Text);
            int myPort = 5020;
            if ("" != textBoxRobotServerPort.Text)
            {
                myPort = int.Parse(textBoxRobotServerPort.Text);
            }
            robotListener = new TcpListener(ip, myPort);//创建TcpListener实例
            robotListener.Start();//start
            showMessage("等待robot连接..." + ip + ":" + myPort);
            try
            {
                robotClient = robotListener.AcceptTcpClient();//等待客户端连接
            }
            catch (Exception e)
            {
                showErrorMessage("robot连接异常:" + e.ToString());
                return;
            }
            showMessage("robot连接成功");

            try
            {
                robotStream = robotClient.GetStream();
            }
            catch (Exception e)
            {
                showErrorMessage("robot读取失败:" + e.ToString());
            }

            int receivedBufLength = 0;
            string tmpStr = "";
            while ((null != robotClient) && (robotClient.Connected))
            {
                try
                {
                    Array.Clear(robotBuf, 0, robotBuf.Length);
                    receivedBufLength = robotStream.Read(robotBuf, 0, robotBuf.Length);
                }
                catch (Exception e)
                {
                    showErrorMessage("robot读取错误！" + e.ToString());
                    return;
                }
                if (receivedBufLength > 0)
                {
                    try
                    {
                        tmpStr = Encoding.UTF8.GetString(robotBuf).Substring(0, receivedBufLength);
                        parserRobotMsg(tmpStr);
                    }
                    catch (Exception e)
                    {
                        showErrorMessage("Robot send message fail：" + e.ToString());
                    }
                }
                else  //socket closed
                {
                    showErrorMessage("robot连接丢失！");
                    stopRobotServer();
                    return;
                }
            }
            showErrorMessage("robot server关闭！");
        }
        public void stopRobotServer()
        {
            if (null != robotStream)
            {
                try
                {
                    robotStream.Close();
                    robotStream.Dispose();
                }
                catch { }
                robotStream = null;
            }
            if (null != robotClient)
            {
                try
                {
                    robotClient.Client.Disconnect(false);
                    robotClient.Close();
                    robotClient.Dispose();
                }
                catch { }
                robotClient = null;
            }
            if (null != robotListener)
            {
                try
                {
                    robotListener.Stop();
                }
                catch { }
                robotListener = null;
            }
        }
        //初始化窗体
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        private void parserTrackerInfo(byte[] buf, int len)
        {
            //if ((0xFF == buf[0]) && (0xFE == buf[1])) //head is 0xff 0xfe
            {
                string strBuf = Encoding.Default.GetString(buf);
           
                //if (strBuf == null) return;
                showMessage("GVC return:" + toHexString(strBuf.Substring(0,len)));
                string s = toHexString(strBuf.Substring(0, len));
                string result = HexStringToASCII(s);
                showMessage("GVC return:" + result);
                Console.WriteLine(result);
                int idx = -1;
                int idy = -1;
                
                double x = 0;
                double z = 0;
                double gap = 0;
                try
                {

                    idx = result.IndexOf("V00A>");
                    idy = result.IndexOf("V00I>");
                    

                    Console.WriteLine("idx="+idx);
                    Console.WriteLine("idy=" + idy);
                    if (idx > 0|| idy > 0)
                    {
                        Console.WriteLine("111111111111");
                        if (idx < idy)
                        {
                            idx = idy;
                        }
                        string xStr = result.Substring(idx + 5, 7);
                        string x_first = xStr.Substring(0, 1);
                        string x_second = xStr.Substring(2, 5);
                        x_finlly = x_first + x_second;
                        //x = (double)(100 * float.Parse(xStr));
                        //if (x < -32768) x = -32768;
                        //preX = x;
                        //Console.WriteLine(preX);
                    }
                    else
                    {
                        showErrorMessage("GVC return:无法获得X数据，use preX=" + preX);
                        x = preX;
                        return;
                    }
                    idx = result.IndexOf("V01A>");
                    idy = result.IndexOf("V01I>");
                    if (idx > 0 || idy > 0)
                    {
                        Console.WriteLine("3222222222222");
                        if (idx < idy)
                        {
                            idx = idy;
                        }
                        string zStr = result.Substring(idx + 5, 7);
                        string z_first = zStr.Substring(0, 1);
                        string z_second = zStr.Substring(2, 5);
                        z_finlly = z_first + z_second;
                    }
                    else
                    {
                        showErrorMessage("GVC return:无法获得Z数据, use preZ=" + preZ);
                        //z = preZ;
                        return;
                    }
                    idx = result.IndexOf("V05A>");
                    idy = result.IndexOf("V05I>");
                    if (idx > 0 || idy > 0)
                    {
                        if (idx < idy)
                        {
                            idx = idy;
                        }
                        Console.WriteLine("3333333333");
                        string gapStr = result.Substring(idx + 5, 7);
                        string gap_first = gapStr.Substring(0, 1);
                        string gap_second = gapStr.Substring(2, 5);
                        gap_finlly = gap_first + gap_second;
                    }
                    else
                    {
                        showErrorMessage("GVC return:无法获得gap数据, use pgap=" + pgap);
                        return;
                    }

                }
                catch (Exception e)
                {
                    showErrorMessage("tracker parser fail:" + e.ToString());
                }
            }
        }

        private void trackerOpenLaser()
        {
            //open laser
            trackerBuf[0] = (byte)'S';
            trackerBuf[1] = (byte)'O';
            trackerBuf[2] = (byte)'1';
            trackerBuf[3] = (byte)'0';
            trackerBuf[4] = (byte)'+';
            trackerBuf[5] = 0x0d;
            sendMsg(trackerStream, trackerBuf, 6);
            laserOn = 1;
            showMessage("打开激光");
        }
        private void trackerSetMode(int mode)
        {
            byte idx = 0;
            trackerMode = mode;
            trackerBuf[0] = (byte)'S';
            trackerBuf[1] = (byte)'M';
            trackerBuf[2] = (byte)'0';
            switch (mode)
            {
                case 0: idx = (byte)'0'; break;
                case 1: idx = (byte)'2'; break;
                case 2: idx = (byte)'3'; break;
                case 3: idx = (byte)'6'; break;
                case 4: idx = (byte)'7'; break;
                case 5: idx = (byte)'8'; break;
                case 6: idx = (byte)'9'; break;
                default: idx = (byte)'0'; break;
            }
            trackerBuf[3] = idx;
            trackerBuf[4] = 0x0d;
            pokou = mode;
            sendMsg(trackerStream, trackerBuf, 5);
            showMessage("设置跟踪模式：" + mode);
        }
        private void trackerGetStatus()
        {
            trackerBuf[0] = (byte)'G';
            trackerBuf[1] = (byte)'V';
            trackerBuf[2] = (byte)'C';
            trackerBuf[3] = 0x0d;
            sendMsg(trackerStream, trackerBuf, 4);
            showMessage("获取跟踪参数");
            int receivedBufLength = 0;
            while ((null != trackerClient) && (trackerClient.Connected))
            {
                try
                {
                    Array.Clear(trackerBuf, 0, trackerBuf.Length);
                    receivedBufLength = trackerStream.Read(trackerBuf, 0, trackerBuf.Length);
                }
                catch (Exception e)
                {
                    showErrorMessage("tracker读取错误！" + e.ToString());
                    return;
                }
                if (receivedBufLength > 0)
                {
                    
                    showMessage(Encoding.Default.GetString(trackerBuf).Substring(0, receivedBufLength));
                    parserTrackerInfo(trackerBuf, receivedBufLength);
                    Console.WriteLine("getstatus");
                    break;
                }
                else
                {
                    showErrorMessage("tracker无法读取错误！");
                    return;
                }
            }
            Console.WriteLine("end");
            showErrorMessage("tracker client关闭！");
        }
        private void trackerCLoseLaser()
        {
            trackerBuf[0] = (byte)'S';
            trackerBuf[1] = (byte)'O';
            trackerBuf[2] = (byte)'1';
            trackerBuf[3] = (byte)'0';
            trackerBuf[4] = (byte)'-';
            trackerBuf[5] = 0x0d;
            sendMsg(trackerStream, trackerBuf, 6);
            laserOn = 0;
            showMessage("关闭激光");
        }
        private void stopTrackerClient()
        {
            if (null != trackerStream)
            {
                try
                {
                    trackerStream.Close();
                    trackerStream.Dispose();
                }
                catch { }
                trackerStream = null;
            }
            if (null != trackerClient)
            {
                try
                {
                    trackerClient.Client.Disconnect(false);
                    trackerClient.Dispose();
                    trackerClient.Close();
                }
                catch { }
                trackerClient = null;
            }
        }

        //开始监听
        private void button2_Click(object sender, EventArgs e)
        {
            if (!serverStarted)
            {
                serverStarted = true;
                startRobotServer();
                button2.Text = "停止监听";
            }
            else
            {
                serverStarted = false;
                stopRobotServer();
                button2.Text = "开始监听";
            }
        }
        public static string HexStringToASCII(string hexstring)
        {
            byte[] bt = HexStringToBinary(hexstring);
            string lin = "";
            for (int i = 0; i < bt.Length; i++)
            {
                lin = lin + bt[i] + " ";
            }

            string[] ss = lin.Trim().Split(new char[] { ' ' });
            char[] c = new char[ss.Length];
            int a;
            for (int i = 0; i < c.Length; i++)
            {
                a = Convert.ToInt32(ss[i]);
                c[i] = Convert.ToChar(a);
            }
            string b = new string(c);
            return b;
        }

        /**/
        /// <summary>
        /// 16进制字符串转换为二进制数组
        /// </summary>
        /// <param name="hexstring">用空格切割字符串</param>
        /// <returns>返回一个二进制字符串</returns>
        public static byte[] HexStringToBinary(string hexstring)
        {
            string[] tmpary = hexstring.Trim().Split(' ');
            byte[] buff = new byte[tmpary.Length];
            for (int i = 0; i < buff.Length; i++)
            {
                buff[i] = Convert.ToByte(tmpary[i], 16);
            }
            return buff;
        }
      
        
    }
}
