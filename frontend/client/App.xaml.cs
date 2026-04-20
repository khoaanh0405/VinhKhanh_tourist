using client;
using client.lib.services;

namespace client
{
    public partial class App : Application
    {
        private readonly TrackingService _trackingService;

        // Tiêm TrackingService vào App
        public App(TrackingService trackingService)
        {
            InitializeComponent();
            _trackingService = trackingService;

            MainPage = new AppShell(); // Hoặc trang chủ của bạn
        }

        // 1. Khi App vừa được mở lên
        protected override void OnStart()
        {
            base.OnStart();
            _trackingService.StartHeartbeat(); // Bắt đầu đếm Active User
        }

        // 2. Khi khách ấn phím Home thoát ra ngoài (App chạy ngầm)
        protected override void OnSleep()
        {
            base.OnSleep();
            _trackingService.StopHeartbeat(); // Tắt gửi tín hiệu để tiết kiệm Pin/Data
        }

        // 3. Khi khách quay lại App
        protected override void OnResume()
        {
            base.OnResume();
            _trackingService.StartHeartbeat(); // Tiếp tục đếm Active User
        }
    }
}