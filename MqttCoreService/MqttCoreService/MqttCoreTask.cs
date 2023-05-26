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
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace MqttCoreService
{
    internal class TopicObject
    {
        public string TopicName { get; set; }
        //Possible to change TopicData to "OBJECT"? 
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

    internal class WildcardObject
    {
        public string WildcardTopic { get; set; }
        public List<TopicObject> WildcardList { get; set; }

        public WildcardObject()
        {
            WildcardTopic = "";
            WildcardList = new List<TopicObject>();
        }
        public WildcardObject(string topicName, List<TopicObject> list)
        {
            WildcardTopic = topicName;
            WildcardList = list;
        }
    }

    internal static class TopicObjectListData
    {
        //Static list for topic subscriptions
        public static List<TopicObject> TopicObjectList = new List<TopicObject>();
        //Static list for wildcard topics subscriptions
        public static List<WildcardObject> WildcardObjectList = new List<WildcardObject>();
    }

    //TEST CLASS FOR TRYING THE JsonSchemaValue? -not used atm
    internal class MqttSubscribeTopicSymbolT : SymbolWithValue
    {
        private TopicObject TopicObjects { get; }

        public MqttSubscribeTopicSymbolT(TopicObject objectsValues) : base(null, SchemaValue)
        {
            this.TopicObjects = objectsValues;
        }

        private static JsonSchemaValue SchemaValue { get; } = JsonType();
        private static JsonSchemaValue JsonType()
        {
            string jsonString =
                 @"{
                    ""type"": ""object"",
                        ""properties"": {
                            ""TopicData"": {
                                ""$ref"": ""tchmi:general#/definitions/Object""
                            },
			                ""TopicName"": {
                                ""$ref"": ""tchmi:general#/definitions/String""
                            }
                        }
                }";
            JToken token = JToken.Parse(jsonString);
            Type type = token.GetType();
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema schema = generator.Generate(type);
            Debug.WriteLine(schema.ToString());
            return new JsonSchemaValue(schema);
        }

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

    }

    //Class used for dynamic creation of TopicObject symbols in the HMI
    internal class MqttSubscribeTopicSymbol : SymbolWithValue
    {
        private TopicObject TopicObjects { get; }

        public MqttSubscribeTopicSymbol(TopicObject objectsValues) : base(null, TcHmiJSchemaGenerator.DefaultGenerator.Generate(objectsValues.GetType()))
        {
            this.TopicObjects = objectsValues;
        }

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

    }

    //Class used for dynamic creation of Wildcard TopicObject symbols in the HMI
    internal class MqttSubscribeWildcardTopicSymbol : SymbolWithValue
    {
        private WildcardObject WildcardObjects { get; }

        private static Value MqttSubscribeTopicAsValue(WildcardObject objects)
        {
            // Finding the matching data
            // var matchingList = TopicObjectListData.TopicObjectWildcardList
            //     .SelectMany(innerList => innerList)
            //     .Where(topic => objectsList
            //     .Any(obj => obj.TopicName == topic.TopicName))
            //     .ToList();



            // if (matchingList != null)
            // {
            //     var objectListIndex = objectsList.FindIndex(lst1 => matchingList.Any(lst2 => lst1.TopicName == lst2.TopicName));
            //     var matchingListIndex = matchingList.FindIndex(lst1 => objectsList.Any(lst2 => lst1.TopicName == lst2.TopicName));
            //     if (objectListIndex != -1 && matchingListIndex != -1)
            //     {
            //         //Update list with new data
            //         objectsList[objectListIndex].TopicData = matchingList[matchingListIndex].TopicData;
            //     }
            // }
            //find data from list
            var item = TopicObjectListData.WildcardObjectList.FirstOrDefault(o => o.WildcardTopic == objects.WildcardTopic);
            if (item != null)
            {
                objects.WildcardList = item.WildcardList;
            }

            //convert to json string
            return TcHmiJsonSerializer.Deserialize<Value>(JsonConvert.SerializeObject(objects));
        }

        protected override Value Value
        {
            get
            {
                return MqttSubscribeTopicAsValue(this.WildcardObjects);
            }
        }

        public MqttSubscribeWildcardTopicSymbol(WildcardObject objects) : base(null, TcHmiJSchemaGenerator.DefaultGenerator.Generate(objects.GetType()))
        {
            this.WildcardObjects = objects;
        }
    }

    internal class MqttCoreTask
    {
        private readonly MqttFactory _mqttFactoryPub = new MqttFactory();
        private readonly MqttFactory _mqttFactorySub = new MqttFactory();
        private IMqttClient _mqttClientPub;
        private IMqttClient _mqttClientSub;
        private string _tcpServer;
        private int? _port;
        private string _userName;
        private string _password;

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
                var topicObjectFilter = new MqttTopicFilter { Topic = topic };
                topicList.Add(topicObjectFilter);

                //Check if topic should be added to wildcardList or normal list
                if (Regex.IsMatch(topic, "[+#]"))
                {
                    //Add to WildcardList                  
                    if (!TopicObjectListData.WildcardObjectList.Any(o => o.WildcardTopic == topic))
                    {
                        var wildcardObject = new WildcardObject(topic, new List<TopicObject>());
                        TopicObjectListData.WildcardObjectList.Add(wildcardObject);
                    }
                }
                else
                {
                    //Add to normal list
                    if (!TopicObjectListData.TopicObjectList.Any(o => o.TopicName == topic))
                    {
                        var topicObject = new TopicObject(topic, null);
                        TopicObjectListData.TopicObjectList.Add(topicObject);
                    }
                }
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

                _mqttClientSub.ApplicationMessageReceivedAsync -= SubscribeTopicMessagesRetrieved;
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
            Debug.WriteLine("-----NEW MESSAGE------");
            Debug.WriteLine("Payload: " + Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment));
            Debug.WriteLine("Topic: " + arg.ApplicationMessage.Topic);
            Debug.WriteLine("---------END-------");

            //kika ifall subscribedatats topic matchar topic i wildcardlista, isf skicka in datat där, om det OCKSÅ finns i vanliga listan skicka in datat där
            //måste kunna få match på olika scenarion av topics som topic/# topic/+/test där # och + kmr att retuneras som det riktiga topic-svaret
            //datat in i denna funktion kmr vara som exempel topic1/test1/test2

            //Check if topic matches objectList
            var topicObject = TopicObjectListData.TopicObjectList.FirstOrDefault(o => o.TopicName == arg.ApplicationMessage.Topic);
            if (topicObject != null) topicObject.TopicData = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

            //Check if topic matches wildcardObjectList
            foreach (var wildcardObject in TopicObjectListData.WildcardObjectList)
            {
                if (MatchTopic(arg.ApplicationMessage.Topic, wildcardObject.WildcardTopic))
                {
                    //Check if wildcard topic has been added before, then update data, else add it to the list
                    if (wildcardObject.WildcardList.Any())
                    {
                        var matchObject = wildcardObject.WildcardList.FirstOrDefault(o => o.TopicName == arg.ApplicationMessage.Topic);
                        if (matchObject != null)
                        {
                            matchObject.TopicData = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);
                        }
                        else
                        {
                            if (!wildcardObject.WildcardList.Any(x => x.TopicName == arg.ApplicationMessage.Topic))
                                wildcardObject.WildcardList.Add(new TopicObject(arg.ApplicationMessage.Topic, Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment)));
                        }
                    }
                    else
                    {
                        if (!wildcardObject.WildcardList.Any(x => x.TopicName == arg.ApplicationMessage.Topic))
                            wildcardObject.WildcardList.Add(new TopicObject(arg.ApplicationMessage.Topic, Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment)));
                    }
                }
            }
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
        private bool MatchTopic(string topic, string pattern)
        {
            string[] topicParts = topic.Split('/');
            string[] patternParts = pattern.Split('/');

            if (topicParts.Length < patternParts.Length)
                return false;

            for (int i = 0; i < patternParts.Length; i++)
            {
                if (patternParts[i] == "#")
                    return true;

                if (patternParts[i] != "+" && patternParts[i] != topicParts[i])
                    return false;
            }

            return topicParts.Length == patternParts.Length;
        }
    }
}