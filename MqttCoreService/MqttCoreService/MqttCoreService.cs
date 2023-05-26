using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;
using System.Threading.Tasks;
using System.Diagnostics;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Settings;
using System.Collections.Generic;
using System.Linq;
using TcHmiSrv.Core.Tools.Json.Newtonsoft;
using TcHmiSrv.Core.Listeners.ConfigListenerEventArgs;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs;
using System.Text.RegularExpressions;

namespace MqttCoreService
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class MqttCoreService : IServerExtension
    {
        #region FIELDS 
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ShutdownListener _shutdownListener = new ShutdownListener();
        private readonly ConfigListener _configListener = new ConfigListener();
        private readonly DynamicSymbolsProvider _symbolProvider = new DynamicSymbolsProvider();

        private readonly MqttCoreTask _mqttCoreTask = new MqttCoreTask();
        private readonly List<string> _topics = new List<string>();
        #endregion FIELDS

        #region EVENTS 

        // Called after the TwinCAT HMI server loaded the server extension.
        public ErrorValue Init()
        {
            // Wait for a debugger to be attached to the current process and signal a
            // breakpoint to the attached debugger in Init
            //TcHmiApplication.AsyncDebugHost.WaitForDebugger(true);

            _requestListener.OnRequest += OnRequest;
            _shutdownListener.OnShutdown += OnShutdown;
            _configListener.OnChange += OnChange;

            //set up the config listener for all parameters
            var settings = new ConfigListenerSettings();
            var filter = new ConfigListenerSettingsFilter(
                ConfigChangeType.OnChange, new string[] { "*" }
            );
            settings.Filters.Add(filter);
            TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, settings);

            //Log
            TcHmiAsyncLogger.Send(Severity.Info, "EXTENSION_INIT", "");

            return ErrorValue.HMI_SUCCESS;
        }

        // Called when the extension get disabled/shutsdown
        private void OnShutdown(object sender, OnShutdownEventArgs e)
        {
            //Unregister listeners
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _requestListener);
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _shutdownListener);
            TcHmiApplication.AsyncHost.UnregisterListener(TcHmiApplication.Context, _configListener);

            //Dispose and disconnect mqtt connections
            _mqttCoreTask.MqttDisposeDisconnect();

            //Log
            TcHmiAsyncLogger.Send(Severity.Info, "EXTENSION_SHUTDOWN", "");
        }

        // Called when the user changes data in the config-page of the extension. Also called on extension init. 
        private void OnChange(object sender, OnChangeEventArgs e)
        {
            if (e.Path.StartsWith("topics"))
            {
                //Get value
                var topics = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "topics");

                //Generate list changes
                var listChanges = new List<string>();
                foreach (var item in topics)
                    listChanges.Add(item.ToString());

                //Compare exception lists
                var topicRemovedList = _topics.Except(listChanges).ToList(); //delete change 
                var topicAddedList = listChanges.Except(_topics).ToList(); //new change

                //Update
                CreateDynamicSymbols(topicAddedList, topicRemovedList);

                //Log
                //TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", new string[] { "Topics" });
            }
            else if (e.Path.StartsWith("Password"))
            {
                //Get Value & Update
                var password = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "Password");
                _mqttCoreTask.Password = password;
                //Log
                TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", new string[] { "Password" });
            }
            else if (e.Path.StartsWith("Username"))
            {
                //Get Value & Update
                var userName = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "Username");
                _mqttCoreTask.Username = userName;
                //Log
                TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", new string[] { "Username" });
            }
            else if (e.Path.StartsWith("MQTTServer"))
            {
                //Get Value & Update
                var server = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "MQTTServer");
                _mqttCoreTask.Server = server;
                //Log
                TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", new string[] { "MQTTServer" });
            }
            else if (e.Path.StartsWith("Port"))
            {
                //Get Value & Update
                var port = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "Port");
                _mqttCoreTask.Port = port;
                //Log
                TcHmiAsyncLogger.Send(e.Context, Severity.Info, "NEW_CONFIG", new string[] { "Port" });
            }
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private async void OnRequest(object sender, OnRequestEventArgs e)
        {
            try
            {
                e.Commands.Result = MqttCoreServiceErrorValue.Success;

                foreach (Command command in _symbolProvider.HandleCommands(e.Commands))
                {
                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (command.Mapping)
                        {
                            case "Publish": //MQTT Publish
                                await Publish(command, e.Context);
                                break;

                            case "Diagnostics": //Diagnostic data for the extension
                                break;

                            default:
                                command.ExtensionResult = MqttCoreServiceErrorValue.Fail;
                                command.ResultString = "Unknown command '" + command.Mapping + "' not handled.";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = MqttCoreServiceErrorValue.Fail;
                        command.ResultString = "Calling command '" + command.Mapping + "' failed! Additional information: " + ex.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(), ErrorValue.HMI_E_EXTENSION);
            }
        }

        #endregion EVENTS

        #region METHODS

        // Called when the client make a change to the topics in the server configuration page.
        private void CreateDynamicSymbols(List<string> addList, List<string> removeList)
        {
            if (addList.Count > 0)
            {
                foreach (var item in addList)
                {
                    if (!_topics.Contains(item.ToString()))
                        _topics.Add(item.ToString());
                }
                foreach (var item in _topics)
                {
                    if (Regex.IsMatch(item, "[+#]"))
                    {
                        _symbolProvider.AddOrUpdate(item.ToString(), new MqttSubscribeWildcardTopicSymbol(new WildcardObject { WildcardTopic = item.ToString() }));
                    }
                    else
                    {
                        //MqttSubscribeTopicSymbolT
                        //_symbolProvider.AddOrUpdate(item.ToString(), new MqttSubscribeTopicSymbolT(new TopicObject { TopicName = item.ToString() }));
                        _symbolProvider.AddOrUpdate(item.ToString(), new MqttSubscribeTopicSymbol(new TopicObject { TopicName = item.ToString() }));
                    }                    
                }
                //Log
                TcHmiAsyncLogger.Send(Severity.Info, "ADD_SYMBOLS", "");
            }
            else if (removeList.Count > 0)
            {
                foreach (var item in removeList)
                {
                    if (_topics.Contains(item.ToString()))
                    {
                        try
                        {
                            _topics.Remove(item.ToString());
                            _symbolProvider.Remove(item.ToString());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }

                    }
                }
                //Log
                TcHmiAsyncLogger.Send(Severity.Info, "REMOVE_SYMBOLS", "");
            }

            //Set up subscriber
            _ = _mqttCoreTask.Subscribe(_topics);
        }

        // Called when Publish method gets triggered by the client.
        private async Task<ErrorValue> Publish(Command command, Context context)
        {
            if (command.WriteValue != null && command.WriteValue.Type == TcHmiSrv.Core.ValueType.Struct)
            {
                try
                {
                    var writeValue = command.WriteValue;
                    var topic = writeValue["topic"];
                    var message = TcHmiJsonSerializer.Serialize(writeValue["message"]);
                    await _mqttCoreTask.Publish(topic, message);
                    command.ExtensionResult = MqttCoreServiceErrorValue.Success;
                }
                catch (Exception ex)
                {
                    command.ExtensionResult = MqttCoreServiceErrorValue.PublishFailed;
                    TcHmiAsyncLogger.Send(context, Severity.Error, "ERROR_PUBLISH", new string[] { ex.Message });
                }
            }
            else
            {
                command.ExtensionResult = MqttCoreServiceErrorValue.DataWrongTypeOrEmpty;
            }

            command.ReadValue = command.WriteValue;
            return ErrorValue.HMI_SUCCESS;
        }

        #endregion METHODS
    }
}
