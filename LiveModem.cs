using GsmComm.GsmCommunication;
using GsmComm.PduConverter;
using GsmComm.PduConverter.SmartMessaging;
using GsmComm.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SMSSender
{
    public delegate void SmsReceivedDelegate(object sender, SmsEventArg e);
    public delegate void SmsSentDelegate(object sender, SmsEventArg e);

    public class LiveModem
    {
        private GsmCommMain comm;
        private delegate void SetTextCallback(string text);


        public event SmsReceivedDelegate OnSmsReceived;
        public event SmsSentDelegate OnSmsSent;

        private static string _smsc = "";
        public bool isRunning = false;
        public bool isConnected = false;
        public int threadSleep = 500;
        Dictionary<string, string> smsList;
        public LiveModem(string comport, int baudrate, int timeout, string smsc)
        {
            comm = new GsmCommMain(comport, baudrate, timeout);
            smsList = new Dictionary<string, string>();
            _smsc = smsc;
        }


        public string sendSms(string phoneno, string sms)
        {

            string ret = "001";
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                if (sms.Length <= 160)
                {
                    SmsSubmitPdu pdu;
                    pdu = new SmsSubmitPdu(sms, phoneno);
                    pdu.SmscAddress = _smsc;
                    comm.SendMessage(pdu);
                }
                else
                {
                    OutgoingSmsPdu[] pdus = null;
                    pdus = SmartMessageFactory.CreateConcatTextMessage(sms, phoneno);
                    int num = pdus.Length;

                    try
                    {
                        // Send the created messages
                        int i = 0;
                        foreach (OutgoingSmsPdu pdu in pdus)
                        {
                            i++;
                            comm.SendMessage(pdu);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        // ShowException(ex);
                    }
                }

                Cursor.Current = Cursors.Default;
                SmsDetail sd = new SmsDetail(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), phoneno, sms, ">>");
                SmsSentTriggerEvent(new SmsEventArg(sd));
                ret = "000";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return ret;
        }
        void SmsReceivedTriggerEvent(SmsEventArg e)
        {
            if (OnSmsReceived == null)
                return;
            OnSmsReceived(this, e);
        }

        void SmsSentTriggerEvent(SmsEventArg e)
        {
            if (OnSmsSent == null) return;
            OnSmsSent(this, e);
        }

        public string connect()
        {
            string ret = "001";
            try
            {
                comm.Open();
                ret = "000";
                Console.WriteLine("Modem connected successfully");
                isConnected = true;
            }
            catch (Exception ex)
            {
                ret = "002, " + ex.Message;
            }
            return ret;
        }
        public string disconnect()
        {
            string ret = "000";
            try
            {
                isRunning = false;
                isConnected = false;
                if (comm.IsConnected() || comm.IsOpen())
                    comm.Close();
            }
            catch (Exception ex)
            {
                ret = "001, " + ex.Message;
            }
            return ret;
        }
        public void startReceive()
        {
            isRunning = true;
            while (isRunning)
            {
                try
                {
                    DecodedShortMessage[] messages = comm.ReadMessages(PhoneMessageStatus.All, PhoneStorageType.Sim);

                    foreach (DecodedShortMessage message in messages)
                    {
                        SmsPdu rawmsg = message.Data;

                        SmsDeliverPdu msg = (SmsDeliverPdu)rawmsg;

                        string m = msg.UserDataText;
                        string s = msg.OriginatingAddress;
                        Console.WriteLine(msg.GetTimestamp().ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") + ":" + s + ":" + m);

                        if (!smsList.ContainsKey(msg.GetTimestamp().ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") + ":" + s))
                        {
                            SmsDetail sd = new SmsDetail(msg.GetTimestamp().ToDateTime().ToString("yyyy-MM-dd HH:mm:ss"), s, m, "<<");
                            SmsReceivedTriggerEvent(new SmsEventArg(sd));
                            smsList.Add(msg.GetTimestamp().ToDateTime().ToString("yyyy-MM-dd HH:mm:ss") + ":" + s, m);
                        }
                    }
                }
                catch { }

                Thread.Sleep(threadSleep);
            }
        }
        public void stopReceive()
        {
            isRunning = false;
        }

        public string clearsms()
        {
            string ret = "001";
            try
            {
                comm.DeleteMessage(0, PhoneStorageType.Sim);
                comm.DeleteMessages(DeleteScope.All, PhoneStorageType.Sim);
                ret = "000";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return ret;
        }



    }
}
