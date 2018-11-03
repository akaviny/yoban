using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using yoban.Mqtt;
using yoban.Mqtt.ControlPacket;

namespace Mqtt.Console
{
    class Program
    {
        public static async Task Main(string[] args)
        {
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
                        TopicFilter = "test/first",
                        RequestedQoS = QoS.AtLeastOnce
                    },
                    new Subscription
                    {
                        TopicFilter = "test/second",
                        RequestedQoS = QoS.AtLeastOnce
                    }
                }
            };
            await mqttClient.SubscribeAsync(subscribePacket);
            System.Console.ReadKey();
        }
    }
}
