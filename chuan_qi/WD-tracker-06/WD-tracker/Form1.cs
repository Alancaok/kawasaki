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

namespace WD_tracker
{

    public partial class Form1 : Form
    {
		public static String cmdOpenLaser ;
		public static String cmdCloseLaser ;
		public static String cmdStartTracker;
		public static String cmdStopTracker;
		public static String cmdSetMode;

		public static String cmdGetData ;
		public static String cmdGetStatus;
		public static String cmdGetMode;

		private byte[] sendMsgBuf = new byte[1024];
        //Robot Server
        bool serverStarted = false;
		bool showRobotMsgHEX = true;
        bool showLog = true;
		private byte[] robotBuf = new byte[1024];
		private TcpListener robotListener;
		private TcpClient robotClient;
        NetworkStream robotStream;

        //Tracker Client
        bool clientStarted = false;
		bool laserOn = false;
		int trackerMode = 0;
        private byte[] trackerBuf = new byte[1024];
        private TcpClient trackerClient;
        NetworkStream trackerStream;
        int preX = 0;
        int preZ = 0;

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

        private String toHexString(String msg)
        {
            if (null == msg) return "";
            byte[] bufHEX = Encoding.Default.GetBytes(msg);
            String msgHEX = "";
            foreach (byte b in bufHEX)
            {
                String value = Convert.ToString(b, 16);
                msgHEX += (value.Length == 2 ? value : "0" + value) + " ";
            }
            return msgHEX;
        }
        private void showHEXMessage(String msg)
		{
			try
			{
				if (null == msg) return;
				infoText.AppendText(System.DateTime.Now.ToLongTimeString() + ":  " + toHexString(msg));
				infoText.AppendText("\r\n");
				infoText.ScrollToCaret();
			}
			catch (Exception)
			{ }
		}

		private int sendMsg(NetworkStream stream, byte[] buf, int len)
        {
            if((null == stream) || (null == buf))
            {
                return - 1;
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


		private void parserRobotMsg(String msg)
		{
			if (null == msg) return;
			
			try
			{
				if (msg.StartsWith(cmdOpenLaser))
				{
					trackerOpenLaser();
					sendMsg(robotStream, new byte[] { 0x82 }, 1);
				}
				else if (msg.StartsWith(cmdCloseLaser))
				{
					trackerCLoseLaser();
					sendMsg(robotStream, new byte[] { 0x82 }, 1);
				}
				else if (msg.StartsWith(cmdStartTracker))
				{
					trackerOpenLaser();
					sendMsg(robotStream, new byte[] { 0x82 }, 1);
				}
				else if (msg.StartsWith(cmdStopTracker))
				{
					trackerCLoseLaser();
					sendMsg(robotStream, new byte[] { 0x82 }, 1);
				}
				else if (msg.StartsWith(cmdSetMode))
				{
					byte[] modeArray = Encoding.ASCII.GetBytes(msg.Substring(3, 1));
					int mode = modeArray[0];
					trackerSetMode(mode);
					sendMsg(robotStream, new byte[] { 0x82 }, 1);
				}
				else if (msg.StartsWith(cmdGetData))
				{
					trackerGetStatus();
				}
				else if (msg.StartsWith(cmdGetStatus))
				{
					if (laserOn)
					{
						sendMsg(robotStream, new byte[] { 0x82, 0x00, 0x18, 0x00 }, 4);
					}
					else
					{
						sendMsg(robotStream, new byte[] { 0x82, 0x00, 0x08, 0x40 }, 4);
					}
				}
				else if (msg.StartsWith(cmdGetMode))
				{
					sendMsg(robotStream, new byte[] { 0x82, 0x00, 0x00, (byte)trackerMode }, 4);
				}
			}catch(Exception e)
			{
                showErrorMessage("Paser Robot Message fail:" + e.ToString());
			}
		}
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
            showMessage("等待robot连接..." + ip + ":" +myPort);
			try
			{
				robotClient = robotListener.AcceptTcpClient();//等待客户端连接
			}catch(Exception e)
			{
                showErrorMessage("robot连接异常:" + e.ToString());
				return;
			}
            showMessage("robot连接成功");

            try
            {
                robotStream = robotClient.GetStream();
            }
            catch(Exception e)
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

						if (showRobotMsgHEX)
						{
							showHEXMessage("Robot:"+tmpStr);
						}
						parserRobotMsg(tmpStr);						
					}catch(Exception e)
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

	private void scanTrackerClient()
        {
            int myPort = 5020;
            if ("" != textBox1.Text)
            {
                myPort = int.Parse(textBox1.Text);
            }
            
            try
            {
                showMessage("连接tracker...." + textBoxTrakerIP.Text + ":" + myPort );
                trackerClient = new TcpClient(textBoxTrakerIP.Text, myPort);
                trackerStream = trackerClient.GetStream();
                showMessage("tracker连接成功！");
            }
            catch(Exception e)
            {
                showErrorMessage("tracker接收失败:" + e.ToString());
            }

			int receivedBufLength = 0;
			while ((null != trackerClient) 
				&& (trackerClient.Connected))
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
				}
				else 
				{
                    showErrorMessage("tracker无法读取错误！");
					return;
				}
			}
            showErrorMessage("tracker client关闭！");
		}

