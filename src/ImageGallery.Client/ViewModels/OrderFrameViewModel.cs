namespace ImageGallery.Client.ViewModels
{
    public class OrderFrameViewModel
    {
        public string Address { get; set; } = string.Empty;
        public OrderFrameViewModel(string address)
        {
            Address = address;
        }
    }

}