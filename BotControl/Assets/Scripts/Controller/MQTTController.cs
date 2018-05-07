using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine;

namespace Controller
{
#if WINDOWS_UWP
    public class MQTTController : IController
    {
        public MqttClient Client { get; private set; }

        public MQTTController(string clientId, string serverAddress, int port)
        {
            try
            {
                Client = new MqttClient(serverAddress);
                //Client = new MqttClient(serverAddress, port, false, MqttSslProtocols.None);
                Client.MqttMsgPublishReceived += MQTTMessageReceived;
                Client.Connect(clientId);
            }
            catch (System.Exception exception)
            {

                Debug.LogException(exception);
            }

            Debug.Log("Connected: " + Client.IsConnected);
        }

        private void MQTTMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.Log("Received message: " + e.Message);
        }

        public void Start()
        {
        }

        /// <summary>
        /// Subscribe to a topic.
        /// </summary>
        /// <param name="topic"></param>
        public void SubscribeTopic(string topic)
        {
            Client.Subscribe(new[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        public void SetVelocity(float lin, float ang)
        {
            Debug.Log("velocity" + lin + "" + ang);

            var jsonString = $"{{\"linear\":  {{\"x\": {lin}, \"y\": 0, \"z\": 0}}, \"angular\": {{\"x\": 0,\"y\": 0,\"z\": {ang}}}}}";
            Debug.Log(jsonString);
            var bytes = Encoding.UTF8.GetBytes(jsonString);
            Client.Publish("velocity", bytes);
        }

        public void Stop()
        {
            Client.Disconnect();
        }
    }
#endif
}