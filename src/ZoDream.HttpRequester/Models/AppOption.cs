using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.HttpRequester.Models
{
    public class AppOption
    {

        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public List<string> Histories { get; set; } = new();
    }
}
