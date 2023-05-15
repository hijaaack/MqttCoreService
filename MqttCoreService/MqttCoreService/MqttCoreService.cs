using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace MqttCoreService
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class MqttCoreService : IServerExtension
    {
        private readonly RequestListener requestListener = new RequestListener();

        // Called after the TwinCAT HMI server loaded the server extension.
        public ErrorValue Init()
        {
            this.requestListener.OnRequest += this.OnRequest;
            return ErrorValue.HMI_SUCCESS;
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            try
            {
                e.Commands.Result = MqttCoreServiceErrorValue.MqttCoreServiceSuccess;

                foreach (Command command in e.Commands)
                {
                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (command.Mapping)
                        {
                            // case "YOUR_MAPPING":
                            //     Handle command
                            //     break;

                            default:
                                command.ExtensionResult = MqttCoreServiceErrorValue.MqttCoreServiceFail;
                                command.ResultString = "Unknown command '" + command.Mapping + "' not handled.";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = MqttCoreServiceErrorValue.MqttCoreServiceFail;
                        command.ResultString = "Calling command '" + command.Mapping + "' failed! Additional information: " + ex.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(), ErrorValue.HMI_E_EXTENSION);
            }
        }
    }
}
