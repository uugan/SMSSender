using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMSSender
{
    public class SmsEventArg : EventArgs
    {
        public SmsDetail smsInfo { get; set; }

        public SmsEventArg(SmsDetail s)
        {
            smsInfo = s;
        }
    }
}
