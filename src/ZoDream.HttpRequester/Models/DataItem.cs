using ZoDream.Shared.ViewModel;

namespace ZoDream.HttpRequester.Models
{
    public class DataItem: BindableBase
    {
        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public DataItem()
        {

        }

        public DataItem(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
