using System;
using System.Collections.Generic;
using System.Text;

namespace MessagingService
{
    public class HitCountMessage
    {
        public int hit { get; set; }
        public string hitTime { get; set; }
        public string corellationId { get; set; }
    }
}
