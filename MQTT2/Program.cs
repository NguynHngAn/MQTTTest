using MQTTnet;
using MQTTnet.Client;
using System.Text;
using Timer = System.Timers.Timer;

/*
         var options = new MqttClientOptionsBuilder()
            .WithClientId("clientId")
            .WithTcpServer("113.161.84.132", 8883)
            .WithCredentials("iot", "iot@123456")
            .WithCleanSession()
            .Build();

            .WithTcpServer("broker.hivemq.com", 1883)

            .WithTcpServer("test.mosquitto.org", 1883)

*/
class Program
{
    private static IMqttClient mqttClient;
    private static Timer publishTimer;

    static async Task Main(string[] args)
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("clientId")
            .WithTcpServer("113.161.84.132", 8883)
            .WithCredentials("iot", "iot@123456")
            .WithCleanSession()
            .Build();

        // Handle the connected event
        mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("### CONNECTED WITH SERVER ###");
            // Subscribe to a topic
            await mqttClient.SubscribeAsync("ancute");
            Console.WriteLine("### SUBSCRIBED ###");
            // Start the timer for publishing messages every 100ms
            StartPublishing();
        };

        // Handle incoming application messages
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            var receivedMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine(receivedMessage);

            // Kiểm tra nếu tin nhắn là "STOP"
            if (receivedMessage.ToLower() == "stop")
            {
                Console.WriteLine("### STOPPING PUBLISHING ###");
                StopPublishing();
            }
            // Kiểm tra nếu tin nhắn là "START"
            else if (receivedMessage.ToLower() == "start")
            {
                Console.WriteLine("### STARTING PUBLISHING ###");
                StartPublishing();
            }

            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(options);

        // Vòng lặp để gửi tin nhắn hoặc nhận lệnh từ console
        while (true)
        {
            Console.WriteLine("Enter a message to send, 'start' to resume, 'stop' to pause, or 'exit' to quit:");
            var userInput = Console.ReadLine();

            if (userInput.ToLower() == "exit")
            {
                break;
            }
            else if (userInput.ToLower() == "stop")
            {
                StopPublishing();
            }
            else if (userInput.ToLower() == "start")
            {
                StartPublishing();
            }
            else
            {
                await PublishMessage(userInput);
            }
        }

        StopPublishing();
        await mqttClient.DisconnectAsync();
    }

    private static void StartPublishing()
    {
        if (publishTimer == null)
        {
            publishTimer = new Timer(500); // 500ms interval
            publishTimer.Elapsed += async (sender, e) =>
            {
                string dynamicMessage = $"Dynamic message at {DateTime.Now}";
                await PublishMessage(dynamicMessage);
            };
        }
        publishTimer.Start();
        Console.WriteLine("Publishing started.");
    }

    private static async Task PublishMessage(string messageContent)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("ancute")
            .WithPayload(messageContent)
            .WithRetainFlag(false)
            .Build();

        if (mqttClient.IsConnected)
        {
            await mqttClient.PublishAsync(message);
            Console.WriteLine($"Message published: {messageContent}");
        }
    }

    private static void StopPublishing()
    {
        publishTimer?.Stop();
        Console.WriteLine("Publishing stopped.");
    }
}
