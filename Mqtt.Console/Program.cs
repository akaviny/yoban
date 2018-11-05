using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using yoban.Mqtt;
using yoban.Mqtt.ControlPacket;

namespace Mqtt.Console
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //var networkConnection = new NetworkConnection("iot.eclipse.org", 1883);
            //var mqttClient = new MqttClient(networkConnection);

            var networkConnection = new NetworkConnection("adb7aem1d5wr-ats.iot.eu-west-1.amazonaws.com", 8883);
            var clientCertificate = new X509Certificate2(@"C:\certs\icap\123456789\7d54cd11a2-certificate.pfx", "123456789");
            var mqttClient = new MqttSecureClient(networkConnection, clientCertificate);
            var connectPacket = new Connect
            {
                ClientId = Guid.NewGuid().ToString()
            };
            await mqttClient.ConnectAsync(connectPacket);
            var subscribePacket = new Subscribe
            {
                PacketId = 0x01,
                Subscriptions = new List<Subscription>
                {
                    new Subscription
                    {
                        TopicFilter = "test/wifiTemp",
                        RequestedQoS = QoS.AtLeastOnce
                    },
                    new Subscription
                    {
                        TopicFilter = "test/wifiHumidity",
                        RequestedQoS = QoS.AtLeastOnce
                    }
                }
            };
            await mqttClient.SubscribeAsync(subscribePacket);
            var publishPacket = new Publish
            {
                PacketId = 0x02,
                TopicName = "test/wifiTemp",
                Message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new
                    {
                        id = "someId",
                        body = new
                        {
                            get = 1234.987,
                            set = 1245.1234
                        }
                    }))
            };
            await mqttClient.PublishAsync(publishPacket);
            System.Console.WriteLine("Press a key to disconnect");
            System.Console.ReadKey();
            await mqttClient.DisconnectAsync();
        }
    }
}
