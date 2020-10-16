using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace NActivityTracker.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            LoadApplication(new NActivityTracker.App());
        }
    }
}
