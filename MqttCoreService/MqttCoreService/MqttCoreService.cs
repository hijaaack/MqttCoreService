//-----------------------------------------------------------------------
// <copyright file="MqttCoreService.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

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

        /*
        GETTING STARTED

        The recommended way to get started is to look at a few of the sample extensions that are available on GitHub:
        https://github.com/Beckhoff/TF2000_Server_Samples

        The full documentation for the extension API can be found in the Beckhoff Information System:
        https://infosys.beckhoff.com/english.php?content=../content/1033/te2000_tc3_hmi_engineering/10591698827.html

        An offline version of this documentation is available at this path:
        %TWINCAT3DIR%..\Functions\TE2000-HMI-Engineering\Infrastructure\TcHmiServer\docs\TcHmiSrvExtNet.Core.Documentation.chm
        */

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
