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

![enter image description here](https://private-user-images.githubusercontent.com/75740551/336399460-d334e35f-a9e5-4bdf-93f9-a53ab0fb9195.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MTc0OTQ4NzEsIm5iZiI6MTcxNzQ5NDU3MSwicGF0aCI6Ii83NTc0MDU1MS8zMzYzOTk0NjAtZDMzNGUzNWYtYTllNS00YmRmLTkzZjktYTUzYWIwZmI5MTk1LnBuZz9YLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFWQ09EWUxTQTUzUFFLNFpBJTJGMjAyNDA2MDQlMkZ1cy1lYXN0LTElMkZzMyUyRmF3czRfcmVxdWVzdCZYLUFtei1EYXRlPTIwMjQwNjA0VDA5NDkzMVomWC1BbXotRXhwaXJlcz0zMDAmWC1BbXotU2lnbmF0dXJlPTk1MDk4YTBlNTI3ODQxOTQyNzA2OTc2YmRkM2NjMzJjNjkwYmI3M2E5YWE0OTQ1M2ViYjBiYmI2YzRkNzgwMzkmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0JmFjdG9yX2lkPTAma2V5X2lkPTAmcmVwb19pZD0wIn0.eiphxgRLHBxkCr9UdZETXvesw-BHW9TlwMr4niWP-M4)

Publish method is available under the HMI Configuration window. This function can be called with javascript. 

For every single topic subscription a symbol will dynamically be created of the type TopicObject. Each TopicObject consists of the TopicData(string) and the TopicName(string).

For every single wildcard subscription a symbol will dynamically be created of the type WildcardObject. The WildcardObject is an dynamic array of the type TopicObject. 

![enter image description here](https://private-user-images.githubusercontent.com/75740551/336399320-e3106cbb-2b94-4571-920a-966766a566af.png?jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3MTc0OTQ4NzEsIm5iZiI6MTcxNzQ5NDU3MSwicGF0aCI6Ii83NTc0MDU1MS8zMzYzOTkzMjAtZTMxMDZjYmItMmI5NC00NTcxLTkyMGEtOTY2NzY2YTU2NmFmLnBuZz9YLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFWQ09EWUxTQTUzUFFLNFpBJTJGMjAyNDA2MDQlMkZ1cy1lYXN0LTElMkZzMyUyRmF3czRfcmVxdWVzdCZYLUFtei1EYXRlPTIwMjQwNjA0VDA5NDkzMVomWC1BbXotRXhwaXJlcz0zMDAmWC1BbXotU2lnbmF0dXJlPTU2OGMxYjFhOTA5ODIxMjFhMDI3NGI3MTk5MDc1Y2NlOGE5ODIyM2IzMWMyMjBkZGVmMzRlOWQxYjYzNTU5MDYmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0JmFjdG9yX2lkPTAma2V5X2lkPTAmcmVwb19pZD0wIn0.PBswpfAA-EE70FeP51YV0MjiVEitr4JQtsgeqG8RqlI)

## Prerequisites

Tested with HMI version: 1.12.760.59

License: [TF2200](https://www.beckhoff.com/sv-se/products/automation/twincat/tfxxxx-twincat-3-functions/tf2xxx-tc3-hmi/tf2200.html) is needed in the HMI-Server to be able to use this C# Server Extension.

## Installation

Easiest way to install this package is inside your TwinCAT HMI Project. 
**Right click** References and click "Manage NuGet Packages.." then browse for the file and install it! 

![enter image description here](https://user-images.githubusercontent.com/75740551/101645035-32cef100-3a36-11eb-88f4-eeaccd3366d6.png) 
