using System.Collections.Generic;

namespace Controller
{
#if WINDOWS_UWP
    public class MainController : IController
    {

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static MainController _instance;
        public static MainController Instance => _instance ?? (_instance = new MainController());

        /// <summary>
        /// Controller for MQTT communication
        /// </summary>
        private MQTTController _mqttController;
        public MQTTController MQTTController
        {
            get { return _mqttController; }
            set { _mqttController = value; }
        }

        /// <summary>
        /// Managed lifecycle elements.
        /// </summary>
        private readonly List<IController> Controllers = new List<IController>();

        public MainController()
        {
            _mqttController = new MQTTController("HoloLensClient", "192.168.102.101", 1883);
            Controllers.Add(_mqttController);
        }


        public void Start()
        {
            Controllers.ForEach((e) => e.Start());
        }

        public void Stop()
        {
            Controllers.ForEach((e) => e.Stop());
        }
    }
#endif
}
