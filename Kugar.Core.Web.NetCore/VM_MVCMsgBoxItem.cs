namespace Kugar.Core.Web
{
    public class VM_MVCMsgBoxItem
    {
        public VM_MVCMsgBoxItem(string msg, string js)
        {
            Message = msg;
            JavsScript = js;
        }

        public string Message { set; get; }

        public string JavsScript { set; get; }
    }
}