        private void parserTrackerInfo(byte[] buf, int len)
		{
			//if ((0xFF == buf[0]) && (0xFE == buf[1])) //head is 0xff 0xfe
			{
				string strBuf = Encoding.Default.GetString(buf);
				//if (strBuf == null) return;
				showMessage("GVC return:" + toHexString(strBuf.Substring(0,len)));
				int idx = -1;
				int x = 0;
				int z = 0;
				try
				{
					idx = strBuf.IndexOf("V00A>");
					if (idx > 0)
					{
						string xStr = strBuf.Substring(idx + 5, 7);
						x = (int)(100 * float.Parse(xStr));
						if (x < -32768) x = -32768;
                        preX = x;
					}
					else
					{
                        showErrorMessage("GVC return:无法获得X数据，use preX=" + preX);
                        //x = preX;
                        return;
					}
					idx = strBuf.IndexOf("V01A>");
					if (idx > 0)
					{
						string yStr = strBuf.Substring(idx + 5, 7);
						z = (int)(100 * float.Parse(yStr));
						if (z < -32768) z = -32768;
                        preZ = z;
					}
					else
					{
                        showErrorMessage("GVC return:无法获得Z数据, use preZ=" + preZ);
                        //z = preZ;
                        return;
					}
					byte[] temp = new byte[16];
					temp[0] = 0x82;
					temp[1] = 0x00;
					temp[2] = (byte)((x >> 8) & 0xFF); //XH
					temp[3] = (byte)(x & 0xFF); //XL
					temp[4] = 0; //Y
					temp[5] = 0;  //Y
					temp[6] = (byte)((z >> 8) & 0xFF);  //ZH
					temp[7] = (byte)(z & 0xFF);  //ZL
					temp[8] = 0;
					temp[9] = 0;
					temp[10] = 0;
					temp[11] = 0;
					temp[12] = 0;
					temp[13] = 0;
					sendMsg(robotStream, temp, 14);
				}catch(Exception e)
				{
                    showErrorMessage("tracker parser fail:" + e.ToString());
				}
			}
		}

        private void startTrackerClient()
        {
            new Thread(scanTrackerClient).Start();
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

		public Form1()
		{
			InitializeComponent();
			Control.CheckForIllegalCrossThreadCalls = false;
		}

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 2;

			cmdOpenLaser    = Encoding.Default.GetString(new byte[]{ 0x02, 0x01, 0x13, 0x01});
			cmdCloseLaser   = Encoding.Default.GetString(new byte[]{ 0x02, 0x01, 0x06, 0x00});
			cmdStartTracker = Encoding.Default.GetString(new byte[]{ 0x02, 0x01, 0x06, 0x01});
			cmdStopTracker  = Encoding.Default.GetString(new byte[]{ 0x02, 0x01, 0x06, 0x00});
			cmdSetMode      = Encoding.Default.GetString(new byte[]{ 0x02, 0x01, 0x10 });

			cmdGetData      = Encoding.Default.GetString(new byte[]{ 0x01, 0x06, 0x08, 0x09, 0x0A });
			cmdGetStatus    = Encoding.Default.GetString(new byte[]{ 0x01, 0x01, 0x06 });
			cmdGetMode      = Encoding.Default.GetString(new byte[]{ 0x01, 0x01, 0x10 });
		}

        private void button6_Click(object sender, EventArgs e)
        {
			if (!serverStarted)
			{
				serverStarted = true;
				startRobotServer();
				button6.Text = "停止监听";
			}
			else
			{
				serverStarted = false;
				stopRobotServer();
				button6.Text = "开始监听";
			}
           


        }

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			stopRobotServer();
			stopTrackerClient();
		}

		private void Form1_Deactivate(object sender, EventArgs e)
		{
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!clientStarted)
            {
                clientStarted = true;
                startTrackerClient();
                button1.Text = "断开";
            }
            else
            {
                clientStarted = false;
                stopTrackerClient();
                button1.Text = "连接";
            }
            string s = "304143 ";
            byte[] buff = new byte[s.Length / 2];
            int index = 0;
            for (int i = 0; i < s.Length; i += 2)
            {
                buff[index] = Convert.ToByte(s.Substring(i, 2), 16);
                ++index;
            }
            string result = Encoding.Default.GetString(buff);
            Console.Write(result);

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
			laserOn = true;
			showMessage("打开激光");
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
			laserOn = false;
			showMessage("关闭激光");
		}
		
		private void trackerGetStatus()
		{
			trackerBuf[0] = (byte)'G';
			trackerBuf[1] = (byte)'V';
			trackerBuf[2] = (byte)'C';
			trackerBuf[3] = 0x0d;
			sendMsg(trackerStream, trackerBuf, 4);
			showMessage("获取跟踪参数");
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
			sendMsg(trackerStream, trackerBuf, 5);
			showMessage("设置跟踪模式：" + mode);
            comboBox1.SelectedIndex = mode;
        }

		private void button3_Click(object sender, EventArgs e)
        {
			//open laser
			trackerOpenLaser();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			//close lasesr
			trackerCLoseLaser();
			
		}

		private void button5_Click(object sender, EventArgs e)
		{
			//get tracker status
			trackerGetStatus();			
		}

		private void button2_Click(object sender, EventArgs e)
		{
			//set traker mode
			trackerSetMode(comboBox1.SelectedIndex);
		}

		private void robotMsgHexChkBox_CheckedChanged(object sender, EventArgs e)
		{
			showRobotMsgHEX = robotMsgHexChkBox.Checked;
		}

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void showLogCheckBox_CheckedChanged(object sender, EventArgs e)
        {
           showLog = showLogCheckBox.Checked;
        }
    }
}
