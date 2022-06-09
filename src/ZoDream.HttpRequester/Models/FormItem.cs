using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.HttpRequester.ViewModels;

namespace ZoDream.HttpRequester.Models
{
    public class FormItem: DataItem
    {

        private int dataType = 0;

        public int DataType
        {
            get => dataType;
            set => Set(ref dataType, value);
        }

        public FormItem()
        {

        }
        public FormItem(string name, string value): base(name, value)
        {
        }

        public FormItem(string name, int dataT, string value) : base(name, value)
        {
            DataType = dataT;
        }
    }
}
