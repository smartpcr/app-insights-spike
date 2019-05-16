using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncLocalSpike
{
    public class DocDbSettings
    {
        public string Account { get; set; }
        public string Db { get; set; }
        public string Collection { get; set; }
        public string AuthKeySecret { get; set; }
    }
}
