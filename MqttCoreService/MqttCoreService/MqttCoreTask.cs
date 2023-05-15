using System;
using System.Collections.Generic;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using MQTTnet.Protocol;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core;
using Newtonsoft.Json;
using TcHmiSrv.Core.Tools.Json.Newtonsoft;
using MQTTnet.Packets;
using System.Linq;

namespace MqttCoreService
{
    internal class TopicObject
    {
        public string TopicName { get; set; }
        public string TopicData { get; set; }

        public TopicObject()
        {
            TopicName = "";
            TopicData = "";
        }

        public TopicObject(string topicName, string topicData)
        {
            TopicName = topicName;
            TopicData = topicData;
        }
    }

    internal static class TopicObjectListData
    {
        public static List<TopicObject> TopicObjectList = new List<TopicObject>();
    }

    //Class used for dynamic creation of symbols in the HMI
    internal class MqttSubscribeTopicSymbol : SymbolWithValue
    {
        private TopicObject TopicObjects { get; }

        private static Value MqttSubscribeTopicAsValue(TopicObject objects)
        {
            //find data from list
            var item = TopicObjectListData.TopicObjectList.FirstOrDefault(o => o.TopicName == objects.TopicName);
            if (item != null)
            {
                objects.TopicData = item.TopicData;
            }
            //convert to json string
            return TcHmiJsonSerializer.Deserialize<Value>(JsonConvert.SerializeObject(objects));
        }

        protected override Value Value
        {
            get
            {
                return MqttSubscribeTopicAsValue(this.TopicObjects);
            }
        }

        public MqttSubscribeTopicSymbol(TopicObject objectsValues) : base(null, TcHmiJSchemaGenerator.DefaultGenerator.Generate(objectsValues.GetType()))
        {
            this.TopicObjects = objectsValues;
        }
    }
   
    internal class MqttCoreTask
    {
        #region FIELDS
        private MqttFactory _mqttFactoryPub = new MqttFactory();
        private MqttFactory _mqttFactorySub = new MqttFactory();
        private IMqttClient _mqttClientPub;
        private IMqttClient _mqttClientSub;
        private string _tcpServer;
        private int? _port;
        private string _userName;
        private string _password;
        #endregion FIELDS
       
        #region PROP
        public string Username
        {
            get { return _userName; }
            set { _userName = value; }
        }
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }
        public string Server
        {
            get { return _tcpServer; }
            set { _tcpServer = value; }
        }
        public int? Port
        {
            get { return _port; }
            set { _port = value; }
        }
        #endregion PROP

        #region METHODS
        public async Task Publish(string topic, string payload)
        {
            _mqttClientPub = _mqttFactoryPub.CreateMqttClient();
            var options = OptionsBuilder();
            await _mqttClientPub.ConnectAsync(options);
            await PublishMessageAsync(_mqttClientPub, topic, payload);
            await _mqttClientPub.DisconnectAsync();
        }

        public async Task Subscribe(List<string> topics)
        {
            TopicObjectListData.TopicObjectList.Clear();
            _mqttClientSub = _mqttFactorySub.CreateMqttClient();

            if (_mqttClientSub.IsConnected) await _mqttClientSub.DisconnectAsync();

            var options = OptionsBuilder();

            MqttClientSubscribeOptions objSubOptions = new MqttClientSubscribeOptions();
            List<MqttTopicFilter> topicList = new List<MqttTopicFilter>();
            foreach (string topic in topics)
            {
                MqttTopicFilter topicObjectFilter = new MqttTopicFilter();
                topicObjectFilter.Topic = topic;
                topicList.Add(topicObjectFilter);

                TopicObject topicObject = new TopicObject(topic, null);
                TopicObjectListData.TopicObjectList.Add(topicObject);
            }
            objSubOptions.TopicFilters = topicList;

            _mqttClientSub.ApplicationMessageReceivedAsync += SubscribeTopicMessagesRetrieved;

            await _mqttClientSub.ConnectAsync(options, CancellationToken.None);
            await _mqttClientSub.SubscribeAsync(objSubOptions);
        }

        //Called on shutdown
        public void MqttDisposeDisconnect()
        {
            try
            {
                _mqttClientPub.DisconnectAsync();
                _mqttClientPub.Dispose();

                _mqttClientSub.DisconnectAsync();
                _mqttClientSub.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private Task SubscribeTopicMessagesRetrieved(MqttApplicationMessageReceivedEventArgs arg)
        {
            //Debug.WriteLine("-----NEW MESSAGE------");
            //Debug.WriteLine("Payload: " + Encoding.UTF8.GetString(arg.ApplicationMessage.Payload));
            //Debug.WriteLine("Topic: " + arg.ApplicationMessage.Topic);
            //Debug.WriteLine("---------END-------");

            var item = TopicObjectListData.TopicObjectList.FirstOrDefault(o => o.TopicName == arg.ApplicationMessage.Topic);
            if (item != null) item.TopicData = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

            return Task.CompletedTask;
        }

        private static async Task PublishMessageAsync(IMqttClient client, string topic, string payload)
        {
            Debug.WriteLine(payload);
            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(topic)
                                .WithPayload(payload)
                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                .Build();
            if (client.IsConnected)
                await client.PublishAsync(message);
        }

        private MqttClientOptions OptionsBuilder()
        {
            MqttClientOptions options;

            if (!string.IsNullOrWhiteSpace(_userName) || !string.IsNullOrWhiteSpace(_password))
            {
                options = new MqttClientOptionsBuilder()
                  .WithClientId(Guid.NewGuid().ToString())
                  .WithTcpServer(_tcpServer, _port)
                  .WithCredentials(_userName, _password)
                  .WithCleanSession()
                  .Build();
            }
            else
            {
                options = new MqttClientOptionsBuilder()
                  .WithClientId(Guid.NewGuid().ToString())
                  .WithTcpServer(_tcpServer, _port)
                  .WithCleanSession()
                  .Build();
            }

            return options;
        }

        #endregion METHODS
    }
}
