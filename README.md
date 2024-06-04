## Disclaimer
This is a personal guide not a peer reviewed journal or a sponsored publication. We make
no representations as to accuracy, completeness, correctness, suitability, or validity of any
information and will not be liable for any errors, omissions, or delays in this information or any
losses injuries, or damages arising from its display or use. All information is provided on an as
is basis. It is the reader’s responsibility to verify their own facts.

The views and opinions expressed in this guide are those of the authors and do not
necessarily reflect the official policy or position of any other agency, organization, employer or
company. Assumptions made in the analysis are not reflective of the position of any entity
other than the author(s) and, since we are critically thinking human beings, these views are
always subject to change, revision, and rethinking at any time. Please do not hold us to them
in perpetuity.

## MqttCoreService

MQTT client with subscribe and publish function and dynamic symbol creation for TwinCAT HMI.

Created with [MQTTnet](https://github.com/dotnet/MQTTnet)

## Functions

The package consists of a service configuration window;

![enter image description here](https://github.com/hijaaack/images/assets/75740551/d334e35f-a9e5-4bdf-93f9-a53ab0fb9195)

Publish method is available under the HMI Configuration window. This function can be called with javascript. 

For every single topic subscription a symbol will dynamically be created of the type TopicObject. Each TopicObject consists of the TopicData(string) and the TopicName(string).

For every single wildcard subscription a symbol will dynamically be created of the type WildcardObject. The WildcardObject is an dynamic array of the type TopicObject. 

![enter image description here](https://github.com/hijaaack/images/assets/75740551/e3106cbb-2b94-4571-920a-966766a566af)

## Prerequisites

Tested with HMI version: 1.12.760.59

License: [TF2200](https://www.beckhoff.com/sv-se/products/automation/twincat/tfxxxx-twincat-3-functions/tf2xxx-tc3-hmi/tf2200.html) is needed in the HMI-Server to be able to use this C# Server Extension.

## Installation

Easiest way to install this package is inside your TwinCAT HMI Project. 
**Right click** References and click "Manage NuGet Packages.." then browse for the file and install it! 

![enter image description here](https://user-images.githubusercontent.com/75740551/101645035-32cef100-3a36-11eb-88f4-eeaccd3366d6.png) 
