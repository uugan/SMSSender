using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMSSender
{
    public class SmsDetail
    {
        public string datetime { get; set; }
        public string phoneno { get; set; }
        public string sms { get; set; }
        public string direct { get; set; }
        public SmsDetail(string dt, string phone, string smsText, string direction)
        {
            datetime = dt;
            phoneno = phone;
            sms = smsText;
            direct = direction;
        }

        public override string ToString()
        {
            return datetime + "\t"+direct +" "+ phoneno + ":" + sms;

        }


    }
}
